---
title: Custom Workflow activity for business failure evaluation–Part 4
categories: .Net
tags: WF
date: 2010-10-13 09:44:00 +10:00
---

The [previous post][0] in this series provided the custom workflow extension that manages business failures. This post will provide a custom activity that evaluates a single business failure.

There are three important pieces of information to provide when defining a business failure with WF. These are the type of failure code, the failure condition and the failure details. The BusinessFailureEvaluator&lt;T&gt; activity supports these requirements in a compact way that makes authoring business failures easy in WF.![image][1]![image][2]

The BusinessFailureEvaluator&lt;T&gt; activity is a generic activity type in order to support the generic type requirement of the Code property in BusinessFailure&lt;T&gt;. For ease of use, Int32 is the default type used for the generic type definition. The ArgumentType property can be used to change the activity use a different type for the Code property.

<!--more-->

The activity defines a Nullable&lt;Boolean&gt; Condition property that drives whether the activity results in a failure at runtime. It also supports two methods of defining the failure details. These are to provide a BusinessFailure&lt;T&gt; instance in the Failure property or to define values for the Code and Description properties.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Activities.Validation;
    using System.ComponentModel;
    using System.Drawing;
    using Neovolve.Toolkit.Workflow.Extensions;
    using Neovolve.Toolkit.Workflow.Properties;
     
    [ToolboxBitmap(typeof(ExecuteBookmark), &quot;shield.png&quot;)]
    [DefaultTypeArgument(typeof(Int32))]
    public sealed class BusinessFailureEvaluator<T> : CodeActivity where T : struct
    {
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            metadata.RequireExtension<BusinessFailureExtension<T>>();
            metadata.AddDefaultExtensionProvider(() => new BusinessFailureExtension<T>());
    
            Boolean conditionBound = Condition != null && Condition.Expression != null;
    
            if (conditionBound == false)
            {
                ValidationError validationError = new ValidationError(Resources.BusinessFailureEvaluator_NoConditionBoundWarning, true, &quot;Condition&quot;);
    
                metadata.AddValidationError(validationError);
            }
    
            Boolean failureBound = Failure != null && Failure.Expression != null;
            Boolean codeBound = Code != null && Code.Expression != null;
            Boolean descriptionBound = Description != null && Description.Expression != null;
            const String CodePropertyName = &quot;Code&quot;;
    
            if (failureBound == false && codeBound == false && descriptionBound == false)
            {
                ValidationError validationError = new ValidationError(
                    Resources.BusinessFailureEvaluator_FailureInformationNotBound, false, CodePropertyName);
    
                metadata.AddValidationError(validationError);
            }
            else if (failureBound && (codeBound || descriptionBound))
            {
                ValidationError validationError = new ValidationError(
                    Resources.BusinessFailureEvaluator_ConflictingFailureInformationBound, false, CodePropertyName);
    
                metadata.AddValidationError(validationError);
            }
            else if (codeBound && descriptionBound == false)
            {
                ValidationError validationError = new ValidationError(Resources.BusinessFailureEvaluator_DescriptionNotBound, false, &quot;Description&quot;);
    
                metadata.AddValidationError(validationError);
            }
            else if (codeBound == false && descriptionBound)
            {
                ValidationError validationError = new ValidationError(Resources.BusinessFailureEvaluator_CodeNotBound, false, CodePropertyName);
    
                metadata.AddValidationError(validationError);
            }
    
            base.CacheMetadata(metadata);
        }
    
        protected override void Execute(CodeActivityContext context)
        {
            Nullable<Boolean> condition = Condition.Get(context);
    
            if (condition != null && condition == false)
            {
                // This is not a failure
                return;
            }
    
            BusinessFailure<T> failure = Failure.Get(context);
    
            if (failure == null)
            {
                T code = Code.Get(context);
                String description = Description.Get(context);
    
                failure = new BusinessFailure<T>(code, description);
            }
    
            BusinessFailureExtension<T> extension = context.GetExtension<BusinessFailureExtension<T>>();
    
            extension.ProcessFailure(this, failure);
        }
    
        [Category(&quot;Inputs&quot;)]
        [Description(&quot;The code of the failure&quot;)]
        public InArgument<T> Code
        {
            get;
            set;
        }
    
        [Category(&quot;Inputs&quot;)]
        [Description(&quot;The condition used to determine if there is a failure&quot;)]
        public InArgument<Nullable<Boolean>> Condition
        {
            get;
            set;
        }
    
        [Category(&quot;Inputs&quot;)]
        [Description(&quot;The description of the failure&quot;)]
        public InArgument<String> Description
        {
            get;
            set;
        }
    
        [Category(&quot;Inputs&quot;)]
        [Description(&quot;The failure&quot;)]
        public InArgument<BusinessFailure<T>> Failure
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The CacheMetadata method identifies that the activity requires a BusinessFailureExtension&lt;T&gt; extension and provides a default function to create one if it does not yet exist. The remainder of the method defines a set of validation rules for the activity. If no Condition is bound then the designer will display a warning message on the activity to indicate that this will always result in an exception.

![image][3]

The validation logic then checks for errors with the configuration of the activity. A Failure value or Code and Description must be provided and these are values are mutually exclusive. If a Code has been bound then a Description must also be bound and visa versa. Like the above warning, these validation errors are displayed in the designer surface however they also result in an exception thrown by the workflow engine if the activity is executed without addressing these errors.

![image][4]

The Execute method of the activity is where all the runtime processing occurs. 

The only way that the activity will not result in an exception is if a Condition expression has been provided and its runtime value is false. Not binding the Condition property, providing a null value or providing a value of true means that a failure will be processed by the extension. The ability to not bind a Condition expression is useful where prior WF processing (such as an If statement) has already processed the conditional logic for the failure.

The activity will resolve a BusinessFailure&lt;T&gt; instance once the Execute method has determined that a business failure has been encountered. It first looks for a Failure property instance and will then create a new instance with Code and Description properties if it does not find a failure instance. The failure instance is then passed on to the extension for processing.

This post has shown how to create a simple business failure evaluation activity with validation support. The next post will look at how to provide the ability to group a set of failures using a composite activity.

[0]: /2010/10/12/custom-workflow-activity-for-business-failure-evaluatione28093part-3/
[1]: /files/image_46.png
[2]: /files/image_47.png
[3]: /files/image_48.png
[4]: /files/image_49.png
