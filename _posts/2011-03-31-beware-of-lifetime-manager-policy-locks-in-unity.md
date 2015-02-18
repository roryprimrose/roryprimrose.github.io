---
title: Beware of lifetime manager policy locks in Unity
categories : .Net
tags : Dependency Injection, Performance, Tracing, Unity, WCF, WF, WIF
date: 2011-03-31 15:32:15 +10:00
---

I have created a caching dependency injection slice in order to squeeze more performance from the DAL in a workflow service. What I found was that the service always hit a timeout when the caching slice was put into the Unity configuration. I spent half a day working with AppFabric monitoring, event logs and all the information I could get out of diagnostic tracing for WCF, WF, WIF and custom sources. After not being able to get any answers along with futile debugging efforts, I realised that I could profile the service to see where the hold up was.

The profiler results told me exactly where to look as soon as I hit the service at got a timeout exception.![image][0]

All the time is getting consumed in a single call to Microsoft.Practices.Unity.SynchronizedLifetimeManager.GetValue(). The first idea that comes to mind is that there is a lock on an object that is not being released. Reflector proves that this is exactly the case. ![image][1]

The GetValue method obtains a lock for the current thread and only releases it if a non-null value is held by the lifetime manager. This logic becomes a big issue if the lifetime manager holds a null value and two different threads call GetValue. I would like to know why this behaviour is there as it is intentional according to the documentation of the function.

This is what is happening in my service. In the profiling above you can see that the lifetime manager is getting called from my [Unity extension for disposing build trees][2]. While the extension is not doing anything wrong, it can handle this scenario by using a timeout on obtaining a value from the lifetime manager.

{% highlight csharp linenos %}
private static Object GetLifetimePolicyValue(ILifetimePolicy lifetimeManager)
{
    if (lifetimeManager is IRequiresRecovery)
    {
        // There may be a lock around this policy where a null value will result in an indefinite lock held by another thread
        // We need to use another thread to access this item so that we can get around the lock using a timeout
        Task<Object> readPolicyTask = new Task<Object>(lifetimeManager.GetValue);
    
        readPolicyTask.Start();
    
        Boolean taskCompleted = readPolicyTask.Wait(10);
    
        if (taskCompleted == false)
        {
            return null;
        }
    
        return readPolicyTask.Result;
    }
    
    return lifetimeManager.GetValue();
}
{% endhighlight %}

This implementation is not ideal, but it is unfortunately the only way to handle this case as there is no way to determine whether another thread has a lock on the lifetime manager.

Testing the service again with the profiler then identified a problem with this workaround.![image][3]

This workaround will consume threads that will be held on a lock and potentially never get released. This is going to be unacceptable as more and more threads attempt to look at values held in the lifetime manager policies, ultimately resulting in thread starvation.

The next solution is to use a lifetime manager that gets around this issue by never allowing the lifetime manager to be assigned a null value.

{% highlight csharp linenos %}
namespace Neovolve.Jabiru.Server.Services
{
    using System;
    using System.Diagnostics.Contracts;
    using Microsoft.Practices.Unity;
    
    public class SafeSingletonLifetimeManager : ContainerControlledLifetimeManager
    {
        public override void SetValue(Object newValue)
        {
            if (newValue == null)
            {
                throw new ArgumentNullException("newValue");
            }
    
            base.SetValue(newValue);
        }
    }
}
{% endhighlight %}

This idea fails to get around the locking issue when the lifetime manager is created but never has a value assigned. The next version of this SafeSingletonLifetimeManager solves this by managing its own locking logic around whether a non-null value has been assigned to the policy.

{% highlight csharp linenos %}
namespace Neovolve.Jabiru.Server.Services
{
    using System;
    using System.Threading;
    using Microsoft.Practices.Unity;
    using Neovolve.Toolkit.Threading;
    
    public class SafeSingletonLifetimeManager : ContainerControlledLifetimeManager
    {
        private readonly ReaderWriterLockSlim _syncLock = new ReaderWriterLockSlim();
    
        private Boolean _valueAssigned;
    
        public override Object GetValue()
        {
            using (new LockReader(_syncLock))
            {
                if (_valueAssigned == false)
                {
                    return null;
                }
    
                return base.GetValue();
            }
        }
    
        public override void SetValue(Object newValue)
        {
            using (new LockWriter(_syncLock))
            {
                if (newValue == null)
                {
                    _valueAssigned = false;
                }
                else
                {
                    _valueAssigned = true;
                }
    
                base.SetValue(newValue);
            }
        }
    }
}
{% endhighlight %}

Using this policy now avoids the locking problem described in this post. I would still like to know the reason for the locking logic as this SafeSingletonLifetimeManager is completely circumventing that logic.

[0]: //blogfiles/image_96.png
[1]: //blogfiles/image_97.png
[2]: /post/2010/06/18/Unity-Extension-For-Disposing-Build-Trees-On-TearDown.aspx
[3]: //blogfiles/image_98.png
