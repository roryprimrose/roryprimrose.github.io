---
title: Unity dependency injection for ASP.Net
categories: .Net
tags: ASP.Net, Dependency Injection, Unity, WCF
date: 2010-05-19 15:39:13 +10:00
---

I’ve been wanting to get dependency injection happening for ASP.Net pages like I have for WCF service instances (see [here][0] and [here][1]). The solutions that I found on the net use combinations of abstract ASP.Net pages or Global.asax with an IHttpModule. I didn’t like these solutions because it is messy to make multiple changes to your application to introduce a single piece of functionality.

My version uses a single IHttpModule without any other dependencies. The only impact on the application is to configure the module in web.config. 

My UnityHttpModule looks like the following. 

<!--more-->

{% highlight csharp %}
using System;
using System.Diagnostics.Contracts;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity
{
    public class UnityHttpModule : IHttpModule
    {
        private static readonly Object _syncLock = new Object();
    
        private static IUnityContainer _container;
    
        public void Dispose()
        {
            DestroyContainer();
        }
    
        public void Init(HttpApplication context)
        {
            AssignContainer(UnityContainerResolver.Resolve);
    
            context.PreRequestHandlerExecute += OnPreRequest;
            context.PostRequestHandlerExecute += OnRequestCompleted;
        }
    
        private static void AssignContainer(Func<IUnityContainer> getContainer)
        {
            Contract.Ensures(_container != null, "Container was not created");
    
            if (_container != null)
            {
                return;
            }
    
            lock (_syncLock)
            {
                // Protect the container against multiple threads and instances that get past the initial check
                if (_container != null)
                {
                    return;
                }
    
                _container = getContainer();
            }
        }
    
        private static void DestroyContainer()
        {
            Contract.Ensures(_container == null, "Container was not destroyed");
    
            if (_container == null)
            {
                return;
            }
    
            lock (_syncLock)
            {
                // Protect the container against multiple threads and instances that get past the initial check
                if (_container == null)
                {
                    return;
                }
    
                _container.Dispose();
                _container = null;
            }
        }
    
        private static void OnPreRequest(Object sender, EventArgs e)
        {
            ProcessUnityAction(sender, page => _container.BuildUp(page.GetType(), page, null));
        }
    
        private static void OnRequestCompleted(Object sender, EventArgs e)
        {
            ProcessUnityAction(sender, page => _container.Teardown(page));
        }
    
        private static void ProcessUnityAction(Object sender, Action<Page> unityAction)
        {
            if (sender == null)
            {
                throw new ArgumentNullException("sender");
            }
    
            if (unityAction == null)
            {
                throw new ArgumentNullException("unityAction");
            }
    
            if (HttpContext.Current == null)
            {
                return;
            }
    
            Page page = HttpContext.Current.Handler as Page;
    
            if (page == null)
            {
                return;
            }
    
            unityAction(page);
        }
    
        public static IUnityContainer Container
        {
            get
            {
                return _container;
            }
    
            set
            {
                AssignContainer(() => value);
            }
        }
    }
}
{% endhighlight %}

The UnityHttpModule calls into a UnityContainerResolver helper class to resolve the unity container. The code for UnityContainerResolver can be found [here][2].

The instancing behaviour of IHttpModule must be considered for this implementation as we only want a single container to be used. Many instances of a IHttpModule type get created within an application pool. This is because there are many HttpApplication instances created to satisfy multiple requests for a site. Each HttpApplication instance has it own set of modules. A thread lock is used in this module to protect the static container while it is being assigned or destroyed. The creation of the container happens when the first IHttpModule across any of the the HttpApplications is initialized and the disposal happens when the first IHttpModule is disposed. All other creations and disposals are ignored.

Once created, this module builds up pages with injection values and then tears them down again when the pages are finished with their request processing.

The following is an example ASP.Net page that uses property injection.

{% highlight csharp %}
using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity.WebIntegrationTests
{
    public partial class _Default : Page
    {
        protected void Page_Load(Object sender, EventArgs e)
        {
            if (HashCalculator == null)
            {
                throw new ConfigurationErrorsException("Unity was not used to build up this page");
            }
    
            String valueToHash = Guid.NewGuid().ToString();
            Byte[] valueInBytes = Encoding.UTF8.GetBytes(valueToHash);
            Byte[] hashBytes = HashCalculator.ComputeHash(valueInBytes);
    
            Original.Text = valueToHash;
            HashValue.Text = Convert.ToBase64String(hashBytes);
        }
    
        [Dependency]
        public HashAlgorithm HashCalculator
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The property injection for this page is configured via the web.config. The following example also includes the configuration for the module for both classic and integrated IIS pipeline modes.

{% highlight xml %}
<?xml version="1.0" ?>
<configuration>
    <configSections>
        <section name="unity"
                    type="Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration"/>
    </configSections>
    <unity>
        <containers>
            <container>
                <register type="System.Security.Cryptography.HashAlgorithm, mscorlib"
                            mapTo="System.Security.Cryptography.SHA256CryptoServiceProvider, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"/>
            </container>
        </containers>
    </unity>
    <system.web>
        <compilation debug="true"
                        targetFramework="4.0"/>
        <authentication mode="None"></authentication>
        <httpModules>
            <add type="Neovolve.Toolkit.Unity.UnityHttpModule"
                    name="UnityHttpModule"/>
        </httpModules>
    </system.web>
    <system.webServer>
        <validation validateIntegratedModeConfiguration="false"/>
        <modules runAllManagedModulesForAllRequests="true">
            <add type="Neovolve.Toolkit.Unity.UnityHttpModule"
                    name="UnityHttpModule"/>
        </modules>
    </system.webServer>
</configuration>
{% endhighlight %}

The beauty of the way Unity is used here is that you do not need to configure each page that uses injection in the Unity configuration. Only the types being injected need to be defined. There are however a few things to note about this usage:

* Properties (and their types) that accept injected values must be public
* Properties that accept injected values must be decorated with the Dependency attribute.
* Named Unity containers are not supported.
* Non-standard Unity section configuration names are not supported (must use “unity”)
    
The only part I do not like about this implementation is that the Unity Dependency attribute must be assigned to the ASP.Net page properties that will use injection. This couples the ASP.Net application to Unity. I suppose a Unity extension could be written to bypass this behaviour, but the attribute is no doubt there for a good reason. My guess is that it caters for scenarios where properties have an associated type in the Unity config but are not intended to have values injected.

The full code for this module with xml comments can be found on my [Toolkit project][3] in Codeplex.

[0]: /2010/05/15/unity-dependency-injection-for-wcf-services-e28093-part-1/
[1]: /2010/05/17/unity-dependency-injections-for-wcf-services-e28093-part-2/
[2]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1195163
[3]: http://neovolve.codeplex.com/SourceControl/changeset/view/58941#1216713
