---
title: Unity dependency injections for WCF services – Part 2
categories: .Net, Applications
tags: Dependency Injection, Unity, WCF, WIF
date: 2010-05-17 21:23:00 +10:00
---

In [Part 1][0], I described the code used to leverage ServiceHostFactory to create WCF service instances with dependency injection via Unity. Using a custom ServiceHostFactory is a really easy way to get dependency injection in WCF but comes with two drawbacks. The standard Unity configuration section name (“unity”) must be used and named Unity containers are not supported. Most applications will work within these constraints. 

Using a configuration based service behavior is the answer to situations where these constraints are a problem. The UnityServiceElement class is used to define the configuration support for the UnityServiceBehavior. As outlined in the last post, the UnityServiceBehavior class is used to assign a custom instance provider to each endpoint in the service. The instance provider is used to resolve a service instance from a Unity container.

<!--more-->

The UnityServiceElement class looks like the following. The UnityServiceElement calls into a UnityContainerResolver helper class to resolve the unity container. The code for UnityContainerResolver can be found [here][1].

```csharp
using System; 
using System.Configuration;
using System.Diagnostics.Contracts;
using System.ServiceModel.Configuration;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity
{
    public class UnityServiceElement : BehaviorExtensionElement
    {
        public const String UnitySectionNameAttributeName = "unitySectionName";
    
        private const String UnityContainerNameAttributeName = "unityContainerName";
    
        private ConfigurationPropertyCollection _properties;
    
        public override void CopyFrom(ServiceModelExtensionElement from)
        {
            Contract.Requires<ArgumentException>(from is UnityServiceElement, "The from parameter is not of type UnityServiceElement.");
    
            base.CopyFrom(from);
    
            UnityServiceElement element = (UnityServiceElement)from;
    
            UnitySectionName = element.UnitySectionName;
            UnityContainerName = element.UnityContainerName;
        }
    
        protected override Object CreateBehavior()
        {
            IUnityContainer container = UnityContainerResolver.Resolve(null, UnitySectionName, UnityContainerName);
    
            return new UnityServiceBehavior(container);
        }
    
        public override Type BehaviorType
        {
            get
            {
                return typeof(UnityServiceBehavior);
            }
        }
    
        [ConfigurationProperty(UnityContainerNameAttributeName)]
        public String UnityContainerName
        {
            get
            {
                return (String)base[UnityContainerNameAttributeName];
            }
    
            set
            {
                base[UnityContainerNameAttributeName] = value;
            }
        }
    
        [ConfigurationProperty(UnitySectionNameAttributeName)]
        public String UnitySectionName
        {
            get
            {
                return (String)base[UnitySectionNameAttributeName];
            }
    
            set
            {
                base[UnitySectionNameAttributeName] = value;
            }
        }
    
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    ConfigurationPropertyCollection propertys = new ConfigurationPropertyCollection();
    
                    propertys.Add(
                        new ConfigurationProperty(
                            UnitySectionNameAttributeName, typeof(String), String.Empty, null, null, ConfigurationPropertyOptions.None));
    
                    propertys.Add(
                        new ConfigurationProperty(
                            UnityContainerNameAttributeName, typeof(String), String.Empty, null, null, ConfigurationPropertyOptions.None));
    
                    _properties = propertys;
                }
    
                return _properties;
            }
        }
    }
}
```

The configuration to hook this behavior up to a service looks like this.

```xml
<?xml version="1.0" ?>
<configuration>
    <configSections>
        <section name="customUnitySection"
                    type="Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration"/>
    </configSections>
    <customUnitySection>
        <containers>
            <container name="customContainer">
                <register type="Neovolve.Toolkit.Unity.WebIntegrationTests.TestService, Neovolve.Toolkit.Unity.WebIntegrationTests">
                    <constructor>
                        <param name="prefix">
                            <value value="Injected by custom unity section and container"/>
                        </param>
                    </constructor>
                </register>
            </container>
        </containers>
    </customUnitySection>
    <system.serviceModel>
        <behaviors>
            <serviceBehaviors>
                <behavior name="UnityBehavior">
                    <unityService unitySectionName="customUnitySection"
                                    unityContainerName="customContainer"/>
                </behavior>
            </serviceBehaviors>
        </behaviors>
        <extensions>
            <behaviorExtensions>
                <add name="unityService"
                        type="Neovolve.Toolkit.Unity.UnityServiceElement, Neovolve.Toolkit.Unity, Version=1.0.0.0, Culture=neutral, PublicKeyToken=911824a9aa319cb2"/>
            </behaviorExtensions>
        </extensions>
        <services>
            <service behaviorConfiguration="UnityBehavior"
                        name="Neovolve.Toolkit.Unity.WebIntegrationTests.TestService">
                <endpoint address=""
                            binding="basicHttpBinding"
                            bindingConfiguration=""
                            contract="Neovolve.Toolkit.Unity.WebIntegrationTests.ITestService"/>
            </service>
        </services>
    </system.serviceModel>
</configuration>
```

The configuration for the element shown here allows for defining custom configuration section and container names for resolving the Unity container.

## ServiceHostFactory or configured service behavior

Which one to use?

If you have multiple containers, or a single container that must have a name, then you have no choice but to use the service behavior element as outlined in this post. The only other reason for using the behavior element is if you must use another type of custom factory (such as the [WIF WSTrustServiceHostFactory][2]). There really should be no reason to use a non-standard configuration section name for Unity so I don’t think that is a big factor.

Using the ServiceHostFactory is the preferred option as it will result in a slightly cleaner configuration if you are not forced into using a configured service behavior,

The code with xml comments can be found in my [Toolkit project][3] on CodePlex.

[0]: /2010/05/15/unity-dependency-injection-for-wcf-services-e28093-part-1/
[1]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1195163
[2]: http://msdn.microsoft.com/en-us/library/microsoft.identitymodel.protocols.wstrust.wstrustservicehostfactory.aspx
[3]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1216930
