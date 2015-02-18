---
title: Injecting AppSetting values via Unity
categories : .Net
tags : Unity
date: 2010-01-28 13:14:50 +10:00
---

I've been working with Unity a bit in an enterprise level system and I have been trying to separate objects of different concerns as much as possible. One requirement hit me today where I have a dependency that is resolved from a Unity container. I created a cache dependency layer to go around it, but needed to provide a configuration value to the cache wrapper.

To demonstrate this scenario, consider the following example:

 {% highlight csharp linenos %}using System; namespace Neovolve.UnityTesting { public interface IDoSomething { String Execute(); } public class SomethingDone : IDoSomething { public String Execute() { return&quot;Some random value&quot;; } } public class CachedSomethingDone : IDoSomething { private readonly IDoSomething _dependency; public CachedSomethingDone(IDoSomething dependency, Int64 maxAgeInMilliseconds) { _dependency = dependency; MaxAgeInMilliseconds = maxAgeInMilliseconds; } public String Execute() { // Check cache for value // If cache has value and is not too old then return the value // If not, get value from dependency, cache it for the next call and return the value return _dependency.Execute(); } public long MaxAgeInMilliseconds { get; private set; } } }{% endhighlight %} 

Unfortunately Unity does not provide a way to resolve a configuration value and inject it into the instance being resolved. This means that the configuration value must be injected as a literal value defined in the unity configuration. While this works, most people think in terms of the appSettings element when configuring the options for their application. This would require them to also review the often complex unity configuration to adjust a required value. I wanted a way to essentially redirect the appSetting value into a unity injection value.

I spent a bit of time cruising around the EntLib source with Reflector to see what could be done about this. Unity natively understands injection configuration for dependencies, literal values and array types. Unity happens to make a call down to InjectionParameterValueHelper.DeserializeUnrecognizedElement() which has a switch over the element name (being value, dependency or array). If the type is not understood then it makes a call out to DeserializePolymorphicElement().

The DeserializePolymorphicElement method is the key. It uses an elementType attribute that gets resolved as InjectionParameterValueElement. This is the point at which Unity can be extended to provide a custom injection implementation. I created an AppSettingsParameterInjectionElement class that is mostly a copy of InstanceValueElement which is the unity definition for injection configuration of literal values. The class looks like the following:

 {% highlight csharp linenos %}using System; using System.ComponentModel; using System.Configuration; using System.Globalization; using Microsoft.Practices.Unity; using Microsoft.Practices.Unity.Configuration; namespace Neovolve.UnityTesting { public class AppSettingsParameterInjectionElement : InjectionParameterValueElement { public Object CreateInstance() { String configurationValue = ConfigurationManager.AppSettings[AppSettingKey]; Type typeToCreate = TypeToCreate; if (typeToCreate == typeof(String)) { return configurationValue; } TypeConverter converter = GetTypeConverter(typeToCreate, TypeConverterName, TypeResolver); try { return converter.ConvertFromString(configurationValue); } catch (NotSupportedException ex) { const String MessageFormat = &quot;The AppSetting with key '{0}' and value '{1}' cannot be converted to type '{2}'&quot;; String settingValue = configurationValue ?? &quot;(null)&quot;; String failureMessage = String.Format( CultureInfo.InvariantCulture, MessageFormat, AppSettingKey, settingValue, typeToCreate.FullName); throw new ConfigurationErrorsException(failureMessage, ex); } } public override InjectionParameterValue CreateParameterValue(Type targetType) { Type type; if (String.IsNullOrEmpty(TypeName)) { type = targetType; } else { type = TypeResolver.ResolveType(TypeName); } return new InjectionParameter(type, CreateInstance()); } private static TypeConverter GetTypeConverter( Type typeToCreate, String typeConverterName, UnityTypeResolver typeResolver) { if (!String.IsNullOrEmpty(typeConverterName)) { return (TypeConverter)Activator.CreateInstance(typeResolver.ResolveType(typeConverterName)); } return TypeDescriptor.GetConverter(typeToCreate); } [ConfigurationProperty(&quot;appSettingKey&quot;, IsRequired = true)] public String AppSettingKey { get { return (String)base[&quot;appSettingKey&quot;]; } set { base[&quot;appSettingKey&quot;] = value; } } [ConfigurationProperty(&quot;typeConverter&quot;, IsRequired = false, DefaultValue = null)] public String TypeConverterName { get { return (String)base[&quot;typeConverter&quot;]; } set { base[&quot;typeConverter&quot;] = value; } } [ConfigurationProperty(&quot;type&quot;, DefaultValue = null)] public String TypeName { get { return (String)base[&quot;type&quot;]; } set { base[&quot;type&quot;] = value; } } public Type TypeToCreate { get { return TypeResolver.ResolveWithDefault(TypeName, typeof(String)); } } } }{% endhighlight %} 

The configuration for this looks like the following:

 {% highlight xml linenos %}<?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt; <configuration&gt; <configSections&gt; <section name=&quot;unity&quot; type=&quot;Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration&quot; /&gt; </configSections&gt; <appSettings&gt; <add key=&quot;MyTestSetting&quot; value=&quot;234234&quot; /&gt; </appSettings&gt; <unity&gt; <containers&gt; <container&gt; <types&gt; <type type=&quot;Neovolve.UnityTesting.IDoSomething, Neovolve.UnityTesting&quot; mapTo=&quot;Neovolve.UnityTesting.CachedSomethingDone, Neovolve.UnityTesting&quot;&gt; <typeConfig extensionType=&quot;Microsoft.Practices.Unity.Configuration.TypeInjectionElement, Microsoft.Practices.Unity.Configuration&quot;&gt; <constructor&gt; <param name=&quot;dependency&quot; parameterType=&quot;Neovolve.UnityTesting.IDoSomething, Neovolve.UnityTesting&quot;&gt; <dependency name=&quot;CacheSomething&quot; /&gt; </param&gt; <param name=&quot;maxAgeInMilliseconds&quot; parameterType=&quot;System.Int64, mscorlib, Version=2.0.0.0&quot;&gt; <appSetting elementType=&quot;Neovolve.UnityTesting.AppSettingsParameterInjectionElement, Neovolve.UnityTesting&quot; appSettingKey=&quot;MyTestSetting&quot; type=&quot;System.Int64, mscorlib, Version=2.0.0.0&quot;/&gt; </param&gt; </constructor&gt; </typeConfig&gt; </type&gt; <type type=&quot;Neovolve.UnityTesting.IDoSomething, Neovolve.UnityTesting&quot; mapTo=&quot;Neovolve.UnityTesting.SomethingDone, Neovolve.UnityTesting&quot; name=&quot;CacheSomething&quot;&gt; </type&gt; </types&gt; </container&gt; </containers&gt; </unity&gt; </configuration&gt;{% endhighlight %} 

The test application to tie all this together is the following:

 {% highlight csharp linenos %}using System; using System.Configuration; using Microsoft.Practices.Unity; using Microsoft.Practices.Unity.Configuration; namespace Neovolve.UnityTesting { class Program { static void Main(string[] args) { UnityConfigurationSection section = (UnityConfigurationSection)ConfigurationManager.GetSection(&quot;unity&quot;); UnityContainer container = new UnityContainer(); section.Containers.Default.Configure(container); CachedSomethingDone something = (CachedSomethingDone)container.Resolve(); Console.WriteLine(&quot;Dependency configured with max age: &quot; + something.MaxAgeInMilliseconds); Console.ReadKey(); } } }{% endhighlight %} 

In this process I found that it would be nice if the InstanceValueElement class was designed to allow for easier extension. The two likely options here are to allow the CreateInstance method or the Value property to be overridden. This would mean that the above class would only have to resolve the application setting rather than essentially duplicate the logic in InstanceValueElement.

[Neovolve.UnityTesting.zip (8.63 kb)][0]

[0]: /blogfiles/2010%2f1%2fNeovolve.UnityTesting.zip