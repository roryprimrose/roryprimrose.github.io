---
title: ThreadStatic gotcha with ASP.Net
categories : .Net
tags : ASP.Net
date: 2009-09-23 11:40:42 +10:00
---

I had a requirement recently in a service implementation that business rules at any point throughout the execution of a service request could determine a message to return in the header of the response. Because these circumstances don’t affect the execution of the request (no exception is thrown), I needed a loosely coupled way of recording all the required messages and handling them higher up the stack.

Storing data against the executing thread was the obvious solution. I considered manually storing information in the context data of the thread, but then thought this would be a great opportunity to use the [ThreadStaticAttribute][0]. I coded this up and it all seemed to work well.

<!--more-->

Yesterday a bug was raised against this code because messages unrelated to the service request were being returned. I looked at the code and rechecked the ThreadStatic documentation. It shouldn’t be possible, but it was definitely happening. 

I quickly realised that the thread must be being reused across multiple service requests. This reminded me that ASP.Net uses a ThreadPool and it would not be doing any clean up of ThreadStatic data as it reused an existing thread for a new service request. 

The simple fix was to reset the ThreadStatic reference when the response is generated.

[0]: http://msdn.microsoft.com/en-us/library/system.threadstaticattribute(VS.71).aspx
