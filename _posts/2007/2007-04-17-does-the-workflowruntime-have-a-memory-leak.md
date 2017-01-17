---
title: Does the WorkflowRuntime have a memory leak?
categories: .Net
tags: WCF, WF
date: 2007-04-17 12:15:52 +10:00
---

Over the last week at work, we have been doing some testing of our WCF services that call into a business layer that uses WF. We have the services set as single instance, and we are using a new instance of the WorkflowRuntime for each service call. There are obvious inefficiencies of creating the runtime for each call, but that isn't the issue. The problem is that we noticed that the memory went up and didn't get released (until the AppDomain was unloaded).

After a lot of testing, it came down to the WorkflowRuntime instances that were remaining in memory, even through all of our other objects were cleaned up and disposed. We did everything we could to release the runtime from our code but the memory issues remained. So, is there are memory leak in the runtime?

The solution seems to be to wrap the runtime as a singleton. Singletons have been bashed a lot over recent months and while I agree with the criticism over the misuse/overuse of singletons, I think this is an appropriate case for one. Initial testing has shown that there is no memory problems once only a single WorkflowRuntime was used. This also comes with the performance benefit of not having to create and destroy the runtimes for each call.

[Scott Allen][0] has [some more pitfalls][1] to watch out for with the runtimes.

[0]: http://odetocode.com/Blogs/scott/
[1]: http://odetocode.com/Blogs/scott/archive/2007/01/20/9882.aspx
