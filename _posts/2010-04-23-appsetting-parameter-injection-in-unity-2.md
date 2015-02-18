---
title: AppSetting parameter injection in Unity 2
categories : .Net
tags : Unity
date: 2010-04-23 12:26:00 +10:00
---

I [recently posted][0] about how to resolve appSetting values for parameter injection in Unity. After seeing the news yesterday ([here][1] and [here][2]) of Unity 2 being released with EntLib 5, I decided to revisit this functionality to adopt the latest and greatest.

There are some issues with adopting Unity 2 in the short-term. Unity 2 isn’t actually released by itself (as [noted][3] in a comment by [Grigori Melnik][4]), although it is bundled with EntLib 5. This means that there is no documentation for the changes found in the new version of Unity. The EntLib documentation for Unity simply says that “Content for this topic is not yet available”.

Given these constraints, I have a working solution after several hours of surfing Reflector.

Firstly, the configuration for Unity 2 is much neater and removes redundant configuration. The feature I like the most is that type information is now interpreted where previously it needed to be specified.

    <?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
    <configuration&gt;
     
      <configSections&gt;
        <section name=&quot;unity&quot;
                 type=&quot;Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration&quot; /&gt;
      </configSections&gt;
    
      <appSettings&gt;
        <add key=&quot;MyTestSetting&quot;
             value=&quot;234234&quot; /&gt;
      </appSettings&gt;
    
      <unity&gt;
        
        <sectionExtension type=&quot;Neovolve.Toolkit.Unity.AppSettingParameterValueExtension, Neovolve.Toolkit.Unity&quot; /&gt;
    
        <containers&gt;
          <container&gt;
            <register type=&quot;Neovolve.Toolkit.Unity.IntegrationTests.IDoSomething, Neovolve.Toolkit.Unity.IntegrationTests&quot;
                      mapTo=&quot;Neovolve.Toolkit.Unity.IntegrationTests.CachedSomethingDone, Neovolve.Toolkit.Unity.IntegrationTests&quot;&gt;
              <constructor&gt;
                <param name=&quot;dependency&quot;&gt;
                  <dependency name=&quot;CacheSomething&quot; /&gt;
                </param&gt;
                <param name=&quot;maxAgeInMilliseconds&quot;&gt;
                  <appSetting appSettingKey=&quot;MyTestSetting&quot; /&gt;
                </param&gt;
              </constructor&gt;
    
            </register&gt;
    
            <register type=&quot;Neovolve.Toolkit.Unity.IntegrationTests.IDoSomething, Neovolve.Toolkit.Unity.IntegrationTests&quot;
                      mapTo=&quot;Neovolve.Toolkit.Unity.IntegrationTests.SomethingDone, Neovolve.Toolkit.Unity.IntegrationTests&quot;
                      name=&quot;CacheSomething&quot;/&gt;
    
          </container&gt;
        </containers&gt;
      </unity&gt;
    </configuration&gt;{% endhighlight %}

The big change for supporting custom parameter injection is that the InjectionParameterValueElement is now called ParameterValueElement and no longer supports the elementType attribute. While this cleans up the configuration of the injection itself, it means that more work needs to be done to tell Unity how to inject the parameter using a custom implementation. 

A Unity SectionExtension is used to achieve this. The configuration for the extension is shown as the first line in the unity configuration above.

    using Microsoft.Practices.Unity.Configuration;
    
    namespace Neovolve.Toolkit.Unity
    {
        public class AppSettingParameterValueExtension : SectionExtension
        {
            public override void AddExtensions(SectionExtensionContext context)
            {
                context.AddElement<AppSettingsParameterValueElement&gt;(AppSettingsParameterValueElement.ElementName);
            }
        }
    }{% endhighlight %}

