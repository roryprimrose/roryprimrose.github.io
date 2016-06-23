---
title: Insight.Database with Unity integration
categories: .Net
tags: Azure, Dependency Injection, Unity
date: 2013-06-18 14:01:42 +10:00
---

One of my favourite development packages to use over the last year has been a micro-ORM called [Insight.Database][0] written by [Jon Wagner][1]. Version 2.1 of this excellent package comes with automatic interface implementations so you don’t even have to write DAL classes anymore. 

I haven’t been able to play with this feature until the last week. One of the roadblocks was that I use Unity as my IoC for dependency injection which does not natively support injecting these types of instances. What I need is to be able to configure a Unity container to return an auto-implemented interface in a constructor. I have created Unity extensions before for injecting [ConnectionStringSettings][2], [AppSettings][3] and [RealProxy implementations][4] so creating one for Insight.Database should be simple. The RealProxy Unity extension was very similar to what I need as the logic is just about the same.

<!--more-->

I want to support Unity configuration so we need to start with a ParameterValueElement class.

{% highlight csharp %}
namespace MyApplication.Server.Unity
{
    using System;
    using System.Configuration;
    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.Configuration;
    using Seterlund.CodeGuard;
    
    public class InsightOrmParameterValueElement : ParameterValueElement
    {
        public const string AppSettingKeyAttributeName = "appSettingKey";
    
        public const string ElementName = "insightOrm";
    
        public const string IsReliableAttributeName = "isReliable";
    
        public override InjectionParameterValue GetInjectionParameterValue(
            IUnityContainer container, Type parameterType)
        {
            Guard.That(container, "container").IsNotNull();
            Guard.That(parameterType, "parameterType").IsNotNull();
    
            var connectionSettings = GetConnectionSettings();
    
            return new InsightOrmParameterValue(parameterType, connectionSettings, IsReliable);
        }
    
        protected virtual ConnectionStringSettings GetConnectionSettings()
        {
            // Use the existing azure settings parameter value element to obtain the configuration value
            var configElement = new AzureSettingsParameterValueElement
            {
                AppSettingKey = AppSettingKey
            };
    
            var connectionSettings =
                configElement.CreateValue(typeof(ConnectionStringSettings)) as ConnectionStringSettings;
    
            return connectionSettings;
        }
    
        [ConfigurationProperty(AppSettingKeyAttributeName, IsRequired = true)]
        public string AppSettingKey
        {
            get
            {
                return (string)base[AppSettingKeyAttributeName];
            }
    
            set
            {
                base[AppSettingKeyAttributeName] = value;
            }
        }
    
        [ConfigurationProperty(IsReliableAttributeName, IsRequired = false, DefaultValue = false)]
        public bool IsReliable
        {
            get
            {
                return (bool)base[IsReliableAttributeName];
            }
    
            set
            {
                base[IsReliableAttributeName] = value;
            }
        }
    }
}
{% endhighlight %}

This class leverages a variant of the aforementioned ConnectionStringSettings Unity extension which reads database connection information from Azure configuration. You can easily change this part of the class to read the connection string from wherever is appropriate in your system.

Next we need an InjectionParameterValue class that will actual create the instance when the type is resolved in/by Unity.

{% highlight csharp %}
namespace MyApplication.Server.Unity
{
    using System;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using System.Reflection;
    using Insight.Database;
    using Insight.Database.Reliable;
    using Microsoft.Practices.ObjectBuilder2;
    using Microsoft.Practices.Unity;
    using Microsoft.Practices.Unity.ObjectBuilder;
    using Seterlund.CodeGuard;
    
    public class InsightOrmParameterValue : TypedInjectionValue
    {
        private readonly ConnectionStringSettings _connectionSettings;
    
        private readonly bool _isReliable;
    
        public InsightOrmParameterValue(
            Type parameterType, ConnectionStringSettings connectionSettings, bool isReliable) : base(parameterType)
        {
            Guard.That(parameterType, "parameterType").IsNotNull();
            Guard.That(connectionSettings, "connectionSettings").IsNotNull();
    
            _connectionSettings = connectionSettings;
            _isReliable = isReliable;
        }
    
        public override IDependencyResolverPolicy GetResolverPolicy(Type typeToBuild)
        {
            var instance = ResolveInstance(ParameterType);
    
            return new LiteralValueDependencyResolverPolicy(instance);
        }
    
        private object ResolveInstance(Type typeToBuild)
        {
            // Return the connection.As<ParameterType> value
            var parameterTypes = new[]
            {
                typeof(IDbConnection)
            };
            var genericMethod = typeof(DBConnectionExtensions).GetMethod(
                "As", BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null);
            var method = genericMethod.MakeGenericMethod(typeToBuild);
    
            DbConnection connection;
    
            if (_isReliable)
            {
                connection = _connectionSettings.ReliableConnection();
            }
            else
            {
                connection = _connectionSettings.Connection();
            }
    
            var parameters = new object[]
            {
                connection
            };
    
            var dalInstance = method.Invoke(null, parameters);
    
            return dalInstance;
        }
    }
}
{% endhighlight %}

This class will either create a connection or a reliable connection using the extension methods available in Insight.Database. It then gets a reflected reference to the As&lt;T&gt; extension method that converts that connection into an auto-implemented interface. This is the instance that gets returned.

Unity needs to know about the custom extension so that it can support it in configuration. The element class needs to be registered with Unity via a SectionExtension.

{% highlight csharp %}
namespace MyApplication.Server.Unity
{
    using Microsoft.Practices.Unity.Configuration;
    
    public class SectionExtensionInitiator : SectionExtension
    {
        public override void AddExtensions(SectionExtensionContext context)
        {
            if (context == null)
            {
                return;
            }
    
            context.AddElement<AzureSettingsParameterValueElement>(AzureSettingsParameterValueElement.ElementName);
            context.AddElement<InsightOrmParameterValueElement>(InsightOrmParameterValueElement.ElementName);
        }
    }
}
{% endhighlight %}

Lastly the configuration in Unity needs a pointer to the SectionExtension.

{% highlight xml %}
<?xml version="1.0"?>
<unity>
    
    <sectionExtension type="MyApplication.Server.Unity.SectionExtensionInitiator, MyApplication.Server.Unity" />
    
    <!-- Unity configuration here -->
    
</unity>
{% endhighlight %}

The only thing left to do is use the Unity configuration to inject an auto-implemented interface instance.

{% highlight xml %}
<register type="MyApplication.Server.BusinessContracts.IAccountManager, MyApplication.Server.BusinessContracts"
            mapTo="MyApplication.Server.Business.AccountManager, MyApplication.Server.Business">
    <constructor>
    <param name="store">
        <dependency />
    </param>
    <param name="verificationStore">
        <insightOrm appSettingKey="MyDatabaseConnectionConfigurationKey" isReliable="true" />
    </param>
    </constructor>
</register>
{% endhighlight %}

And we are done.

[0]: https://github.com/jonwagner/Insight.Database
[1]: http://code.jonwagner.com/
[2]: /2010/07/05/connectionstringsettings-parameter-injection-in-unity/
[3]: /2010/04/23/appsetting-parameter-injection-in-unity-2/
[4]: http://neovolve.codeplex.com/SourceControl/latest#1420795
