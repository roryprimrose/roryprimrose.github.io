---
title: ConnectionStringSettings parameter injection in Unity
tags : Unity
date: 2010-07-05 23:15:00 +10:00
---

I have previously posted about supporting AppSetting value resolution for Unity injection (Unity 1.x [here][0] and Unity 2.0 [here][1]). Today I had a requirement to inject connection string values from application configuration. Like the AppSetting implementation, this creates a nice redirection of injection values as developers and administrators are more familiar with the connection string section in application configuration compared to finding the right value in large amounts of Unity configuration.

This implementation leverages the AppSettingParameterValueExtension class from the prior examples and renames it to SectionExtensionInitiator. This class now configures a Unity section for multiple element extensions rather than a single specific implementation.

    using Microsoft.Practices.Unity.Configuration;
    
    namespace Neovolve.Toolkit.Unity
    {
        public class SectionExtensionInitiator : SectionExtension
        {
            public override void AddExtensions(SectionExtensionContext context)
            {
                if (context == null)
                {
                    return;
                }
     
                context.AddElement<AppSettingsParameterValueElement&gt;(AppSettingsParameterValueElement.ElementName);
                context.AddElement<ConnectionStringParameterValueElement&gt;(ConnectionStringParameterValueElement.ElementName);
            }
        }
    }{% endhighlight %}

The ConnectionStringParameterValueElement is responsible for creating an injection value for a parameter. It can be used to create a parameter for either the String or ConnectionStringSettings types.

    using System;
    using System.Configuration;
    using System.Globalization;
    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.Configuration;
    using Neovolve.Toolkit.Storage;
    using Neovolve.Toolkit.Unity.Properties;
    
    namespace Neovolve.Toolkit.Unity
    {
        public class ConnectionStringParameterValueElement : ParameterValueElement
        {
            public const String ElementName = &quot;connectionSetting&quot;;
    
            public ConnectionStringParameterValueElement()
            {
                Config = ConfigurationStoreFactory.Create();
            }
    
            public ConnectionStringSettings CreateValue()
            {
                ConnectionStringSettings configurationValue = Config.GetConnectionSetting(ConnectionStringKey);
    
                if (configurationValue == null)
                {
                    String message = String.Format(CultureInfo.InvariantCulture, Resources.ConnectionStringParameterValueElement_ConnectionStringKeyNotFound, ConnectionStringKey);
    
                    throw new ConfigurationErrorsException(message);
                }
    
                return configurationValue;
            }
    
            public override InjectionParameterValue GetInjectionParameterValue(IUnityContainer container, Type parameterType)
            {
                if (parameterType == null)
                {
                    throw new ArgumentNullException(&quot;parameterType&quot;);
                }
    
                ConnectionStringSettings injectionValue = CreateValue();
    
                if (parameterType.Equals(typeof(String)))
                {
                    return new InjectionParameter(parameterType, injectionValue.ConnectionString);
                }
                
                if (parameterType.Equals(typeof(ConnectionStringSettings)))
                {
                    return new InjectionParameter(parameterType, injectionValue);
                }
    
                String message = String.Format(CultureInfo.InvariantCulture, Resources.ConnectionStringParameterValueElement_InvalidParameterType, parameterType.FullName);
    
                throw new InvalidOperationException(message);
            }
    
            [ConfigurationProperty(&quot;connectionStringKey&quot;, IsRequired = true)]
            public String ConnectionStringKey
            {
                get
                {
                    return (String)base[&quot;connectionStringKey&quot;];
                }
    
                set
                {
                    base[&quot;connectionStringKey&quot;] = value;
                }
            }
    
            private IConfigurationStore Config
            {
                get;
                set;
            }
        }
    }{% endhighlight %}

Usually a string parameter would be used as the injection type. This will then decouple the dependency from the System.Configuration assembly. The only time that the ConnectionStringSettings class should be used as the injection parameter type is when the provider information on the connection string configuration is required for some application logic.

The Unity configuration needs to be set up with the SectionExtensionInitiator class to support connection string injection. A connectionString element can then be used to define a connection string injection value.

    <?xml version=&quot;1.0&quot;
          encoding=&quot;utf-8&quot; ?&gt;
    <configuration&gt;
      <configSections&gt;
        <section name=&quot;unity&quot;
                 type=&quot;Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration&quot; /&gt;
      </configSections&gt;
      <connectionStrings&gt;
        <add name=&quot;TestConnection&quot; connectionString=&quot;Data Source=localhost;Database=SomeDatabase;Integrated Security=SSPI;&quot;/&gt;
      </connectionStrings&gt;
      <unity&gt;
        <sectionExtension type=&quot;Neovolve.Toolkit.Unity.SectionExtensionInitiator, Neovolve.Toolkit.Unity&quot; /&gt;
        <containers&gt;
          <container&gt;
            <register type=&quot;Neovolve.Toolkit.Unity.IntegrationTests.IDoSomething, Neovolve.Toolkit.Unity.IntegrationTests&quot;
                      mapTo=&quot;Neovolve.Toolkit.Unity.IntegrationTests.ConnectionTest, Neovolve.Toolkit.Unity.IntegrationTests&quot;
                      name=&quot;ConnectionStringTesting&quot;&gt;
              <constructor&gt;
                <param name=&quot;connectionString&quot;&gt;
                  <connectionSetting connectionStringKey=&quot;TestConnection&quot; /&gt;
                </param&gt;
              </constructor&gt;        
            </register&gt;
          </container&gt;
        </containers&gt;
      </unity&gt;
    </configuration&gt;{% endhighlight %}

This configuration will resolve a connection string with the name TestConnection and inject it into the constructor of the ConnectionTest class. 

All the code and documentation for this implementation is in my [Toolkit project][2] on CodePlex.

[0]: /post/2010/01/28/Injecting-AppSetting-values-via-Unity.aspx
[1]: /post/2010/04/23/AppSetting-parameter-injection-in-Unity-2.aspx
[2]: http://neovolve.codeplex.com/SourceControl/changeset/view/61829#1279353
