---
title: BusinessFailureScope activity with deep nested support
tags : WF
date: 2011-03-13 13:50:00 +10:00
---

I wrote a [series of posts][0] late last year about a custom WF activity that collates a set of business failures that child evaluator activities identify into a single exception. At the time, the only way I could get the child evaluator activities to communicate failures to the parent scope activity was by using a [custom WF extension][1] to store the failures and manage the exception throwing logic. ![][2]

The relationship between the parent scope activity and child evaluator activity works like this. The parent scope activity registers all the child evaluator activities with the extension in order to create a link between them. The extension then holds on to the failures from the child activities so that it can throw an exception for them as a set when the parent scope completes. If there was no link between the parent scope and a child evaluator then the extension would throw the exception directly for the singular failure.

One of the limitations of this design was that I could not create a link between a parent scope and child evaluator activity when the child activity was not a direct descendent. This was quite limiting because you could not branch off into validation checks and run evaluators within those sub-branches. You can see from the [post that describes the extension][1] that the implementation is also very messy. 

If only I had known about [Workflow Execution Properties][3] back then. You can see a good description of execution properties in [Tim’s post][4]. 

A great feature of execution properties in a workflow is that they are scoped to a specific sub-tree of the workflow structure. This is perfect for managing sets of failures between the parent scope and child evaluator activities. This method also allows for child evaluators to communicate failures to the parent scope from any depth of the workflow sub-tree.![image][5]

{% highlight csharp linenos %}
namespace Neovolve.Toolkit.Workflow.Activities
{
    using System;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;
    
    [DataContract]
    internal class BusinessFailureInjector<T> where T : struct
    {
        public BusinessFailureInjector()
        {
            Failures = new Collection<BusinessFailure<T>>();
        }
    
        public static String Name
        {
            get
            {
                return "BusinessFailureInjector" + typeof(T).FullName;
            }
        }
    
        [DataMember]
        public Collection<BusinessFailure<T>> Failures
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

An instance of the BusinessFailureInjector<T> class is the value that is added to the parent scope’s execution context as an execution property. This class simply holds the failures that child evaluator activities find. One thing to notice about BusinessFailureInjector is the usage of DataContract and DataMember attributes. WF does all the heavy lifting for us with regard to persistence. The data held in the execution property automatically gets persisted and then restored for us. This was done manually in the [old extension version][6] as well as manually tracking the links between scopes and evaluators. 

There are some minor changes to the code in the [BusinessFailureScope][7] and [BusinessFailureEvaluator][8] activities to work with the execution property rather than the extension. 

The parent scope adds the execution property to its context when it is executed.

{% highlight csharp linenos %}
BusinessFailureInjector<T> injector = new BusinessFailureInjector<T>();
    
context.Properties.Add(BusinessFailureInjector<T>.Name, injector);
{% endhighlight %}

If the child activity can’t find the execution property then it throws the failure exception straight away for the single failure. If the execution property is found then it adds its failure to the collection of failures.

{% highlight csharp linenos %}
BusinessFailureInjector<T> injector = context.Properties.Find(BusinessFailureInjector<T>.Name) as BusinessFailureInjector<T>;
    
if (injector == null)
{
    throw new BusinessFailureException<T>(failure);
}
    
injector.Failures.Add(failure);
{% endhighlight %}

The parent scope then checks with the execution property to determine if there are any failures to throw in an exception.

{% highlight csharp linenos %}
private static void CompleteScope(NativeActivityContext context)
{
    BusinessFailureInjector<T> injector = context.Properties.Find(BusinessFailureInjector<T>.Name) as BusinessFailureInjector<T>;
    
    if (injector == null)
    {
        return;
    }
    
    if (injector.Failures.Count == 0)
    {
        return;
    }
    
    throw new BusinessFailureException<T>(injector.Failures);
}
{% endhighlight %}

Overall, the complexity of the code has been significantly reduced by this this method of inter-activity communication. In addition, child evaluators can now exist anywhere under a parent scope activity and still have their failures managed by the parent scope.

You can download the updated version of BusinessFailureScope in the latest beta of my [Neovolve.Toolkit][9] project out on CodePlex.

[0]: /post/2010/10/13/Custom-Workflow-activity-for-business-failure-evaluatione28093Wrap-up.aspx
[1]: /post/2010/10/12/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-3.aspx
[2]: //files/image_45.png
[3]: http://msdn.microsoft.com/en-us/library/ee358743.aspx
[4]: http://blogs.msdn.com/b/tilovell/archive/2009/12/20/workflow-scopes-and-execution-properties.aspx?wa=wsignin1.0
[5]: //files/image_92.png
[6]: http://neovolve.codeplex.com/SourceControl/changeset/view/74306#1422353
[7]: http://neovolve.codeplex.com/SourceControl/diff/file/view/74545?fileId=1422354
[8]: http://neovolve.codeplex.com/SourceControl/diff/file/view/74545?fileId=1422366
[9]: http://neovolve.codeplex.com/releases/view/53499
