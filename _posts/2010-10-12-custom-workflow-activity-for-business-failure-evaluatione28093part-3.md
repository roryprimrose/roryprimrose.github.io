---
title: Custom Workflow activity for business failure evaluation–Part 3
categories : .Net
tags : WF
date: 2010-10-12 16:15:00 +10:00
---

The [previous post][0] looked at the support for creating business failures and throwing the failures with an exception. This post will look at a custom WF extension that will process business failures in WF.

Using a custom extension to manage business failures abstracts workflow activities from the logic of processing business failures. The extension is responsible for processing business failures as they are reported by activities. It will also need to track relationships between activities in order to support the design goal of grouping related business failures into a single exception. This is achieved by providing a way of linking activities.

The Activity class in WF 3.0 and 3.5 provided a public Activity Parent property that could be used to traverse up the activity tree hierarchy. Unfortunately this property has been marked as internal in WF 4.0. Reflection could be used to [get around this restriction][1] but it is a hack at best. This prevents an automated method of walking up the workflow activity hierarchy. Similarly, inspecting child activity hierarchies is unreliable as there is no standard method for exposing or identifying child activities even if they are publicly available on an activity type. This prevents automated discovery of child activities. In addition to these issues, the automatic detection of activity hierarchies for linking activities may produce unintended results as activities are linked when they were not expected to be. 

The alternative to these automated methods is to implement an explicit opt-in design where a parent activity informs the extension about a link to a child activity. This makes the parent activity responsible for informing the extension about a link to a child activity. The extension then uses this knowledge in the processing of the business failure.

{% highlight csharp linenos %}
namespace Neovolve.Toolkit.Workflow.Extensions
{ 
    using System;
    using System.Activities;
    using System.Activities.Persistence;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Threading;
    using System.Xml.Linq;
    using Neovolve.Toolkit.Threading;
    
    public class BusinessFailureExtension<T> : PersistenceParticipant, IDisposable where T : struct
    {
        private static readonly XNamespace _persistenceNamespace = XNamespace.Get("http://www.neovolve.com/toolkit/workflow/properties");
    
        private static readonly XName _scopeEvaluatorsName = _persistenceNamespace.GetName("ScopeEvaluators");
    
        private static readonly XName _scopeFailuresName = _persistenceNamespace.GetName("ScopeFailures");
    
        private readonly ReaderWriterLockSlim _scopeEvaluatorsLock = new ReaderWriterLockSlim();
    
        private readonly ReaderWriterLockSlim _scopeFailuresLock = new ReaderWriterLockSlim();
    
        private Dictionary<String, String> _scopeEvaluators = new Dictionary<String, String>();
    
        private Dictionary<String, Collection<BusinessFailure<T>>> _scopeFailures = new Dictionary<String, Collection<BusinessFailure<T>>>();
    
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
        public IEnumerable<BusinessFailure<T>> GetFailuresForScope(Activity scopeActivity)
        {
            Contract.Requires<ArgumentNullException>(scopeActivity != null);
            Contract.Requires<ArgumentException>(String.IsNullOrEmpty(scopeActivity.Id) == false);
    
            String activityId = scopeActivity.Id;
    
            RemoveActivitiesLinkedToScope(activityId);
    
            using (new LockWriter(_scopeFailuresLock))
            {
                if (_scopeFailures.ContainsKey(activityId))
                {
                    Collection<BusinessFailure<T>> failures = _scopeFailures[activityId];
    
                    _scopeFailures.Remove(activityId);
    
                    return failures;
                }
            }
    
            return null;
        }
    
        public Boolean IsLinkedToScope(Activity activity)
        {
            Contract.Requires<ArgumentNullException>(activity != null);
            Contract.Requires<ArgumentException>(String.IsNullOrEmpty(activity.Id) == false);
    
            String activityId = activity.Id;
    
            String scopeActivityId = GetOwningScopeId(activityId);
    
            if (String.IsNullOrWhiteSpace(scopeActivityId))
            {
                return false;
            }
    
            return true;
        }
    
        public void LinkActivityToScope(Activity scopeActivity, Activity childActivity)
        {
            Contract.Requires<ArgumentNullException>(scopeActivity != null);
            Contract.Requires<ArgumentNullException>(childActivity != null);
            Contract.Requires<ArgumentException>(String.IsNullOrEmpty(scopeActivity.Id) == false);
            Contract.Requires<ArgumentException>(String.IsNullOrEmpty(childActivity.Id) == false);
    
            using (new LockWriter(_scopeEvaluatorsLock))
            {
                _scopeEvaluators[childActivity.Id] = scopeActivity.Id;
            }
        }
    
        public void ProcessFailure(Activity activity, BusinessFailure<T> failure)
        {
            Contract.Requires<ArgumentNullException>(activity != null);
            Contract.Requires<ArgumentException>(String.IsNullOrEmpty(activity.Id) == false);
            Contract.Requires<ArgumentNullException>(failure != null);
    
            String activityId = activity.Id;
    
            String scopeActivityId = GetOwningScopeId(activityId);
    
            if (String.IsNullOrEmpty(scopeActivityId))
            {
                // There is no scope activity that contains this evaluator
                throw new BusinessFailureException<T>(failure);
            }
    
            using (new LockWriter(_scopeFailuresLock))
            {
                Collection<BusinessFailure<T>> failures;
    
                if (_scopeFailures.ContainsKey(scopeActivityId))
                {
                    failures = _scopeFailures[scopeActivityId];
                }
                else
                {
                    failures = new Collection<BusinessFailure<T>>();
    
                    _scopeFailures.Add(scopeActivityId, failures);
                }
    
                // Store the failure for the scope
                failures.Add(failure);
            }
        }
    
