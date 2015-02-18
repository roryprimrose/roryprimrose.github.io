---
title: Unity dependency injections for WCF services – Part 2
categories : .Net, Applications
tags : Dependency Injection, Unity, WCF, WIF
date: 2010-05-17 21:23:00 +10:00
---

In [Part 1][0], I described the code used to leverage ServiceHostFactory to create WCF service instances with dependency injection via Unity. Using a custom ServiceHostFactory is a really easy way to get dependency injection in WCF but comes with two drawbacks. The standard Unity configuration section name (“unity”) must be used and named Unity containers are not supported. Most applications will work within these constraints. 

Using a configuration based service behavior is the answer to situations where these constraints are a problem. The UnityServiceElement class is used to define the configuration support for the UnityServiceBehavior. As outlined in the last post, the UnityServiceBehavior class is used to assign a custom instance provider to each endpoint in the service. The instance provider is used to resolve a service instance from a Unity container.

The UnityServiceElement class looks like the following. The UnityServiceElement calls into a UnityContainerResolver helper class to resolve the unity container. The code for UnityContainerResolver can be found [here][1].

    using System; 
    using System.Configuration;
    using System.Diagnostics.Contracts;
    using System.ServiceModel.Configuration;
    using Microsoft.Practices.Unity;
    
    namespace Neovolve.Toolkit.Unity
    {
        public class UnityServiceElement : BehaviorExtensionElement
        {
            public const String UnitySectionNameAttributeName = &quot;unitySectionName&quot;;
    
            private const String UnityContainerNameAttributeName = &quot;unityContainerName&quot;;
    
            private ConfigurationPropertyCollection _properties;
    
            public override void CopyFrom(ServiceModelExtensionElement from)
            {
                Contract.Requires<ArgumentException&gt;(from is UnityServiceElement, &quot;The from parameter is not of type UnityServiceElement.&quot;);
    
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
    }{% endhighlight %}

The configuration to hook this behavior up to a service looks like this.

    <?xml version=&quot;1.0&quot; ?&gt;
    <configuration&gt;
        <configSections&gt;
            <section name=&quot;customUnitySection&quot;
                     type=&quot;Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration&quot;/&gt;
        </configSections&gt;
        <customUnitySection&gt;
            <containers&gt;
                <container name=&quot;customContainer&quot;&gt;
                    <register type=&quot;Neovolve.Toolkit.Unity.WebIntegrationTests.TestService, Neovolve.Toolkit.Unity.WebIntegrationTests&quot;&gt;
                        <constructor&gt;
                            <param name=&quot;prefix&quot;&gt;
                                <value value=&quot;Injected by custom unity section and container&quot;/&gt;
                            </param&gt;
                        </constructor&gt;
                    </register&gt;
                </container&gt;
            </containers&gt;
        </customUnitySection&gt;
        <system.serviceModel&gt;
            <behaviors&gt;
                <serviceBehaviors&gt;
                    <behavior name=&quot;UnityBehavior&quot;&gt;
                        <unityService unitySectionName=&quot;customUnitySection&quot;
                                      unityContainerName=&quot;customContainer&quot;/&gt;
                    </behavior&gt;
                </serviceBehaviors&gt;
            </behaviors&gt;
            <extensions&gt;
                <behaviorExtensions&gt;
                    <add name=&quot;unityService&quot;
                         type=&quot;Neovolve.Toolkit.Unity.UnityServiceElement, Neovolve.Toolkit.Unity, Version=1.0.0.0, Culture=neutral, PublicKeyToken=911824a9aa319cb2&quot;/&gt;
                </behaviorExtensions&gt;
            </extensions&gt;
            <services&gt;
                <service behaviorConfiguration=&quot;UnityBehavior&quot;
                         name=&quot;Neovolve.Toolkit.Unity.WebIntegrationTests.TestService&quot;&gt;
                    <endpoint address=&quot;&quot;
                              binding=&quot;basicHttpBinding&quot;
                              bindingConfiguration=&quot;&quot;
                              contract=&quot;Neovolve.Toolkit.Unity.WebIntegrationTests.ITestService&quot;/&gt;
                </service&gt;
            </services&gt;
        </system.serviceModel&gt;
    </configuration&gt;{% endhighlight %}

The configuration for the element shown here allows for defining custom configuration section and container names for resolving the Unity container.

**ServiceHostFactory or configured service behavior**

Which one to use?

If you have multiple containers, or a single container that must have a name, then you have no choice but to use the service behavior element as outlined in this post. The only other reason for using the behavior element is if you must use another type of custom factory (such as the [WIF WSTrustServiceHostFactory][2]). There really should be no reason to use a non-standard configuration section name for Unity so I don’t think that is a big factor.

Using the ServiceHostFactory is the preferred option as it will result in a slightly cleaner configuration if you are not forced into using a configured service behavior,

The code with xml comments can be found in my [Toolkit project][3] on CodePlex.

[0]: /post/2010/05/15/Unity-dependency-injection-for-WCF-services-e28093-Part-1.aspx
[1]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1195163
[2]: http://msdn.microsoft.com/en-us/library/microsoft.identitymodel.protocols.wstrust.wstrustservicehostfactory.aspx
[3]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1216930
