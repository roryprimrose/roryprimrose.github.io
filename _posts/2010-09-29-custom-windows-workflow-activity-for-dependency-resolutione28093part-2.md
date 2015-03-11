---
title: Custom Windows Workflow activity for dependency resolution–Part 2
categories : .Net
tags : Unity, WF
date: 2010-09-29 10:08:25 +10:00
---

My [previous post][0] described the design goals for creating a custom WF4 activity that provides dependency resolution functionality. This post will look at the underlying support for making this happen.

The main issue with dependency resolution/injection in WF is supporting persistence. An exception will be thrown when a workflow is persisted when it holds onto a dependency that is not serializable. The previous post indicated that the solution to this issue is to have the workflow persist the resolution description and explicitly prevent serialization of the resolved instance itself.

The way this is done is via an InstanceHandler&lt;T&gt; class.

<!--more-->

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow
{
    using System;
    using Neovolve.Toolkit.Workflow.Activities;
    using Neovolve.Toolkit.Workflow.Extensions;
    
    [Serializable]
    public class InstanceHandler<T>
    {
        [NonSerialized]
        private Boolean _resolveAttemptMade;
    
        [NonSerialized]
        private T _resolvedInstance;
    
        internal InstanceHandler(String resolutionName)
        {
            InstanceHandlerId = Guid.NewGuid();
            ResolutionName = resolutionName;
        }
    
        public T Instance
        {
            get
            {
                if (_resolveAttemptMade)
                {
                    return _resolvedInstance;
                }
    
                _resolveAttemptMade = true;
    
                _resolvedInstance = InstanceManagerExtension.Resolve(this);
    
                return _resolvedInstance;
            }
        }
    
        public Guid InstanceHandlerId
        {
            get;
            private set;
        }
    
        public String ResolutionName
        {
            get;
            private set;
        }
    }
}
{% endhighlight %}

The InstanceHandler&lt;T&gt; class contains the description of the resolution. This identifies the type of resolution (being &lt;T&gt;) and the name for the resolution. This may by null for default resolutions in Unity. The class also creates a GUID value that identifies a particular instance of this class. This is needed so that instances can be resolved from a cache when the owning activity is either persisted or finalised (completed, aborted or cancelled).

The InstanceHandler also exposes a property for the instance being resolved. It is strongly typed to the generic type argument of T. The serialization of this instance is specifically denied via the NonSerialized attribute on the backing field. The other field used here is a flag that indicates whether the instance resolution has already occurred. This is used in order to support null resolution values.

This class can be serialized at any point and only the definition of how to resolve an instance will be stored. The instance is resolved when the Instance property is referenced rather than when the activity starts. This is done for two reasons.
    
1. Lazy loading the instance – resolutions only occur when they are referenced
1. Supporting resolutions after workflow has persisted and been resumed.
    
The first reason is merely a beneficial side effect. The second reason listed is a technical restriction that comes into play when a persisted workflow resumes and the Instance property of the handler is invoked. There is no reference to an activity context at this point and no prior code can execute in which the dependency can be resolved.