        protected override void CollectValues(out IDictionary<XName, Object> readWriteValues, out IDictionary<XName, Object> writeOnlyValues)
        {
            Dictionary<String, String> evaluators;
    
            using (new LockReader(_scopeEvaluatorsLock))
            {
                evaluators = new Dictionary<String, String>(_scopeEvaluators);
            }
    
            Dictionary<String, Collection<BusinessFailure<T>>> scopeFailures;
    
            using (new LockReader(_scopeFailuresLock))
            {
                scopeFailures = new Dictionary<string, Collection<BusinessFailure<T>>>(_scopeFailures);
            }
    
            readWriteValues = new Dictionary<XName, Object>
                                {
                                    {
                                        _scopeEvaluatorsName, evaluators
                                        }, 
                                    {
                                        _scopeFailuresName, scopeFailures
                                        }
                                };
    
            writeOnlyValues = null;
        }
    
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // Free managed resources
                if (Disposed == false)
                {
                    Disposed = true;
    
                    _scopeEvaluatorsLock.Dispose();
                    _scopeFailuresLock.Dispose();
                }
            }
    
            // Free native resources if there are any.
        }
    
        protected override void PublishValues(IDictionary<XName, Object> readWriteValues)
        {
            base.PublishValues(readWriteValues);
    
            Object evaluators;
    
            if (readWriteValues.TryGetValue(_scopeEvaluatorsName, out evaluators))
            {
                using (new LockWriter(_scopeEvaluatorsLock))
                {
                    _scopeEvaluators = (Dictionary<String, String>)evaluators;
                }
            }
    
            Object failures;
    
            if (readWriteValues.TryGetValue(_scopeFailuresName, out failures))
            {
                using (new LockWriter(_scopeFailuresLock))
                {
                    _scopeFailures = (Dictionary<String, Collection<BusinessFailure<T>>>)failures;
                }
            }
        }
    
        private String GetOwningScopeId(String activityId)
        {
            Debug.Assert(String.IsNullOrEmpty(activityId) == false, "No activity id provided");
    
            using (new LockReader(_scopeEvaluatorsLock))
            {
                if (_scopeEvaluators.ContainsKey(activityId))
                {
                    return _scopeEvaluators[activityId];
                }
            }
    
            return null;
        }
    
        private void RemoveActivitiesLinkedToScope(String scopeActivityId)
        {
            List<String> evaluatorIds = new List<String>();
    
            using (new LockReader(_scopeEvaluatorsLock))
            {
                evaluatorIds.AddRange(
                    from valuePair in _scopeEvaluators
                    where valuePair.Value == scopeActivityId
                    select valuePair.Key);
            }
    
            using (new LockWriter(_scopeEvaluatorsLock))
            {
                evaluatorIds.ForEach(x => _scopeEvaluators.Remove(x));
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

The BusinessFailureExtension exposes a LinkActivityToScope method that creates the link between a scope and child activity. Child activities can check if they are linked to a scope by calling the IsLinkedToScope method. The link between these activities uses a Dictionary&lt;String, String&gt; instance to store the associations. The key of the dictionary is the ActivityId of the linked activity and the value is the ActivityId of the scope activity. This design allows for multiple activities to be linked to a scope while enforcing that an activity is only linked to a single scope activity.

The extension defines a ProcessFailure method for processing failures provided by an activity. The extension will throw a BusinessFailureException&lt;T&gt; straight away if the method does not find a link between the failure activity and a scope activity. The failure is stored in a failure list associated with the scope activity if there is a link found with a scope activity.

The GetFailuresForScope method returns any failures stored against a scope activity. This method returns the collection of failures that have been stored against a scope activity when a linked activity has invoked ProcessFailure. This method also cleans up stored information for the scope by removing any links to other activities and removing the failures stored for it.

The extension must support workflow persistence. This caters for the scenario where a linked activity has stored a failure against a scope activity and the workflow is persisted before the scope activity invokes GetFailuresForScope.

![image][2]

Persistence is supported by inheriting from PersistenceParticipant and overriding CollectValues and PublishValues. The CollectValues method provides the activity links and stored failures to the persistence process. The PublishValues method then restores these values again when the workflow is rehydrated from the persistence store.

Lastly, the extension implements IDisposable to ensure that ReaderWriterLock instances are disposed. The workflow execution engine will dispose the extension when the workflow execution has completed.

This post has demonstrated how a custom extension can be used by any activity to work with business failures. The next post will look at the custom activity for evaluating a business failure.

[0]: /2010/10/12/custom-workflow-activity-for-business-failure-evaluatione28093part-2/
[1]: /2010/08/18/getting-meaningful-exceptions-from-wf/
[2]: /files/image_45.png
