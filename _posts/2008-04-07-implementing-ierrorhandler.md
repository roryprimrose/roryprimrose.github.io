---
title: Implementing IErrorHandler
categories : .Net
tags : Tracing, WCF
date: 2008-04-07 16:08:49 +10:00
---

As soon as I read about [IErrorHandler][0] in [Juval Lowy's book][1], I was sold. This interface in WCF is excellent to use for error handling and shielding at service boundaries. The interface comes with two methods. 

1. [ProvideFault][2] allows you to handle the error and determine what is returned to the client. This is where exception shielding comes in. The service implementation or any of the layers in the service can throw an exception. The error handler is the opportunity to determine whether the exception thrown is something that the service understands and is happy for the client to receive or whether the exception needs to be shielded into another exception/fault. A shielded exception would typically be a generic service exception that says that the service encountered an error.
1. [HandleError][3] allows you to process some error specific logic asynchronous to the service call. This means that you could do some tracing/instrumentation or some expensive operation without blocking the client call.

There issue that I have with IErrorHandler is that the documentation is not detailed enough to allow you to hook up an error handler so that it is actually used. My recent experience has resulted in me defining an IErrorHandler, but not correctly configuring it so that it gets invoked. This highlights a disadvantage of the IErrorHandler design (assuming a configuration based setup). The service will still work without the handler being invoked, you will just get different exceptions on the client. This has a potential security risk if exception shielding has been written to shield sensitive information from clients.

The MSDN documentation (see [here][0]) provides examples about how to to create an IErrorHandler implementation including the behavior extension, but doesn't provide the full class examples. Hence my misunderstanding that resulted in my handler not being invoked. I suggest that two classes get created. One is the error handler, the other is the error handler element for configuration.

**ErrorHandler**

The error handler implements IErrorHandler, but also implements IServiceBehavior. This interface allows the error handler to be hooked up by configuration.

    {% highlight csharp linenos %}
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
     
    namespace WcfServiceLibrary1
    {
        public class ErrorHandler : IErrorHandler, IServiceBehavior
        {
            public void AddBindingParameters(
                ServiceDescription serviceDescription,
                ServiceHostBase serviceHostBase,
                Collection<ServiceEndpoint&gt; endpoints,
                BindingParameterCollection bindingParameters)
            {
            }
     
            public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
            {
                IErrorHandler errorHandler = new ErrorHandler();
     
                foreach (ChannelDispatcherBase channelDispatcherBase in serviceHostBase.ChannelDispatchers)
                {
                    ChannelDispatcher channelDispatcher = channelDispatcherBase as ChannelDispatcher;
     
                    if (channelDispatcher != null)
                    {
                        channelDispatcher.ErrorHandlers.Add(errorHandler);
                    }
                }
            }
     
            public bool HandleError(Exception error)
            {
                Trace.TraceError(error.ToString());
     
                // Returning true indicates you performed your behavior.
                return true;
            }
     
            public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
            {
            }
     
            public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
            {
                // Shield the unknown exception
                FaultException faultException = new FaultException(
                    "Server error encountered. All details have been logged.");
                MessageFault messageFault = faultException.CreateMessageFault();
     
                fault = Message.CreateMessage(version, messageFault, faultException.Action);
            }
        }
    }
    
    {% endhighlight %}

**ErrorHandlerElement**

The error handler element defines the extension behavior such that ErrorHandler can be defined against a service behavior.

    {% highlight csharp linenos %}
    using System;
    using System.ServiceModel.Configuration;
     
    namespace WcfServiceLibrary1
    {
        public class ErrorHandlerElement : BehaviorExtensionElement
        {
            protected override object CreateBehavior()
            {
                return new ErrorHandler();
            }
     
            public override Type BehaviorType
            {
                get
                {
                    return typeof(ErrorHandler);
                }
            }
        }
    }
    
    {% endhighlight %}

**Configuration**

This configuration identifies the ErrorHandlerElement as a behavior extension, which then allows errorHandler (the name of the configured extension) to be defined against the service behavior. This is how the error handler gets hooked up for the service.

    {% highlight xml linenos %}
    <?xml version="1.0" encoding="utf-8" ?&gt;
    <configuration&gt;
     
      <system.web&gt;
        <compilation debug="true" /&gt;
      </system.web&gt;
     
      <system.serviceModel&gt;
        <behaviors&gt;
          <serviceBehaviors&gt;
            <behavior name="MyServiceBehavior"&gt;
              <serviceDebug includeExceptionDetailInFaults="true" /&gt;
              <errorHandler /&gt;
            </behavior&gt;
          </serviceBehaviors&gt;
        </behaviors&gt;
        <extensions&gt;
          <behaviorExtensions&gt;
            <add name="errorHandler"
                type="WcfServiceLibrary1.ErrorHandlerElement, WcfServiceLibrary1" /&gt;
          </behaviorExtensions&gt;
        </extensions&gt;
        <services&gt;
          <service behaviorConfiguration="MyServiceBehavior"
                  name="WcfServiceLibrary1.Service1"&gt;
            <endpoint binding="wsHttpBinding"
                      contract="WcfServiceLibrary1.IService1" /&gt;
          </service&gt;
        </services&gt;
      </system.serviceModel&gt;
    </configuration&gt;
    
    {% endhighlight %}

This will hook up the error handler to cover any exceptions thrown by the service. Error handlers can also be debugged via the IDE.

[0]: http://msdn2.microsoft.com/en-us/library/system.servicemodel.dispatcher.ierrorhandler.aspx
[1]: http://www.amazon.com/gp/product/0596101627/ref=cm_cr_pr_product_top
[2]: http://msdn2.microsoft.com/en-us/library/system.servicemodel.dispatcher.ierrorhandler.providefault.aspx
[3]: http://msdn2.microsoft.com/en-us/library/system.servicemodel.dispatcher.ierrorhandler.handleerror.aspx
