---
title: Working with custom proxies
categories : .Net
tags : WCF
date: 2010-07-22 12:56:00 +10:00
---

My recent post about [creating proxies with RealProxy][0] provided an example for creating a custom proxy implementation. Using proxies can provide a lot of power and flexibility to an application. Most of this code is common plumbing code that can be refactored out into some reusable classes.

The ProxyHandler class below is the first of these reusable classes. It helps with creating RealProxy types by providing the common logic of method identification, exception processing and method response management. It also provides the support for a derived proxy implementation to leverage some initialization logic provided by the calling application. It uses a MethodResolver class from my [Toolkit project][1] to identify the method to invoke on the proxy.

{% highlight csharp linenos %}
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security.Permissions;
using Neovolve.Toolkit.Reflection;
     
namespace Neovolve.Toolkit.Communication
{
    public abstract class ProxyHandler<T> : RealProxy where T : class
    {
        protected ProxyHandler()
            : base(typeof(T))
        {
        }
    
        public virtual void Initialize<TInitialize>(Action<TInitialize> action)
        {
            InitializeAction = action;
        }
    
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        [DebuggerStepThrough]
        public override IMessage Invoke(IMessage msg)
        {
            Debug.Assert(msg != null, "No message has been provided");
    
            ReturnMessage responseMessage;
            Object response = null;
            Exception caughtException = null;
    
            try
            {
                String methodName = (String)msg.Properties["__MethodName"];
                Type[] parameterTypes = (Type[])msg.Properties["__MethodSignature"];
                MethodBase method = MethodResolver.Resolve(typeof(T), methodName, parameterTypes);
    
                Debug.Assert(method != null, "Method was not found on the proxy");
    
                Object[] parameters = (Object[])msg.Properties["__Args"];
    
                // Invoke the action
                response = ExecuteMethod(method, parameters);
            }
            catch (Exception ex)
            {
                // Store the caught exception
                caughtException = ex;
            }
    
            IMethodCallMessage message = msg as IMethodCallMessage;
    
            // Check if there is an exception
            if (caughtException == null)
            {
                // Return the response from the service
                responseMessage = new ReturnMessage(response, null, 0, null, message);
            }
            else
            {
                // Return the exception thrown by the service
                responseMessage = new ReturnMessage(caughtException, message);
            }
    
            // Return the response message
            return responseMessage;
        }
    
        protected abstract Object ExecuteMethod(MethodBase method, Object[] parameters);
    
        protected MulticastDelegate InitializeAction
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The ProxyManager class below encapsulates the creation and lifetime management of a ProxyHandler instance. It creates a proxy instance from the ProxyHandler when the Proxy property is referenced and disposes the proxy when the ProxyManager is disposed. It has some logic for attempted to resolve a ProxyHandler&lt;T&gt; or then falling back on a default proxy or a proxy to a WCF channel if no ProxyHandler is provided in its constructor.

{% highlight csharp linenos %}
using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using Neovolve.Toolkit.Reflection;
    
namespace Neovolve.Toolkit.Communication
{
    public class ProxyManager<T> : IDisposable where T : class
    {
        private T _proxy;
    
        public ProxyManager()
        {
            if (TypeResolver.CanResolveType(typeof(ProxyHandler<T>)))
            {
                ProxyHandler = TypeResolver.Create<ProxyHandler<T>>();
            }
            else
            {
                // Check if T is a service contract
                Object serviceContract = typeof(T).GetCustomAttributes(true).Where(x => x is ServiceContractAttribute).FirstOrDefault();
    
                if (serviceContract != null)
                {
                    // This is a service contract, default to using the WCF proxy handler
                    ProxyHandler = new ChannelProxyHandler<T>();
                }
                else
                {
                    // No proxy handler is known for this type
                    ProxyHandler = new DefaultProxyHandler<T>();
                }
            }
        }
    
        public ProxyManager(ProxyHandler<T> proxyHandler)
        {
            ProxyHandler = proxyHandler;
        }
    
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // Free managed resources
                if (Disposed == false)
                {
                    Disposed = true;
    
                    IDisposable disposableHandler = ProxyHandler as IDisposable;
    
                    if (disposableHandler != null)
                    {
                        disposableHandler.Dispose();
                    }
    
                    ProxyHandler = null;
                    Proxy = null;
                }
            }
    
            // Free native resources if there are any.
        }
    
        public T Proxy
        {
            get
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
    
                if (_proxy == null)
                {
                    // Create a channel from the handler
                    _proxy = (T)ProxyHandler.GetTransparentProxy();
    
                    Debug.Assert(_proxy != null, "The proxy handler failed to create the proxy.");
                }
    
                return _proxy;
            }
    
            private set
            {
                _proxy = value;
            }
        }
    
        public ProxyHandler<T> ProxyHandler
        {
            get;
            private set;
        }
    
        protected Boolean Disposed
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

These classes work together to make it really easy to work with custom proxies. The most common usage I have for these classes is to proxy a call out to a WCF service. The following is an example of how this looks in a client console application. The benefit here for working with WCF is that all the management of WCF channels is abstracted away from the application code.

{% highlight csharp linenos %}
using System;
using System.ServiceModel;
using Neovolve.Toolkit.Communication;
    
namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ProxyManager<ITestService> manager = new ProxyManager<ITestService>())
            {
                String result = manager.Proxy.DoSomething("Service input data");
    
                Console.WriteLine("Result from service is: " + result);
            }
    
            Console.ReadKey();
        }
    }
    
    [ServiceContract]
    public interface ITestService
    {
        [OperationContract]
        String DoSomething(String data);
    }
}
{% endhighlight %}

The code for these classes can be found in my [Neovolve.Toolkit][2] project out on Codeplex.

[0]: /2010/07/17/creating-proxies-with-realproxy/
[1]: http://neovolve.codeplex.com/SourceControl/changeset/view/62725#443620
[2]: http://neovolve.codeplex.com/SourceControl/changeset/view/62725#1151241
