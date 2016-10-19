---
title: Strict IErrorHandler usage
categories: .Net
tags: WCF
date: 2008-11-07 14:50:00 +10:00
---

I [previously posted][0] about using IErrorHandler implementations for running error handling and exception shielding in WCF services. A few months after that post, [Dave Jansen][1] suggested that there are some risks in leaving it up to configuration to get the error handlers invoked. 

The advantage of using configuration is that the behaviour of the service can be changed in a production environment without necessarily requiring the service itself to be recompiled. The disadvantage is that configuration could be either missing or incorrect. 

Configuration is something that probably shouldn't change independent of the dev/test/release cycle of a service, especially when it comes to error handling and exception shielding. This in itself means that minimal flexibility is lost if the error handler is compiled into the service rather than wired up through configuration. 

<!--more-->

Failure to configure the service correctly for IErrorHandler implementations may pose a risk for two reasons. Firstly, without the intended error handlers being invoked, incorrect exceptions may flow across the service boundary. This occurs because there is no error handling in place. Secondly and more importantly, sensitive information in exceptions may also flow across the service boundary which could be a security risk. This occurs because there is no exception shielding in place. 

An alternative to configuration is to use an attribute implementation to compile IErrorHandler implementations to the service implementation. 

```csharp
[ErrorHandler(typeof(KnownErrorHandler), typeof(UnknownErrorHandler))]
public class TestService : ITestService
{
}
```

In this example, there is a TestService implementation that uses the ITestService contract. The ErrorHandler attribute is used to define two error handlers for the service implementation, being KnownErrorHandler and UnknownErrorHandler. 

Here is the attribute code. 

