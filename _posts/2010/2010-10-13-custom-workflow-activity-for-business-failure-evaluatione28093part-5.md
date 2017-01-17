---
title: Custom Workflow activity for business failure evaluation–Part 5
categories: .Net
tags: WF
date: 2010-10-13 12:47:00 +10:00
---

The [previous post][0] in this series provided the custom activity that evaluates a single business failure in WF. One of the [design goals][1] of this series is to support the evaluation and notification of multiple failures. This post will provide a custom activity that supports this design goal.

At the very least, this activity needs to be able to contain multiple BusinessFailureEvaluator&lt;T&gt; activities. The design of this control will be like the Sequence activity where it can define and execute a collection of child activities. ![][2]

There is no reason to restrict the child activities to the BusinessFailureEvaluator activity type so it will allow any child activity type. The screenshot above demonstrates this by adding an ExecuteBookmark activity in the middle of the business evaluators within the scope. ![image][3]

<!--more-->

The activity has the same behaviour as BusinessFailureEvaluator for managing the generic type on the activity. It defaults to using Int32 and exposes an ArgumentType property to change the type after the activity is on the designer.

```csharp
namespace Neovolve.Toolkit.Workflow.Activities
{ 
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.Windows.Markup;
    using Neovolve.Toolkit.Workflow.Extensions;
    
    [ContentProperty(&quot;Activities&quot;)]
    [ToolboxBitmap(typeof(ExecuteBookmark), &quot;shield_go.png&quot;)]
    [DefaultTypeArgument(typeof(Int32))]
    public sealed class BusinessFailureScope<T> : NativeActivity where T : struct
    {
        private readonly Variable<Int32> _activityExecutionIndex = new Variable<Int32>();
    
        public BusinessFailureScope()
        {
            Activities = new Collection<Activity>();
            Variables = new Collection<Variable>();
        }
    
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.RequireExtension<BusinessFailureExtension<T>>();
            metadata.AddDefaultExtensionProvider(() => new BusinessFailureExtension<T>());
            metadata.SetChildrenCollection(Activities);
            metadata.SetVariablesCollection(Variables);
            metadata.AddImplementationVariable(_activityExecutionIndex);
        }
    
        protected override void Execute(NativeActivityContext context)
        {
            if (Activities == null)
            {
                return;
            }
    
            if (Activities.Count <= 0)
            {
                return;
            }
    
            Activity activity = Activities[0];
    
            ExecuteChildActivity(context, activity);
        }
    
        private void CompleteScope(NativeActivityContext context)
        {
            BusinessFailureExtension<T> extension = context.GetExtension<BusinessFailureExtension<T>>();
    
            IEnumerable<BusinessFailure<T>> businessFailures = extension.GetFailuresForScope(this);
    
            if (businessFailures == null)
            {
                return;
            }
    
            throw new BusinessFailureException<T>(businessFailures);
        }
    
        private void ExecuteChildActivity(NativeActivityContext context, Activity activity)
        {
            BusinessFailureExtension<T> extension = context.GetExtension<BusinessFailureExtension<T>>();
    
            extension.LinkActivityToScope(this, activity);
    
            context.ScheduleActivity(activity, OnActivityCompleted);
        }
    
        private void OnActivityCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            Int32 currentIndex = _activityExecutionIndex.Get(context);
    
            if ((currentIndex >= Activities.Count) || (Activities[currentIndex] != completedInstance.Activity))
            {
                currentIndex = Activities.IndexOf(completedInstance.Activity);
            }
    
            Int32 nextActivityIndex = currentIndex + 1;
    
            if (nextActivityIndex != Activities.Count)
            {
                Activity activity = Activities[nextActivityIndex];
    
                ExecuteChildActivity(context, activity);
    
                _activityExecutionIndex.Set(context, nextActivityIndex);
            }
            else
            {
                CompleteScope(context);
            }
        }
    
        [Browsable(false)]
        public Collection<Activity> Activities
        {
            get;
            set;
        }
    
        [Browsable(false)]
        public Collection<Variable> Variables
        {
            get;
            set;
        }
    }
}
```

The activity uses the CacheMetadata method to identify that it requires a BusinessFailureExtension&lt;T&gt; extension and provides the method to create one if it does not already exist. This method also configures the activity to support child activities and variable definitions.

The activity execution will resolve the extension instance before executing a child activity. It notifies the extension about the link between the scope and the child activity. The child activity can then use the extension to [add a failure][0] which the extension will [store on behalf of the scope][4]. The activity then gets the failures for the scope from the extension when it has executed all child activities. It then throws a BusinessFailureException&lt;T&gt; if there are any failures reported. 

On a side note, the next version of this activity will refactor this last step so that the extension manages the exception throwing process as it does for BusinessFailureEvaluator&lt;T&gt;. This will align the code with the Single Responsibility Pattern so that only the extension understands how to handle failures and how to throw the BusinessFailureException&lt;T&gt;.

This post has provided the implementation for handling multiple business failures in a set. The next post will provide the designer implementation for these two activities.

[0]: /2010/10/12/custom-workflow-activity-for-business-failure-evaluatione28093part-4/
[1]: /2010/10/11/custom-workflow-activity-for-business-failure-evaluatione28093part-1/
[2]: /files/image_45.png
[3]: /files/image_50.png
[4]: /2010/10/12/custom-workflow-activity-for-business-failure-evaluatione28093part-3/
