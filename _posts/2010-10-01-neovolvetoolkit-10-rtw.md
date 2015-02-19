---
title: Neovolve.Toolkit 1.0 RTW
categories : .Net, Applications, My Software
tags : ASP.Net, Tracing, Unity, WCF
date: 2010-10-01 15:15:00 +10:00
---

I have finally marked my Neovolve.Toolkit project as stable for version 1.0. It includes the recent work I have done for WF4. The toolkit comes with the binaries, a chm help file for documentation information and xml comment files for intellisense in Visual Studio. 

You can [download the toolkit][0] from the project on Codeplex.

The following tables outline the types available in the namespaces across the toolkit assemblies. The information here is copied from the compiled help file.

**Neovolve.Toolkit.dll**

Neovolve.Toolkit.CommunicationName 

Description 

ChannelProxyHandler<T&gt; 

The ChannelProxyHandler<T&gt; class is used to provide a proxy implementation for a WCF service channel. 

DefaultProxyHandler<T&gt; 

The DefaultProxyHandler<T&gt; class is used to provide a default handler for invoking methods on a type. 

ErrorHandlerAttribute 

The ErrorHandlerAttribute class is used to decorate WCF service implementations with a IServiceBehavior that identifies IErrorHandler references to invoke when the service encounters errors. 

ProxyHandler<T&gt; 

The ProxyHandler<T&gt; class is used to provide the base logic for managing the execution of methods on a proxy. 

ProxyManager<T&gt; 

The ProxyManager<T&gt; class is used to manage invocations of proxy objects. 

Neovolve.Toolkit.Communication.SecurityName 

Description 

DefaultPasswordValidator 

The DefaultPasswordValidator class provides a user name password validation implementation that ensures that a user name and password value have been supplied. 

OptionalPasswordValidator 

The OptionalPasswordValidator class provides a user name password validation implementation that ensures that a user name value has been supplied. 

PasswordIdentity 

The PasswordIdentity class provides an IIdentity that exposes the password related to the username. 

PasswordPrincipal 

The PasswordPrincipal class provides information about the roles available to the PasswordIdentity that it exposes. 

PasswordServiceCredentials 

The PasswordServiceCredentials class provides a username password security implementation for WCF services. It will generate a PasswordPrincipal containing a PasswordIdentity that exposes the password of the client credentials. 

Neovolve.Toolkit.InstrumentationName 

Description 

ActivityTrace 

The ActivityTrace class is used to trace related sets of activities in applications. 

ConfigurationResolver 

The ConfigurationResolver class is used to resolve a collection of TraceSource instances from application configuration. 

MemberTrace 

The MemberTrace class is used to provide activity tracing functionality for methods that declare them. 

RecordTrace 

The RecordTrace class is used to trace record information. 

TraceSourceLoadException 

The TraceSourceLoadException class is used to identify scenarios where a TraceSource is not retrieved for use by a RecordTrace instance. 

TraceSourceResolverFactory 

The TraceSourceResolverFactory class is used to create an instance of a ITraceSourceResolver. 

IActivityWriter 

The IActivityWriter interface is used to define how instrumentation records are written. 

IRecordWriter 

The IRecordWriter interface defines the methods for writing instrumentation records. 

ITraceSourceResolver 

The ITraceSourceResolver interface is used to resolve a collection of TraceSource instances. 

ActivityTraceState 

The ActivityTraceState enum is used to define the state of a ActivityTrace instance. 

RecordType 

The RecordType enum is used to define the type of record created. 

Neovolve.Toolkit.ReflectionName 

Description 

MethodResolver 

The MethodResolver class resolves MethodInfo instances of types and caches results for faster access. 

TypeResolver 

The TypeResolver class is used to resolve types from configuration mapping information. 

Neovolve.Toolkit.StorageName 

Description 

AbsoluteExpirationPolicy 

The AbsoluteExpirationPolicy class is used to define an absolute time when a cache item is to expire. 

AspNetCacheStore 

The AspNetCacheStore class is used to provide a ICacheStore implementation that leverages a Cache instance. 

CacheStoreFactory 

The CacheStoreFactory class is used to create ICacheStore instances. 

ConfigurationManagerStore 

The ConfigurationManagerStore class is used to provide a IConfigurationStore implementation based on the ConfigurationManager class. 

ConfigurationStoreFactory 

The ConfigurationStoreFactory class is used to create IConfigurationStore instances. 

DictionaryCacheStore 

The DictionaryCacheStore class is used to provide a ICacheStore implementation that leverages a Dictionary<TKey, TValue&gt; instance. 

ExpirationCacheStoreBase 

The ExpirationCacheStoreBase class is used to provide the base cache store implementation that handles expiration policies. 

RelativeExpirationPolicy 

