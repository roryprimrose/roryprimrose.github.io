---
title: Caching workflow activities to increase performance
categories : .Net
tags : Performance, WF
date: 2010-07-23 12:14:06 +10:00
---

[David Paquette][0] posted last year about [the performance characteristics of WF4][1]. Runtime performance is a typical complaint with WF but it can be minimised as David has indicated. Adding some caching logic for workflow activity instances will avoid the expensive start-up process for invoking activities. Subsequent activity invocations will be much faster by getting the activity instance from the cache rather than creating a new one.

I’ve put together an ActivityStore class that handles this caching requirement.{% highlight csharp linenos %}
using System;
using System.Activities;
using System.Collections.Generic;
using System.Threading;
using Neovolve.Toolkit.Threading;
    
namespace Neovolve.ActivityTesting
{
    internal static class ActivityStore
    {
        private static readonly IDictionary<Type, Activity> _store = new Dictionary<Type, Activity>();
    
        private static readonly ReaderWriterLockSlim _syncLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    
        public static T Resolve<T>() where T : Activity, new()
        {
            Type activityType = typeof(T);
    
            using (new LockReader(_syncLock))
            {
                if (_store.ContainsKey(activityType))
                {
                    return _store[activityType] as T;
                }
            }
    
            using (new LockWriter(_syncLock))
            {
                // Protect the store against mutliple threads that get passed the reader lock
                if (_store.ContainsKey(activityType))
                {
                    return _store[activityType] as T;
                }
    
                T activity = new T();
    
                _store[activityType] = activity;
    
                return activity;
            }
        }
    }
}
{% endhighlight %}

This class will hold on to activity instances in a dictionary using the activity type as the key. To use this class you simply need to make a call out to ActivityStore.Resolve<T>() rather than new T() where T is your activity type.

[0]: http://geekswithblogs.net/DavidPaquette
[1]: http://geekswithblogs.net/DavidPaquette/archive/2009/10/26/wf4-performance.aspx
