---
title: Injecting AppSetting values via Unity
categories: .Net
tags: Unity
date: 2010-01-28 13:14:50 +10:00
---

I've been working with Unity a bit in an enterprise level system and I have been trying to separate objects of different concerns as much as possible. One requirement hit me today where I have a dependency that is resolved from a Unity container. I created a cache dependency layer to go around it, but needed to provide a configuration value to the cache wrapper.

To demonstrate this scenario, consider the following example:

<!--more-->

```csharp
using System;
    
namespace Neovolve.UnityTesting
{
    public interface IDoSomething
    {
        String Execute();
    }
    
    public class SomethingDone : IDoSomething
    {
        public String Execute()
        {
            return"Some random value";
        }
    }
    
    public class CachedSomethingDone : IDoSomething
    {
        private readonly IDoSomething _dependency;
    
        public CachedSomethingDone(IDoSomething dependency, Int64 maxAgeInMilliseconds)
        {
            _dependency = dependency;
            MaxAgeInMilliseconds = maxAgeInMilliseconds;
        }
    
        public String Execute()
        {
            // Check cache for value
            // If cache has value and is not too old then return the value
            // If not, get value from dependency, cache it for the next call and return the value
    
            return _dependency.Execute();
        }
    
        public long MaxAgeInMilliseconds
        {
            get;
            private set;
        }
    }
}
```

Unfortunately Unity does not provide a way to resolve a configuration value and inject it into the instance being resolved. This means that the configuration value must be injected as a literal value defined in the unity configuration. While this works, most people think in terms of the appSettings element when configuring the options for their application. This would require them to also review the often complex unity configuration to adjust a required value. I wanted a way to essentially redirect the appSetting value into a unity injection value.

I spent a bit of time cruising around the EntLib source with Reflector to see what could be done about this. Unity natively understands injection configuration for dependencies, literal values and array types. Unity happens to make a call down to InjectionParameterValueHelper.DeserializeUnrecognizedElement() which has a switch over the element name (being value, dependency or array). If the type is not understood then it makes a call out to DeserializePolymorphicElement().

The DeserializePolymorphicElement method is the key. It uses an elementType attribute that gets resolved as InjectionParameterValueElement. This is the point at which Unity can be extended to provide a custom injection implementation. I created an AppSettingsParameterInjectionElement class that is mostly a copy of InstanceValueElement which is the unity definition for injection configuration of literal values. The class looks like the following:

```csharp
using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
    
namespace Neovolve.UnityTesting
{
    public class AppSettingsParameterInjectionElement : InjectionParameterValueElement
    {
        public Object CreateInstance()
        {
            String configurationValue = ConfigurationManager.AppSettings[AppSettingKey];
            Type typeToCreate = TypeToCreate;
    
            if (typeToCreate == typeof(String))
            {
                return configurationValue;
            }
    
            TypeConverter converter = GetTypeConverter(typeToCreate, TypeConverterName, TypeResolver);
    
            try
            {
                return converter.ConvertFromString(configurationValue);
            }
            catch (NotSupportedException ex)
            {
                const String MessageFormat =
                    "The AppSetting with key '{0}' and value '{1}' cannot be converted to type '{2}'";
                String settingValue = configurationValue ?? "(null)";
    
                String failureMessage = String.Format(
                    CultureInfo.InvariantCulture, MessageFormat, AppSettingKey, settingValue, typeToCreate.FullName);
                    
                throw new ConfigurationErrorsException(failureMessage, ex);
            }
        }
    
        public override InjectionParameterValue CreateParameterValue(Type targetType)
        {
            Type type;
    
            if (String.IsNullOrEmpty(TypeName))
            {
                type = targetType;
            }
            else
            {
                type = TypeResolver.ResolveType(TypeName);
            }
    
            return new InjectionParameter(type, CreateInstance());
        }
    
        private static TypeConverter GetTypeConverter(
            Type typeToCreate, String typeConverterName, UnityTypeResolver typeResolver)
        {
            if (!String.IsNullOrEmpty(typeConverterName))
            {
                return (TypeConverter)Activator.CreateInstance(typeResolver.ResolveType(typeConverterName));
            }
    
            return TypeDescriptor.GetConverter(typeToCreate);
        }
    
        [ConfigurationProperty("appSettingKey", IsRequired = true)]
        public String AppSettingKey
        {
            get
            {
                return (String)base["appSettingKey"];
            }
    
            set
            {
                base["appSettingKey"] = value;
            }
        }
    
        [ConfigurationProperty("typeConverter", IsRequired = false, DefaultValue = null)]
        public String TypeConverterName
        {
            get
            {
                return (String)base["typeConverter"];
            }
    
            set
            {
                base["typeConverter"] = value;
            }
        }
    
        [ConfigurationProperty("type", DefaultValue = null)]
        public String TypeName
        {
            get
            {
                return (String)base["type"];
            }
    
            set
            {
                base["type"] = value;
            }
        }
    
        public Type TypeToCreate
        {
            get
            {
                return TypeResolver.ResolveWithDefault(TypeName, typeof(String));
            }
        }
    }
}
```

The configuration for this looks like the following:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    
    <configSections>
    <section name="unity"
                type="Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration" />
    </configSections>
    
    <appSettings>
    <add key="MyTestSetting"
            value="234234" />
    </appSettings>
    
    <unity>
    <containers>
        <container>
        <types>
    
            <type type="Neovolve.UnityTesting.IDoSomething, Neovolve.UnityTesting"
                mapTo="Neovolve.UnityTesting.CachedSomethingDone, Neovolve.UnityTesting">
            <typeConfig extensionType="Microsoft.Practices.Unity.Configuration.TypeInjectionElement,
                                        Microsoft.Practices.Unity.Configuration">
                <constructor>
                <param name="dependency"
                        parameterType="Neovolve.UnityTesting.IDoSomething, Neovolve.UnityTesting">
                    <dependency name="CacheSomething" />
                </param>
                <param name="maxAgeInMilliseconds"
                        parameterType="System.Int64, mscorlib, Version=2.0.0.0">
                    <appSetting elementType="Neovolve.UnityTesting.AppSettingsParameterInjectionElement, Neovolve.UnityTesting"
                                appSettingKey="MyTestSetting"
                                type="System.Int64, mscorlib, Version=2.0.0.0"/>
                </param>
                </constructor>
            </typeConfig>
            </type>
    
            <type type="Neovolve.UnityTesting.IDoSomething, Neovolve.UnityTesting"
                mapTo="Neovolve.UnityTesting.SomethingDone, Neovolve.UnityTesting"
                name="CacheSomething">
            </type>
    
        </types>
        </container>
    </containers>
    </unity>
</configuration>
```

The test application to tie all this together is the following:

```csharp
using System;
using System.Configuration;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
    
namespace Neovolve.UnityTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            UnityConfigurationSection section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            UnityContainer container = new UnityContainer();
    
            section.Containers.Default.Configure(container);
    
            CachedSomethingDone something = (CachedSomethingDone)container.Resolve();
    
            Console.WriteLine("Dependency configured with max age: " + something.MaxAgeInMilliseconds);
            Console.ReadKey();
        }
    }
}
```

In this process I found that it would be nice if the InstanceValueElement class was designed to allow for easier extension. The two likely options here are to allow the CreateInstance method or the Value property to be overridden. This would mean that the above class would only have to resolve the application setting rather than essentially duplicate the logic in InstanceValueElement.

[Neovolve.UnityTesting.zip (8.63 kb)][0]

[0]: /files/2010/1/Neovolve.UnityTesting.zip
