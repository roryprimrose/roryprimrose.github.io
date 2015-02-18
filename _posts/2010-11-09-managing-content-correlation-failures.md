---
title: Managing content correlation failures
categories : .Net
tags : WCF, WF
date: 2010-11-09 16:05:58 +10:00
---

I [posted yesterday][0] about how to get content correlation of multiple WCF service operations to work with hosted WF services. The request and reply data of the WCF service operations is the basis of content correlation. It is therefore quite easy for a client application to send data to the service that does not satisfy business validation. 

If the invalid data sent to the service operation participates in content correlation then the WorkflowServiceHost will not identify a WF instance. This unfortunately means that business validation of this data will not run in the workflow as no workflow instance is executing. The result is that the client will receive the following very unfriendly FaultException (assuming no exception shielding is in place).

> _System.ServiceModel.FaultException: The execution of an InstancePersistenceCommand was interrupted because the instance key 'bea948ac-70fa-b125-91e6-f06752ce7671' was not associated to an instance. This can occur because the instance or key has been cleaned up, or because the key is invalid. The key may be invalid if the message it was generated from was sent at the wrong time or contained incorrect correlation data.   
>    at System.ServiceModel.Activities.Dispatcher.ControlOperationInvoker.ControlOperationAsyncResult.Process()   
>    at System.ServiceModel.Activities.Dispatcher.ControlOperationInvoker.ControlOperationAsyncResult..ctor(ControlOperationInvoker invoker, Object[] inputs, IInvokeReceivedNotification notification, TimeSpan timeout, AsyncCallback callback, Object state)   
>    at System.ServiceModel.Activities.Dispatcher.ControlOperationInvoker.InvokeBegin(Object instance, Object[] inputs, IInvokeReceivedNotification notification, AsyncCallback callback, Object state)   
>    at System.ServiceModel.Dispatcher.DispatchOperationRuntime.InvokeBegin(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage5(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage41(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage4(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage31(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage3(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage2(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage11(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.ImmutableDispatchRuntime.ProcessMessage1(MessageRpc& rpc)   
>    at System.ServiceModel.Dispatcher.MessageRpc.Process(Boolean isOperationContextSet)_

If exception shielding is in place by using an IErrorHandler implementation then the client should get a generic service fault message instead. Neither scenario is appropriate as the actual failure is due to the service receiving invalid business data. The service should be able to provide a meaningful business fault to the client in this case to indicate what the client application has done wrong. 

This is the outstanding issue from yesterday’s post. How can the service identify that the FaultException thrown relates to a correlation failure and determine which content correlation was the cause? 

My scenario from yesterday’s post was using a TransactionId as the value that participates in content correlation between the RegisterAccount and ConfirmRegistration service operations. The client should expect a known business fault that identifies that the TransactionId is invalid rather than the above generic FaultException or shielded exception.

The solution to this issue is to extend the behaviour of an IErrorHandler implementation. The change in question will be to determine that the thrown exception is a content correlation exception. It will then convert the exception to a specific business fault based on the current service operation.

The ProvideFault method in the IErrorHandler implementation in the service has been updated to manage this detection and conversion process.{% highlight csharp linenos %}
public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
{
    Exception errorToProcess = AttemptBusinessFailureExceptionConversion(error);
    
    BusinessFailureException<BusinessFaultCode> businessFailure = errorToProcess as BusinessFailureException<BusinessFaultCode>;
    
    if (businessFailure != null)
    {
        IEnumerable<BusinessFaultItem> faultSet = CreateFaultSet(businessFailure.Failures);
    
    
        BusinessFault businessFault = new BusinessFault(businessFailure.Message, faultSet);
        FaultException<BusinessFault> faultException = new FaultException<BusinessFault>(businessFault, businessFailure.Message);
        MessageFault messageFault = faultException.CreateMessageFault();
    
        fault = Message.CreateMessage(version, messageFault, faultException.Action);
    }
    else
    {
        SystemFault systemFault;
        {
            SystemFailureException systemFailure = errorToProcess as SystemFailureException;
            SecurityException securityFailure = errorToProcess as SecurityException;
    
            if (systemFailure != null)
            {
                systemFault = new SystemFault(systemFailure.Message);
            }
            else if (securityFailure != null)
            {
                systemFault = new SystemFault(Resources.FaultHandler_AccessDenied);
            }
            else
            {
                systemFault = new SystemFault(Resources.FaultHandler_UnknownFailure);
            }
        }
    
        FaultException<SystemFault> faultException = new FaultException<SystemFault>(systemFault, systemFault.Message);
    
        MessageFault messageFault = faultException.CreateMessageFault();
    
        fault = Message.CreateMessage(version, messageFault, faultException.Action);
    }
}
{% endhighlight %}

The existing code already managed a BusinessFailureException<T> and converted it to a BusinessFault that is a known fault contract for the service. The method now makes a call out to AttemptBusinessFailureExceptionConversion as an initial step.{% highlight csharp linenos %}
private static Exception AttemptBusinessFailureExceptionConversion(Exception errorToProcess)
{
    FaultException thrownFaultException = errorToProcess as FaultException;
    
    if (thrownFaultException == null)
    {
        return errorToProcess;
    }
    
    FaultCode faultCode = thrownFaultException.Code;
    
    if (faultCode == null
        || faultCode.IsPredefinedFault == false
        || faultCode.IsSenderFault == false)
    {
        return errorToProcess;
    }
    
    FaultCode subCode = faultCode.SubCode;
    
    if (subCode == null
        || subCode.Namespace != &quot;http://schemas.datacontract.org/2008/10/WorkflowServices&quot;
        || subCode.Name != &quot;InstanceNotFound&quot;)
    {
        return errorToProcess;
    }
    
    String contractName = OperationContext.Current.IncomingMessageHeaders.Action;
    
    if (contractName == &quot;http://tempuri.org/IRegistration/ConfirmRegistration&quot;)
    {
        return new BusinessFailureException<BusinessFaultCode>(BusinessFailureStore.InvalidTransactionIdProvided);
    }
        
    return errorToProcess;
}
{% endhighlight %}

The AttemptBusinessFailureExceptionConversion method looks for a predefined FaultException thrown by the service that has a particular failure code. Correlation failures have a Code.SubCode name of _InstanceNotFound_ with the namespace of _http://schemas.datacontract.org/2008/10/WorkflowServices_. The method can return a specific business failure now that a correlation failure has been identified. 

The business failure to return will be determined based on the current service operation. In this scenario there is a check that the failed service operation is the IRegistration.ConfirmRegistration method. A TransactionId value is the content correlation value for this service operation. If the WorkflowServiceHost is not able to identify a workflow instance using the provided TransactionId then the TransactionId value is invalid. This is the business failure that is then returned. The ProvideFault method then continues its existing logic by processing the business failure as an appropriate business fault.

[0]: /post/2010/11/08/Hosted-workflow-service-with-content-correlation.aspx