This extension simply tells the Unity configuration system how to understand a custom parameter injection handler. The context.AddElement method tells Unity which type to use when processing a particular configuration element name. The appSetting configuration element is now hooked up to use the AppSettingParameterValueElement which looks like the following:

    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Globalization;
    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.Configuration;
    using Neovolve.Toolkit.Storage;
    
    namespace Neovolve.Toolkit.Unity
    {
        public class AppSettingsParameterValueElement : ParameterValueElement
        {
            public const String ElementName = &quot;appSetting&quot;;
    
            public AppSettingsParameterValueElement()
            {
                Config = ConfigurationStoreFactory.Create();
            }
    
            public Object CreateValue(Type parameterType)
            {
                String configurationValue = Config.GetApplicationSetting<String&gt;(AppSettingKey);
                Object injectionValue;
    
                if (parameterType == typeof(String))
                {
                    injectionValue = configurationValue;
                }
                else
                {
                    TypeConverter converter = GetTypeConverter(parameterType);
    
                    try
                    {
                        injectionValue = converter.ConvertFromInvariantString(configurationValue);
                    }
                    catch (NotSupportedException ex)
                    {
                        const String MessageFormat = &quot;The AppSetting with key '{0}' and value '{1}' cannot be converted to type '{2}'&quot;;
                        String settingValue = configurationValue ?? &quot;(null)&quot;;
    
                        String failureMessage = String.Format(
                            CultureInfo.InvariantCulture, MessageFormat, AppSettingKey, settingValue, parameterType.FullName);
    
                        throw new ConfigurationErrorsException(failureMessage, ex);
                    }
                }
    
                return injectionValue;
            }
    
            public override InjectionParameterValue GetInjectionParameterValue(IUnityContainer container, Type parameterType)
            {
                Object injectionValue = CreateValue(parameterType);
    
                return new InjectionParameter(parameterType, injectionValue);
            }
    
            private TypeConverter GetTypeConverter(Type parameterType)
            {
                if (String.IsNullOrEmpty(TypeConverterTypeName) == false)
                {
                    Type converterType = Type.GetType(TypeConverterTypeName);
    
                    if (converterType == null)
                    {
                        const String MessageFormat = &quot;The type '{0}' could not be loaded.&quot;;
                        String message = String.Format(CultureInfo.InvariantCulture, MessageFormat, TypeConverterTypeName);
    
                        throw new ConfigurationErrorsException(message);
                    }
    
                    if (typeof(TypeConverter).IsAssignableFrom(converterType) == false)
                    {
                        const String MessageFormat = &quot;The type '{0}' does not inherit from '{1}'.&quot;;
                        String message = String.Format(CultureInfo.InvariantCulture, MessageFormat, TypeConverterTypeName, typeof(TypeConverter).FullName);
    
                        throw new ConfigurationErrorsException(message);
                    }
    
                    return (TypeConverter)Activator.CreateInstance(converterType);
                }
    
                return TypeDescriptor.GetConverter(parameterType);
            }
    
            [ConfigurationProperty(&quot;appSettingKey&quot;, IsRequired = true)]
            public String AppSettingKey
            {
                get
                {
                    return (String)base[&quot;appSettingKey&quot;];
                }
    
                set
                {
                    base[&quot;appSettingKey&quot;] = value;
                }
            }
    
            [ConfigurationProperty(&quot;typeConverter&quot;, IsRequired = false, DefaultValue = null)]
            public String TypeConverterTypeName
            {
                get
                {
                    return (String)base[&quot;typeConverter&quot;];
                }
    
                set
                {
                    base[&quot;typeConverter&quot;] = value;
                }
            }
    
            private IConfigurationStore Config
            {
                get;
                set;
            }
        }
    }{% endhighlight %}

The class is similar to the original version with most of the changes being a refactored Unity API. My implementation uses a interface based configuration store, but this can be easily substituted for ConfigurationManager.AppSettings.

This code can be found out on CodePlex in this [changeset][5].

[0]: /post/2010/01/28/Injecting-AppSetting-values-via-Unity.aspx
[1]: http://www.alvinashcraft.com/2010/04/21/dew-drop-april-21-2010/?utm_source=feedburner&amp;utm_medium=feed&amp;utm_campaign=Feed%3A+alvinashcraft+%28Alvin+Ashcraft%27s+Morning+Dew%29
[2]: http://blog.cwa.me.uk/2010/04/21/the-morning-brew-584/?utm_source=feedburner&amp;utm_medium=feed&amp;utm_campaign=Feed%3A+ReflectivePerspective+%28Reflective+Perspective+-+Chris+Alcock%29
[3]: http://blogs.msdn.com/agile/archive/2010/04/20/microsoft-enterprise-library-5-0-released.aspx
[4]: http://blogs.msdn.com/agile/default.aspx
[5]: http://neovolve.codeplex.com/SourceControl/changeset/changes/58040