```csharp
using System; 
using System.Collections.ObjectModel; 
using System.Diagnostics; 
using System.ServiceModel; 
using System.ServiceModel.Channels; 
using System.ServiceModel.Description; 
using System.ServiceModel.Dispatcher; 
namespace Neovolve.Toolkit.Communication 
{ 
    /// <summary> 
    /// The <see cref="ErrorHandlerAttribute"/> 
    /// class is used to decorate WCF service implementations with a 
    /// <see cref="IServiceBehavior"/> that identifies <see cref="IErrorHandler"/> references to invoke 
    /// when the service encounters errors. 
    /// </summary> 
    /// <remarks> 
    /// <para> 
    /// The constructor of the attribute instance takes a <c>params</c> array of strings or types. 
    /// This allows the attribute to attach one or more error handlers to the service implementation. 
    /// </para> 
    /// <para> 
    /// <see cref="IErrorHandler"/> implementations are often set up for services using configuration. 
    /// This may create risks in cases where configuration is not correctly defined or configuration values are missing. 
    /// The result of this may be that exception shielding and error handling are not functioning as intended. 
    /// </para> 
    /// <para> 
    /// Lack of exception shielding may be a security risk because exception information crosses the service boundary 
    /// that was not intended. The details of the exception may contain sensitive information that the consumers of 
    /// the services should not be able to read. 
    /// </para> 
    /// <para> 
    /// Lack of error handling may not be a security risk, but will produce an unexpected behaviour for clients as 
    /// they consume the service. According to the contract, the client may expect to see a particular fault exception 
    /// being raised, but may instead receive a different fault. 
    /// </para> 
    /// <example> 
    /// The following example demonstrates how to use this attribute on a service implementation. 
    /// <code lang="C#"> 
    ///    // In this case, the ErrorHandler attribute is used to assign multiple error handlers 
    ///    [ErrorHandler(typeof(KnownErrorHandler), typeof(UnknownErrorHandler))] 
    ///    public class TestService : ITestService 
    ///    { 
    ///    } 
    /// </code> 
    /// </example> 
    /// </remarks> 
    [AttributeUsage(AttributeTargets.Class)] 
    [CLSCompliant(false)] 
    public sealed class ErrorHandlerAttribute : Attribute, IServiceBehavior 
    { 
        /// <summary> 
        /// Initializes a new instance of the <see cref="ErrorHandlerAttribute"/> class. 
        /// </summary> 
        /// <param name="errorHandlerTypes">The error handler types.</param> 
        public ErrorHandlerAttribute(params Type[] errorHandlerTypes) 
        { 
            const String ErrorHandlerTypeParameterName = "errorHandlerTypes"; 
    
            // Checks whether the errorHandlerType parameter has been supplied 
            if (errorHandlerTypes == null) 
            { 
                throw new ArgumentNullException(ErrorHandlerTypeParameterName); 
            } 
    
            if (errorHandlerTypes.Length == 0) 
            { 
                throw new ArgumentOutOfRangeException(ErrorHandlerTypeParameterName); 
            } 
    
            // Loop through each item supplied 
            for (Int32 index = 0; index < errorHandlerTypes.Length; index++) 
            { 
                Type errorHandlerType = errorHandlerTypes[index]; 
    
                // Check if the item supplied is null 
                if (errorHandlerType == null) 
                { 
                    throw new ArgumentNullException(ErrorHandlerTypeParameterName); 
                } 
            } 
    
            // Store the types 
            ErrorHandlerTypes = errorHandlerTypes; 
            ValidateHandlerTypes(); 
        } 
    
        /// <summary> 
        /// Initializes a new instance of the <see cref="ErrorHandlerAttribute"/> class. 
        /// </summary> 
        /// <param name="errorHandlerTypeNames">The error handler type names.</param> 
        public ErrorHandlerAttribute(params String[] errorHandlerTypeNames) 
        { 
            const String ErrorHandlerTypeNamesParameterName = "errorHandlerTypeNames"; 
    
            // Checks whether the errorHandlerTypeName parameter has been supplied 
            if (errorHandlerTypeNames == null) 
            { 
                throw new ArgumentNullException(ErrorHandlerTypeNamesParameterName); 
            } 
    
            if (errorHandlerTypeNames.Length == 0) 
            { 
                throw new ArgumentOutOfRangeException(ErrorHandlerTypeNamesParameterName); 
            } 
    
            // Loop through each item supplied 
            for (Int32 index = 0; index < errorHandlerTypeNames.Length; index++) 
            { 
                String errorHandlerTypeName = errorHandlerTypeNames[index]; 
    
                // Ensure that a value has been supplied 
                if (String.IsNullOrEmpty(errorHandlerTypeName)) 
                { 
                    throw new ArgumentNullException(ErrorHandlerTypeNamesParameterName); 
                } 
            } 
    
            ErrorHandlerTypes = new Type[errorHandlerTypeNames.Length]; 
    
            // Loop through each type 
            for (Int32 index = 0; index < errorHandlerTypeNames.Length; index++) 
            { 
                // Get a reference to the type 
                String errorHandlerTypeName = errorHandlerTypeNames[index]; 
                Type handlerType = Type.GetType(errorHandlerTypeName, true, true); 
    
                ErrorHandlerTypes[index] = handlerType; 
            } 
    
            ValidateHandlerTypes(); 
        } 
    
        /// <summary> 
        /// Provides the ability to pass custom data to binding elements to support the contract implementation. 
        /// </summary> 
        /// <param name="serviceDescription">The service description of the service.</param> 
        /// <param name="serviceHostBase">The host of the service.</param> 
        /// <param name="endpoints">The service endpoints.</param> 
        /// <param name="bindingParameters">Custom objects to which binding elements have access.</param> 
        public void AddBindingParameters( 
            ServiceDescription serviceDescription, 
            ServiceHostBase serviceHostBase, 
            Collection<ServiceEndpoint> endpoints, 
            BindingParameterCollection bindingParameters) 
        { 
            // Nothing to do here 
        } 
    
        /// <summary> 
        /// Provides the ability to change run-time property values or insert custom extension objects such as error handlers, message or parameter interceptors, security extensions, and other custom extension objects. 
        /// </summary> 
        /// <param name="serviceDescription">The service description.</param> 
        /// <param name="serviceHostBase">The host that is currently being built.</param> 
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) 
        { 
            // Loop through each channel dispatcher 
            for (Int32 dispatcherIndex = 0; 
                dispatcherIndex < serviceHostBase.ChannelDispatchers.Count; 
                dispatcherIndex++) 
            { 
                // Get the dispatcher for this index and cast to the type we are after 
                ChannelDispatcher dispatcher = serviceHostBase.ChannelDispatchers[dispatcherIndex] as ChannelDispatcher; 
                Debug.Assert(dispatcher != null, "The dispatcher collection returned an item of an incorrect type"); 
    
                // Loop through each error handler 
                for (Int32 typeIndex = 0; typeIndex < ErrorHandlerTypes.Length; typeIndex++) 
                { 
                    Type errorHandlerType = ErrorHandlerTypes[typeIndex]; 
    
                    // Create a new error handler instance 
                    IErrorHandler handler = Activator.CreateInstance(errorHandlerType) as IErrorHandler; 
                    Debug.Assert(handler != null, "Failed to create the IErrorHandler instance"); 
    
                    // Add the handler to the dispatcher 
                    dispatcher.ErrorHandlers.Add(handler); 
                } 
            } 
        } 
    
        /// <summary> 
        /// Provides the ability to inspect the service host and the service description to confirm that the service can run successfully. 
        /// </summary> 
        /// <param name="serviceDescription">The service description.</param> 
        /// <param name="serviceHostBase">The service host that is currently being constructed.</param> 
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) 
        { 
            // Nothing to do here 
        } 
    
        /// <summary> 
        /// Validates the handler types. 
        /// </summary> 
        private void ValidateHandlerTypes() 
        { 
            Debug.Assert(ErrorHandlerTypes != null, "No ErrorHandlerTypes is null"); 
            Debug.Assert(ErrorHandlerTypes.Length > 0, "No error handler types are available"); 
    
            // Loop through each handler 
            for (Int32 index = 0; index < ErrorHandlerTypes.Length; index++) 
            { 
                Type errorHandlerType = ErrorHandlerTypes[index]; 
    
                // Check if the type doesn't define the IErrorHandler interface 
                if (typeof(IErrorHandler).IsAssignableFrom(errorHandlerType) == false) 
                { 
                    // We can't use this type 
                    throw new InvalidCastException(); 
                } 
            } 
        } 
    
        /// <summary> 
        /// Gets or sets the error handler types. 
        /// </summary> 
        /// <value>The error handler types.</value> 
        private Type[] ErrorHandlerTypes 
        { 
            get; 
            set; 
        } 
    } 
} 
```

This attribute can be found in the Neovolve.Toolkit project in the [Neovolve][2] project on CodePlex. The Toolkit binary in the project downloads will include this attribute in the near future. Until then, you can access the code either here or from the CodePlex site. 

[0]: /2008/04/07/implementing-ierrorhandler/
[1]: http://dansen.wordpress.com/
[2]: http://www.codeplex.com/neovolve
