---
title: Developing multi-threaded workflows
categories: .Net
tags: WF
date: 2010-08-06 12:21:00 +10:00
---

Most people (myself included) assume that the Parallel and ParallelForEach&lt;t&gt;
    activities in WF4 run each child in parallel on multiple threads. Unfortunately this is not the case. Each child activity is scheduled in the workflow runtime at the same time. The child activities will only start running in “parallel” if one of the branches is in a waiting state. You can read [this post][0] which links to [this post][1] for some more detailed information

You can achieve multi-threaded parallel execution by using AsyncCodeActivity derived activities (such as InvokeMethod) with the RunAsynchronously set to True running in a Parallel or ParallelForEach&lt;t&gt;
activity. Consider the following workflow.

<!--more-->

![image][2]

Each parallel branch writes the start time to the console, runs a Thread.Sleep for 2, 4 or 6 seconds and writes the end time. The following is written to the console if each InvokeMethod activity has RunAsynchronously set to False.

{% highlight text %}
Starting - 6/08/2010 10:41:38 AM
Starting First - 6/08/2010 10:41:38 AM
Finishing First - 6/08/2010 10:41:40 AM
Starting Second - 6/08/2010 10:41:41 AM
Finishing Second - 6/08/2010 10:41:45 AM
Starting Third - 6/08/2010 10:41:45 AM
Finishing Third - 6/08/2010 10:41:51 AM
Completed - 6/08/2010 10:41:51 AM
{% endhighlight %}

Each branch in the parallel is waiting until the previous branch has completed. The following is written to the console if each InvokeMethod activity has RunAsynchronously set to True.

{% highlight text %}
Starting - 6/08/2010 10:47:35 AM
Starting First - 6/08/2010 10:47:35 AM
Starting Second - 6/08/2010 10:47:35 AM
Starting Third - 6/08/2010 10:47:35 AM
Finishing First - 6/08/2010 10:47:37 AM
Finishing Second - 6/08/2010 10:47:39 AM
Finishing Third - 6/08/2010 10:47:41 AM
Completed - 6/08/2010 10:47:41 AM
{% endhighlight %}

Each parallel branch has now executed on different threads. The overall affect is that the workflow now runs must faster.

ParallelForEach&lt;t&gt; works in the same manner. This workflow can be refactored to the following:

![image][3]

The result without asynchronous processing is:

{% highlight text %}
Starting - 6/08/2010 11:55:09 AM
Starting 1 - 6/08/2010 11:55:09 AM
Finishing 1 - 6/08/2010 11:55:11 AM
Starting 2 - 6/08/2010 11:55:11 AM
Finishing 2 - 6/08/2010 11:55:16 AM
Starting 3 - 6/08/2010 11:55:16 AM
Finishing 3 - 6/08/2010 11:55:22 AM
Completed - 6/08/2010 11:55:22 AM
{% endhighlight %}
    
The result with asynchronous processing is:

{% highlight text %}
Starting - 6/08/2010 11:56:02 AM
Starting 1 - 6/08/2010 11:56:02 AM
Starting 2 - 6/08/2010 11:56:02 AM
Starting 3 - 6/08/2010 11:56:02 AM
Finishing 1 - 6/08/2010 11:56:04 AM
Finishing 2 - 6/08/2010 11:56:07 AM
Finishing 3 - 6/08/2010 11:56:08 AM
Completed - 6/08/2010 11:56:08 AM
{% endhighlight %}

What happens when an exception is thrown? If the exception is thrown in the parallel branch, it will stop executing all the other running branches and the exception will be thrown up the call stack. Any exceptions thrown from within the asynchronous code will have the exception pushed back onto the original workflow execution thread and then thrown. Effectively there is no difference regarding where the exception is thrown from, the parallel branches will be stopped.

[0]: http://blogs.msdn.com/b/xiaowen/archive/2009/10/25/threadedness-in-wf.aspx
[1]: http://blogs.msdn.com/b/advancedworkflow/archive/2006/02/23/538160.aspx
[2]: /files/image_23.png
[3]: /files/image_24.png
