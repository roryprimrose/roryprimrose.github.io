---
title: Unity dependency injection for WCF services – Part 1
categories: .Net
tags: Dependency Injection, Unity, WCF
date: 2010-05-15 15:26:11 +10:00
---

There are a few ways that Unity can be used to construct WCF service instances. The options are poor mans injection, service host factories and configured behaviours.

The poor mans injection option is to create a unity container within the constructor of the service type and resolve dependencies from within the service instance. This is not elegant and couples the IOC implementation (Unity in this case) to the service.

The second option is to create a custom service host factory. This option allows a service type to be created with constructor dependencies rather than a default constructor. It uses the mark-up in svc files to identify the custom factory to be used rather than the default one. The 4.0 framework also supports this configuration via web.config as svc files are no longer required.

<!--more-->

The code goes a bit like this. The UnityServiceHostFactory class is used to define how to create a service host to host the service endpoints. The factory calls into a UnityContainerResolver helper class to resolve the unity container. The code for UnityContainerResolver can be found [here][0].

{% highlight csharp %}
using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using Microsoft.Practices.Unity;
using Neovolve.Toolkit.Storage;
    
namespace Neovolve.Toolkit.Unity
{
    public class UnityServiceHostFactory : ServiceHostFactory
    {
        public UnityServiceHostFactory()
            : this(null, String.Empty, String.Empty)
        {
        }
    
        public UnityServiceHostFactory(IConfigurationStore configuration, String unitySectionName, String containerName)
        {
            Container = UnityContainerResolver.Resolve(configuration, unitySectionName, containerName);
        }
    
        public UnityServiceHostFactory(IUnityContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
    
            Container = container;
        }
    
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new UnityServiceHost(serviceType, Container, baseAddresses);
        }
    
        public IUnityContainer Container
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The configuration for the factory via svc mark-up is the following.

{% highlight xml %}
<%@ ServiceHost 
    Language="C#" 
    Debug="true" 
    Service="Neovolve.Toolkit.Unity.WebIntegrationTests.TestService" 
    Factory="Neovolve.Toolkit.Unity.UnityServiceHostFactory" %>
{% endhighlight %}

The configuration via web.config (.net 4.0 only) is like this. This configuration also includes an example Unity container configuration.

{% highlight xml %}
<configuration>
    <configSections>
        <section name="unity"
                    type="Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration"/>
    </configSections>
    <unity>
        <containers>
            <container>
                <register type="Neovolve.Toolkit.Unity.WebIntegrationTests.TestService, Neovolve.Toolkit.Unity.WebIntegrationTests">
                    <constructor>
                        <param name="prefix">
                            <value value="Injected by default unity section and container"/>
                        </param>
                    </constructor>
                </register>
            </container>
        </containers>
    </unity>
    <system.serviceModel>
        <serviceHostingEnvironment>
            <serviceActivations>
                <add service="Neovolve.Toolkit.Unity.WebIntegrationTests.TestService" 
                        factory="Neovolve.Toolkit.Unity.UnityServiceHostFactory" 
                        relativeAddress="/TestService.svc" />
            </serviceActivations>
        </serviceHostingEnvironment>
    </system.serviceModel>
</configuration>
{% endhighlight %}

The UnityServiceHost class is used to configure the host for a behaviour that is used to create the service instances.

{% highlight csharp %}
using System;
using System.ServiceModel;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity
{
    internal class UnityServiceHost : ServiceHost
    {
        public UnityServiceHost(Type serviceType, IUnityContainer container, params Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
    
            Container = container;
        }
    
        protected override void OnOpening()
        {
            if (Description.Behaviors.Find<UnityServiceBehavior>() == null)
            {
                Description.Behaviors.Add(new UnityServiceBehavior(Container));
            }
    
            base.OnOpening();
        }
    
        protected IUnityContainer Container
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The UnityServiceBehavior is used assign a new instance provider to each endpoint in the service host.

{% highlight csharp %}
using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity
{
    internal class UnityServiceBehavior : IServiceBehavior
    {
        public UnityServiceBehavior(IUnityContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
    
            Container = container;
        }
    
        public void AddBindingParameters(
            ServiceDescription serviceDescription, 
            ServiceHostBase serviceHostBase, 
            Collection<ServiceEndpoint> endpoints, 
            BindingParameterCollection bindingParameters)
        {
        }
    
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            if (serviceDescription == null)
            {
                throw new ArgumentNullException("serviceDescription");
            }
    
            if (serviceHostBase == null)
            {
                throw new ArgumentNullException("serviceHostBase");
            }
    
            for (Int32 dispatcherIndex = 0; dispatcherIndex < serviceHostBase.ChannelDispatchers.Count; dispatcherIndex++)
            {
                ChannelDispatcherBase dispatcher = serviceHostBase.ChannelDispatchers[dispatcherIndex];
                ChannelDispatcher channelDispatcher = (ChannelDispatcher)dispatcher;
    
                for (Int32 endpointIndex = 0; endpointIndex < channelDispatcher.Endpoints.Count; endpointIndex++)
                {
                    EndpointDispatcher endpointDispatcher = channelDispatcher.Endpoints[endpointIndex];
    
                    endpointDispatcher.DispatchRuntime.InstanceProvider = new UnityInstanceProvider(Container, serviceDescription.ServiceType);
                }
            }
        }
    
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
    
        protected IUnityContainer Container
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The UnityInstanceProvider is used to resolve an instance from a Unity container using the service type defined for an endpoint. This is where the real work happens to create a service instance with injected dependencies. This implementation also calls the Unity container to destroy the service instance according to its configuration.

{% highlight csharp %}
using System;
using System.Configuration;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity
{
    internal class UnityInstanceProvider : IInstanceProvider
    {
        public UnityInstanceProvider(IUnityContainer container, Type serviceType)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }
    
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }
    
            Container = container;
            ServiceType = serviceType;
        }
    
        public Object GetInstance(InstanceContext instanceContext)
        {
            return GetInstance(instanceContext, null);
        }
    
        public Object GetInstance(InstanceContext instanceContext, Message message)
        {
            Object instance = Container.Resolve(ServiceType);
    
            if (instance == null)
            {
                const String MessageFormat = "No unity configuration was found for service type '{0}'";
                String failureMessage = String.Format(CultureInfo.InvariantCulture, MessageFormat, ServiceType.FullName);
    
                throw new ConfigurationErrorsException(failureMessage);
            }
    
            return instance;
        }
    
        public void ReleaseInstance(InstanceContext instanceContext, Object instance)
        {
            Container.Teardown(instance);
        }
    
        protected Type ServiceType
        {
            get;
            set;
        }
    
        private IUnityContainer Container
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

That’s it for the custom service host factory code. The one disadvantage with this technique is that there is no configuration support for ServiceHostFactory implementations. The UnityServiceHostFactory is restricted to resolving the default (un-named) container using the standard unity section name of “unity”. If this restriction is not suitable, then a custom UnityServiceBehavior must be used.

Part 2 will look at leveraging the UnityServiceBehavior directly via configuration rather than going through a factory.

The code with xml comments can be found in my [Tookit project][1] on CodePlex.

[0]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1195163
[1]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1149392