This class makes a static call out to an InstanceManagerExtension to resolve the instance. The InstanceManagerExtension class is used to abstract the resolution, management and clean up logic for instances requested by the handler class.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Extensions
{
    using System;
    using System.Activities.Persistence;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Xml.Linq;
    using Microsoft.Practices.Unity;
    using Neovolve.Toolkit.Threading;
    using Neovolve.Toolkit.Unity;
    
    public class InstanceManagerExtension : PersistenceParticipant, IDisposable
    {
        private static readonly Dictionary<Guid, Object> _instanceCache = new Dictionary<Guid, Object>();
    
        private static readonly ReaderWriterLockSlim _instanceSyncLock = new ReaderWriterLockSlim();
    
        private readonly List<Guid> _handlerIdCache = new List<Guid>();
    
        private readonly ReaderWriterLockSlim _handlerSyncLock = new ReaderWriterLockSlim();
    
        private static IUnityContainer _container;
    
        public static T Resolve<T>(InstanceHandler<T> handler)
        {
            Contract.Requires<ArgumentNullException>(handler != null);
    
            T resolvedInstance = Container.Resolve<T>(handler.ResolutionName);
    
            using (new LockWriter(_instanceSyncLock))
            {
                if (_instanceCache.ContainsKey(handler.InstanceHandlerId))
                {
                    throw new InvalidOperationException("InstanceHandler cache is corrupted with a stale instance");
                }
    
                _instanceCache.Add(handler.InstanceHandlerId, resolvedInstance);
    
                return resolvedInstance;
            }
        }
    
        public void DestroyHandler(Guid instanceHandlerId)
        {
            Contract.Requires<ArgumentException>(instanceHandlerId.Equals(Guid.Empty) == false);
    
            DestroyHandlerByHandlerInstanceId(instanceHandlerId);
        }
    
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
        internal InstanceHandler<T> CreateInstanceHandler<T>(String resolutionName)
        {
            Contract.Requires<ObjectDisposedException>(Disposed == false);
    
            InstanceHandler<T> handler = new InstanceHandler<T>(resolutionName);
    
            // Store this created handler so that it can be destroyed when the workflow is persisted
            using (new LockWriter(_handlerSyncLock))
            {
                _handlerIdCache.Add(handler.InstanceHandlerId);
            }
    
            return handler;
        }
    
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // Free managed resources
                if (Disposed == false)
                {
                    Disposed = true;
    
                    DestroyLocalHandles();
    
                    _handlerSyncLock.Dispose();
                }
            }
    
            // Free native resources if there are any.
        }
    
        protected override IDictionary<XName, Object> MapValues(
            IDictionary<XName, Object> readWriteValues, IDictionary<XName, Object> writeOnlyValues)
        {
            // This method is used to detect when the activity is being persisted
            DestroyLocalHandles();
    
            return base.MapValues(readWriteValues, writeOnlyValues);
        }
    
        private void DestroyHandlerByHandlerInstanceId(Guid instanceHandlerId)
        {
            using (new LockWriter(_handlerSyncLock))
            {
                _handlerIdCache.Remove(instanceHandlerId);
            }
    
            // Get this handler
            using (new LockReader(_instanceSyncLock))
            {
                if (_instanceCache.ContainsKey(instanceHandlerId) == false)
                {
                    return;
                }
            }
    
            Object instance = null;
    
            using (new LockWriter(_instanceSyncLock))
            {
                if (_instanceCache.ContainsKey(instanceHandlerId))
                {
                    instance = _instanceCache[instanceHandlerId];
    
                    _instanceCache.Remove(instanceHandlerId);
                }
            }
    
            if (instance != null)
            {
                Container.Teardown(instance);
            }
        }
    
        private void DestroyLocalHandles()
        {
            List<Guid> handleIdList;
    
            using (new LockReader(_handlerSyncLock))
            {
                handleIdList = new List<Guid>(_handlerIdCache);
            }
    
            handleIdList.ForEach(DestroyHandlerByHandlerInstanceId);
        }
    
        public static IUnityContainer Container
        {
            get
            {
                if (_container == null)
                {
                    _container = UnityContainerResolver.Resolve();
                }
    
                return _container;
            }
    
            set
            {
                _container = value;
            }
        }
    
        protected Boolean Disposed
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The InstanceManagerExtension creates InstanceHandler instances, resolve dependencies and tear down dependencies. The extension will resolve a Unity container from configuration if a container has not already been assigned prior to the first dependency being resolved. This allows for a custom container to be provided if that is required but also falls back on a default behaviour.

The extension will first be used by an activity to create an InstanceHandler&lt;T&gt;. This is done so that the handler GUID can be tracked by the extension. Each InstanceHandlerId created is cached in a list stored against the extension instance.

When an activity execution makes a call to the Instance property of the handler, the handler then comes back to the extension to resolve the dependency using the static method. The reason for the static method is that the handler does not have a reference to the activity context from which the extension instance is obtained. The resolved instance is then cached in a dictionary object. Because the resolution process is static, the cache of instances is also static.

The last function of the extension is the ability to tear down the instances it has resolved. 

The extension has exposes a public DestroyHandler method that tears down the instance for a specified InstanceHandlerId. The tear down logic will remove the InstanceHandlerId from the instance handler Id cache and then obtain a reference to the resolved instance from the static dictionary cache. This instance is then removed from the cache and torn down using the container.

Tearing down a resolved instance occurs in any of the following circumstances:

* The workflow completes
* The workflow is cancelled
* The workflow is aborted
* The workflow is persisted
* The extension is disposed
    
The custom activity will notify the extension when any of the first three events occur. The custom activity will request the extension to tear down all the InstanceHandlerId values for which it has a reference.

The extension supports persistence by inheriting from PersistenceParticipant and overriding the MapValues method. This method gets invoked as part of a persistence operation. This extension does not provide any custom persistence support, but uses this method as a notification that persistence is executing. At this point the extension instance looks at the instance cache of InstanceHandlerId values that it has created and tears down all the resolved instances.

Finally, the extension implements IDisposable. This will ensure that any resolved instances that are not torn down in any of the above events are correctly disposed when the extension is disposed by the workflow engine.

The InstanceHandler&lt;T&gt; and InstanceManagerExtension classes provide the base framework for resolving and tearing down instances in a workflow execution. The next post will look at a simple custom activity that will leverage these to provide a resolved instance to a workflow execution.

[0]: /2010/09/16/custom-windows-workflow-activity-for-dependency-resolutione28093part-1/