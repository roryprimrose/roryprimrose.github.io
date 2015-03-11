---
title: Custom Workflow activity for business failure evaluation–Part 2
categories : .Net, Applications
tags : WF
date: 2010-10-12 13:28:10 +10:00
---

The [previous post][0] provided the high level design requirements for a custom WF activity that evaluates business failures. This post will provide the base classes that will support the custom WF activity.

The first issue to work on is how to express a business failure. The design requirements indicated that a business failure needs to identify a code and a description. The design also defined that the code value must be generic in order to avoid placing an implementation constraint on the consuming application.  

<!--more-->

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow
{
    using System;
    using System.Diagnostics.Contracts;
    using Neovolve.Toolkit.Workflow.Properties;
    
    [Serializable]
    public class BusinessFailure<T> where T : struct
    {
        public BusinessFailure(T code, String description)
        {
            Contract.Requires<ArgumentNullException>(String.IsNullOrEmpty(description) == false);
    
            Code = code;
            Description = description;
        }
    
        public static BusinessFailure<T> UnknownFailure
        {
            get
            {
                return new BusinessFailure<T>(default(T), Resources.BusinessFailure_UnknownFailure);
            }
        }
    
        public T Code
        {
            get;
            private set;
        }
    
        public String Description
        {
            get;
            private set;
        }
    }
}
{% endhighlight %}

The BusinessFailure&lt;T&gt; class supports these design goals. There is a constraint defined for the code type T that enforces the type to be a struct. Primarily this is to ensure that the code value is serializable and enforces failure codes to be simple types. One thing to note about this class is that it is marked as Serializable. This is critically important in order to support scenarios where a business failure has been created but not yet processed before the executing workflow is persisted.

The next part of the design to address is how a business failure gets processed. The design describes that this will be done using a custom exception. The exception allows calling applications have access to all the failures related to an exception and enforces applications to leverage structured error handling practises to process business failures.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Runtime.Serialization;
    using Neovolve.Toolkit.Workflow.Properties;
    
    [Serializable]
    public class BusinessFailureException<T> : Exception where T : struct
    {
        private const String FailuresKey = &quot;FailuresKey&quot;;
    
        private const String IncludeBaseMessageKey = &quot;IncludeBaseMessageKey&quot;;
    
        public BusinessFailureException()
        {
            IncludeBaseMessage = false;
            Failures = new Collection<BusinessFailure<T>>();
        }
    
        public BusinessFailureException(String message)
            : base(message)
        {
            IncludeBaseMessage = true;
            Failures = new Collection<BusinessFailure<T>>();
        }
    
        public BusinessFailureException(T code, String description)
        {
            IncludeBaseMessage = false;
            BusinessFailure<T> failure = new BusinessFailure<T>(code, description);
    
            Failures = new Collection<BusinessFailure<T>>
                        {
                            failure
                        };
        }
    
        public BusinessFailureException(BusinessFailure<T> failure)
        {
            IncludeBaseMessage = false;
            Failures = new Collection<BusinessFailure<T>>
                        {
                            failure
                        };
        }
    
        public BusinessFailureException(IEnumerable<BusinessFailure<T>> failures)
        {
            IncludeBaseMessage = false;
            Failures = new List<BusinessFailure<T>>(failures);
        }
    
        public BusinessFailureException(String message, Exception inner)
            : base(message, inner)
        {
            IncludeBaseMessage = true;
            Failures = new Collection<BusinessFailure<T>>();
        }
    
        protected BusinessFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            IncludeBaseMessage = info.GetBoolean(IncludeBaseMessageKey);
            Failures = (ICollection<BusinessFailure<T>>)info.GetValue(FailuresKey, typeof(Collection<BusinessFailure<T>>));
        }
    
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(IncludeBaseMessageKey, IncludeBaseMessage);
            info.AddValue(FailuresKey, Failures);
    
            base.GetObjectData(info, context);
        }
    
        public IEnumerable<BusinessFailure<T>> Failures
        {
            get;
            private set;
        }
    
        public override String Message
        {
            get
            {
                String message = String.Empty;
    
                if (IncludeBaseMessage)
                {
                    message = base.Message;
                }
    
                String failureMessages = String.Empty;
    
                List<BusinessFailure<T>> businessFailures = new List<BusinessFailure<T>>(Failures);
    
                if (businessFailures.Count == 0)
                {
                    // Add a default failure
                    businessFailures.Add(BusinessFailure<T>.UnknownFailure);
                }
    
                for (Int32 index = 0; index < businessFailures.Count; index++)
                {
                    BusinessFailure<T> failure = businessFailures[index];
                    String failureMessage = String.Format(
                        CultureInfo.CurrentUICulture, Resources.BusinessFailureException_FailureMessageFormat, failure.Code, failure.Description);
    
                    failureMessages = String.Concat(failureMessages, Environment.NewLine, failureMessage);
                }
    
                message += String.Format(CultureInfo.CurrentUICulture, Resources.BusinessFailureException_MessageHeader, failureMessages);
    
                return message;
            }
        }
    
        protected Boolean IncludeBaseMessage
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The code analysis rules provided by Microsoft defines the bulk of the signatures on this type. This is done to provide a consistent exception implementation for consumers. The business failure specific parts of this implementation are in three areas:

1. Exception construction with business failure information
1. Custom Message property generation to include failure information
1. Access to the set of BusinessFailure&lt;T&gt; entities
1. Serialization
    
The custom Message property is important for human readable scenarios, such as output to a UI or writing the exception to a log entry. Exposing the set of BusinessFailure&lt;T&gt; instances is important for automated referencing, such as associating UI field failure indicators using the failure code as the link.

The next post in the series will provide the implementation of a WF activity that evaluates and processes a business failure.

[0]: /post/2010/10/11/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-1.aspx
