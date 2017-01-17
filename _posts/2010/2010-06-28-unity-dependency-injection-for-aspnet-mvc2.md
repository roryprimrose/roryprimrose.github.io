---
title: Unity dependency injection for ASP.Net MVC2
categories: .Net
tags: ASP.Net, Dependency Injection, Unity, WCF
date: 2010-06-28 13:34:56 +10:00
---

I knew that I should address the popular MVC model for ASP.Net as soon as I had finished my post on [Unity dependency injection for ASP.Net][0]. A different implementation for Unity injection is required here as the MCV model has a different method of processing ASP.Net pages. 

Like the posts for Unity injection with [WCF][1] and [ASP.Net][0], there are similar implementations already published on the internet to support MVC. The main issues I have with the examples posted elsewhere are that:

* global.asax application events are used which are not easily portable between projects
* there is no support for teardown operations of the controllers created via Unity (especially important when Unity creates [disposable build tree][2] hierarchies)

<!--more-->

ASP.Net MVC uses a controller factory to create controller instances. Only the controllers need to be created via Unity as the view is a UI control and the model should not have any logic. In a similar manner to the ASP.Net implementation, supporting MVC Unity injection leverages a HttpModule to configure the application for injection.

The UnityControllerFactoryHttpModule creates a new UnityControllerFactory if the current factory has not already been configured for Unity injection. It resets the controller factory back to a default factory when the module is disposed.

```csharp
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity
{
    public class UnityControllerFactoryHttpModule : UnityHttpModuleBase
    {
        public override void Init(HttpApplication context)
        {
            base.Init(context);
    
            UnityControllerFactory controllerFactory = ControllerBuilder.Current.GetControllerFactory() as UnityControllerFactory;
    
            if (controllerFactory != null)
            {
                return;
            }
    
            UnityControllerFactory factory = new UnityControllerFactory(Container);
    
            ControllerBuilder.Current.SetControllerFactory(factory);
        }
    
        public override void Dispose()
        {
            IControllerFactory defaultFactory = new DefaultControllerFactory();
    
            ControllerBuilder.Current.SetControllerFactory(defaultFactory);
    
            base.Dispose();
        }
    }
}
```

The UnityControllerFactoryHttpModule uses a base class refactored from the original [ASP.Net][0] implementation. This base class is responsible for resolving and disposing the Unity container for the module. The class calls into a UnityContainerResolver helper class to resolve the unity container. The code for UnityContainerResolver can be found [here][3].

```csharp
using System;
using System.Diagnostics.Contracts;
using System.Web;
using System.Web.UI;
using Microsoft.Practices.Unity;
    
namespace Neovolve.Toolkit.Unity
{
    public abstract class UnityHttpModuleBase : IHttpModule
    {
        private static readonly Object SyncLock = new Object();
    
        private static IUnityContainer _container;
    
        public virtual void Dispose()
        {
            DestroyContainer();
        }
    
        public virtual void Init(HttpApplication context)
        {
            // The container should only be assigned once by the Init method of the module
            // There are a pool of modules against the app pool and we only want the container created once
            AssignContainer(UnityContainerResolver.Resolve, false);
        }
    
        protected static void AssignContainer(Func<IUnityContainer> getContainer, Boolean allowContainerReassignment)
        {
            Contract.Requires<ArgumentNullException>(getContainer != null, "No getContainer function was provided");
            Contract.Ensures(_container != null, "Container was not created");
    
            if (allowContainerReassignment == false && _container != null)
            {
                return;
            }
    
            lock (SyncLock)
            {
                // Protect the container against multiple threads and instances that get past the initial check
                if (allowContainerReassignment == false && _container != null)
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
    
            lock (SyncLock)
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
    
        public static IUnityContainer Container
        {
            get
            {
                return _container;
            }
    
            set
            {
                // This is a manual container assignment so it can overwrite the existing container
                AssignContainer(() => value, true);
            }
        }
    }
}
```

The UnityControllerFactory class is used to create and teardown controller instances. This is the class that is the bridge between ASP.Net MVC and a Unity container. The GetControllerInstance method is a copy of the method in the base class with the only change being that the instance is resolved from Unity rather than via Activator.CreateInstance(). 

The factory uses the container to tear down the controller instance when the application notifies the factory that it is finished with it. The base class method is also invoked to cover the case when the container isn’t configured for tear down operations. This ensures that at least the controller instance itself can be disposed if it implements IDisposable. I suggest you configure the container to teardown build trees with my [custom extension][2] as this will ensure that all disposable instances are correctly destroyed.

