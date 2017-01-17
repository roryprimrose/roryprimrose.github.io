---
title: Configuration support for custom IErrorHandler in WCF
categories: .Net
tags: WCF
date: 2010-10-19 10:20:54 +10:00
---

My post about [implementing IErrorHandler for WCF][0] a few years ago is my second top post on this site. My [Toolkit project][1] on Codeplex has had support for hooking up an [IErrorHandler using an attribute][2] on the service implementation class which is an extension of the original post.

My preference has always been to hook up IErrorHandler using an attribute to avoid any potential security holes. This would be a scenario where the configuration for IErrorHandler is removed and exception shielding is no longer available to prevent potentially sensitive information from being displayed to clients. I am now playing with workflow services and am not able to use an attribute for this purpose. I no longer have a choice and must use a configuration based IErrorHandler implementation.

<!--more-->

I have added configuration support for IErrorHandler to my Toolkit project based on the original posts above to assist with this process.

```csharp
namespace Neovolve.Toolkit.Communication
{
    using System;
    using System.Configuration;
    using System.ServiceModel.Configuration;
    
    public class ErrorHandlerElement : BehaviorExtensionElement
    {
        public const String ErrorHandlerTypeAttributeName = &quot;type&quot;;
    
        private ConfigurationPropertyCollection _properties;
    
        protected override Object CreateBehavior()
        {
            return new ErrorHandlerAttribute(ErrorHandlerType);
        }
    
        public override Type BehaviorType
        {
            get
            {
                return typeof(ErrorHandlerAttribute);
            }
        }
    
        [ConfigurationProperty(ErrorHandlerTypeAttributeName)]
        public String ErrorHandlerType
        {
            get
            {
                return (String)base[ErrorHandlerTypeAttributeName];
            }
    
            set
            {
                base[ErrorHandlerTypeAttributeName] = value;
            }
        }
    
        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection();
    
                    properties.Add(
                        new ConfigurationProperty(
                            ErrorHandlerTypeAttributeName, typeof(String), String.Empty, null, null, ConfigurationPropertyOptions.IsRequired));
    
                    _properties = properties;
                }
    
                return _properties;
            }
        }
    }
}
```

The ErrorHandlerElement class allows for WCF configuration to configure an IErrorHandler for a service. This class provides the configuration support to define the type of error handler to use. The CreateBehavior method simply forwards the configured IErrorHandler type to the ErrorHandlerAttribute class that is already in the toolkit.

```xml
<?xml version=&quot;1.0&quot; ?>
<configuration>
    <system.serviceModel>
        <behaviors>
            <serviceBehaviors>
                <behavior name=&quot;ErrorHandlerBehavior&quot;>
                    <errorHandler type=&quot;Neovolve.Toolkit.IntegrationTests.Communication.KnownErrorHandler, Neovolve.Toolkit.IntegrationTests&quot;/>
                </behavior>
            </serviceBehaviors>
        </behaviors>
        <extensions>
            <behaviorExtensions>
                <add name=&quot;errorHandler&quot;
                        type=&quot;Neovolve.Toolkit.Communication.ErrorHandlerElement, Neovolve.Toolkit&quot;/>
            </behaviorExtensions>
        </extensions>
        <services>
            <service behaviorConfiguration=&quot;ErrorHandlerBehavior&quot;
                        name=&quot;Neovolve.Toolkit.IntegrationTests.Communication.TestService&quot;>
                <endpoint address=&quot;&quot;
                            binding=&quot;basicHttpBinding&quot;
                            bindingConfiguration=&quot;&quot;
                            contract=&quot;Neovolve.Toolkit.IntegrationTests.Communication.ITestService&quot;/>
            </service>
        </services>
    </system.serviceModel>
</configuration> 
```

The WCF configuration defines the ErrorHandlerElement class as a behavior extension. This service behavior can then use this extension and identify the type of IErrorHandler to hook into the service.

This class has been added into the next beta of my Toolkit. You can get it from [here][3].

[0]: /2008/04/07/implementing-ierrorhandler/
[1]: http://neovolve.codeplex.com
[2]: /2008/11/07/strict-ierrorhandler-usage/
[3]: http://neovolve.codeplex.com/releases/view/53499