The RelativeExpirationPolicy class is used to define a relative time when a cache item is to expire. 

ICacheStore 

The ICacheStore interface defines the methods used to read and write to a cache store. 

IConfigurationStore 

The IConfigurationStore interface defines the methods used to read and write to a configuration store. 

IExpirationPolicy 

The IExpirationPolicy interface is used to define how a cache item expiration policy is evaluated in order to determine whether the item should be removed from the cache. 

Neovolve.Toolkit.ThreadingName 

Description 

LockReader 

The LockReader class is used to provide thread safe read access to a resource using a provided ReaderWriterLock or ReaderWriterLockSlim instance. 

LockWriter 

The LockWriter class is used to provide thread safe write access to a resource using a provided ReaderWriterLock or ReaderWriterLockSlim instance. 

**Neovolve.Toolkit.Unity.dll**Name 

Description 

AppSettingsParameterValueElement 

The AppSettingsParameterValueElement class is used to configure a Unity injection parameter value to be determined from an AppSettings value. 

ConnectionStringParameterValueElement 

The ConnectionStringParameterValueElement class is used to configure a Unity injection parameter value to be determined from a ConnectionStringSettings value. 

DisposableStrategyExtension 

The DisposableStrategyExtension class is used to define the build strategy for disposing objects on tear down by a IUnityContainer. 

ProxyInjectionParameterValue 

The ProxyInjectionParameterValue class is used to provide the parameter value information for a proxy injection parameter. 

ProxyParameterValueElement 

The ProxyParameterValueElement class is used to configure a Unity parameter value to be determined from a proxy value created by ProxyManager<T&gt;. 

SectionExtensionInitiator 

The SectionExtensionInitiator class is used to initiate a SectionExtension with configuration element support for custom parameter injection values. 

UnityContainerResolver 

The UnityContainerResolver class is used to resolve a IUnityContainer instance from configuration. 

UnityControllerFactoryHttpModule 

The UnityControllerFactoryHttpModule class is used to build up ASP.Net MVC controller instances using an IUnityContainer. 

UnityHttpModule 

The UnityHttpModule class is used to build up ASP.Net pages with property and method injection after they are created but before they are used for request processing. 

UnityHttpModuleBase 

The UnityHttpModuleBase class is used to provide management of a global unity container for IHttpModule instances. 

UnityServiceBehavior 

The UnityServiceBehavior class is used to provide a service behavior for configuring unity injection in WCF. 

UnityServiceElement 

The UnityServiceElement class is used to provide configuration support for defining a unity container via a service behavior. 

UnityServiceHostFactory 

The UnityServiceHostFactory class is used to create a ServiceHost instance that supports creating service instances with Unity. 

**Neovolve.Toolkit.Workflow.dll**

Neovolve.Toolkit.WorkflowName 

Description 

ActivityFailureException 

The ActivityFailureException class is used to describe a failure in the execution of a workflow activity . 

ActivityInvoker 

The ActivityInvoker class is used to invoke activities. 

ActivityStore 

The ActivityStore class is used to cache activity instances for reuse. 

InstanceHandler<T&gt; 

The InstanceHandler<T&gt; class is used to provide instance handling logic for a InstanceResolver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16&gt; instance. 

ResumeBookmarkContext 

The ResumeBookmarkContext class is used to resume a workflow from a bookmark. 

ResumeBookmarkContext<T&gt; 

The ResumeBookmarkContext<T&gt; class is used to define the information required to resume a workflow bookmark. 

GenericArgumentCount 

The GenericArgumentCount enum defines the number of generic arguments available for an activity type. 

Neovolve.Toolkit.Workflow.ActivitiesName 

Description 

ExecuteBookmark 

The ExecuteBookmark class is a workflow activity that is used to process bookmarks. 

ExecuteBookmark<T&gt; 

The ExecuteBookmark<T&gt; class is a workflow activity that is used to process bookmarks. 

GetWorkflowInstanceId 

The GetWorkflowInstanceId class is used to obtain the instance id of the executing workflow. 

InstanceResolver 

The InstanceResolver class is used to provide a resolved instance for a child activity. 

InstanceResolver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16 &gt; 

The InstanceResolver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16 &gt; class is used to provide resolution of instances for workflow activities. 

SystemFailureEvaluator 

The SystemFailureEvaluator class is used to evaluate a condition to determine whether a system failure has occurred. ![][1]

The icons used for these activities come from the fabulous [famfamfam Silk Icon][2] collection.

Neovolve.Toolkit.Workflow.ExtensionsName 

Description 

InstanceManagerExtension 

The InstanceManagerExtension class is used to manage instances resolved from a container. 

[0]: http://neovolve.codeplex.com/releases/view/19004
[1]: //files/image_38.png
[2]: http://www.famfamfam.com/lab/icons/silk/