```csharp
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Practices.Unity;
using Neovolve.Toolkit.Unity.Properties;
    
namespace Neovolve.Toolkit.Unity
{
    internal class UnityControllerFactory : DefaultControllerFactory
    {
        public UnityControllerFactory(IUnityContainer container)
        {
            Contract.Requires<ArgumentNullException>(container != null, "No container has been provided.");
    
            Container = container;
        }
    
        public override void ReleaseController(IController controller)
        {
            Container.Teardown(controller);
    
            base.ReleaseController(controller);
        }
    
        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            Debug.Assert(requestContext != null, "No requestContext was provided");
    
            IController controller;
    
            if (controllerType == null)
            {
                String message = String.Format(
                    CultureInfo.CurrentUICulture, Resources.UnityControllerFactory_NoControllerFound, requestContext.HttpContext.Request.Path);
    
                throw new HttpException(404, message);
            }
    
            if (typeof(IController).IsAssignableFrom(controllerType) == false)
            {
                String message = String.Format(
                    CultureInfo.CurrentUICulture, Resources.UnityControllerFactory_TypeDoesNotSubclassControllerBase, controllerType);
    
                throw new ArgumentException(message, "controllerType");
            }
    
            try
            {
                controller = (IController)Container.Resolve(controllerType);
            }
            catch (Exception exception)
            {
                String message = String.Format(CultureInfo.CurrentUICulture, Resources.UnityControllerFactory_ErrorCreatingController, controllerType);
    
                throw new InvalidOperationException(message, exception);
            }
    
            return controller;
        }
    
        protected IUnityContainer Container
        {
            get;
            private set;
        }
    }
}
```

Configuring this module is similar to the ASP.Net implementation. The following example hooks up the UnityControllerFactoryHttpModule and the DisposableStrategyExtension and configures injection support for the HashAlgorithm type.

```xml
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
                            mapTo="System.Security.Cryptography.SHA256CryptoServiceProvider, System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"/>
                <extensions>
                    <add type="Neovolve.Toolkit.Unity.DisposableStrategyExtension, Neovolve.Toolkit.Unity"/>
                </extensions>
            </container>
        </containers>
    </unity>
    <system.web>
        <compilation debug="true"
                        targetFramework="4.0"/>
        <authentication mode="None"></authentication>
        <httpModules>
            <add type="Neovolve.Toolkit.Unity.UnityControllerFactoryHttpModule"
                    name="UnityControllerFactoryHttpModule"/>
        </httpModules>
    </system.web>
    <system.webServer>
        <validation validateIntegratedModeConfiguration="false"/>
        <modules runAllManagedModulesForAllRequests="true">
            <add type="Neovolve.Toolkit.Unity.UnityControllerFactoryHttpModule"
                    name="UnityControllerFactoryHttpModule"/>
        </modules>
    </system.webServer>
</configuration>
```

This configuration can now be used to create any controller that has a HashAlgorithm type in its constructor.

```csharp
using System;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
    
namespace Neovolve.Toolkit.Unity.MvcWebIntegrationTests.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        public HomeController(HashAlgorithm hashCalulator)
        {
            HashCalculator = hashCalulator;
        }
    
        public ActionResult About()
        {
            return View();
        }
    
        public ActionResult Index()
        {
            String valueToHash = Guid.NewGuid().ToString();
            Byte[] valueInBytes = Encoding.UTF8.GetBytes(valueToHash);
            Byte[] hashBytes = HashCalculator.ComputeHash(valueInBytes);
    
            String values = valueToHash + " - " + Convert.ToBase64String(hashBytes);
    
            ViewData["Message"] = "Welcome to ASP.NET MVC! " + values;
    
            return View();
        }
    
        protected HashAlgorithm HashCalculator
        {
            get;
            private set;
        }
    }
}
```

All the code for this solution (including full xml documentation) can be found in my [Toolkit][4] project on CodePlex.

[0]: /2010/05/19/unity-dependency-injection-for-aspnet/
[1]: /2010/05/17/unity-dependency-injections-for-wcf-services-e28093-part-2/
[2]: /2010/06/18/unity-extension-for-disposing-build-trees-on-teardown/
[3]: http://neovolve.codeplex.com/SourceControl/changeset/view/58851#1195163
[4]: http://neovolve.codeplex.com/SourceControl/changeset/view/60707#1233389
