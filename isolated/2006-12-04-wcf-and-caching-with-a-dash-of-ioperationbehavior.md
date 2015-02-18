---
title: WCF and caching with a dash of IOperationBehavior
categories : .Net, IT Related
tags : WCF
date: 2006-12-04 12:16:03 +10:00
---

I have been looking at caching support and WCF recently. I have come across a few solutions. These are:

1. Calling through HttpRuntime to get at the ASP.Net cache
1. Using Enterprise Library Caching Block
1. Rolling your own implementation, usually storing data in a Dictionary<String, Object&gt; collection.
I have problems with 1 because that ties your WCF service down to an IIS and HTTP solution which is not good design. Option 3 is also not that great because typically this is built as a 'quick and dirty' solution that doesn't support any cache dependency or cache expiration policies. I haven't played with 2 yet, but it looks like that will be the go.

On a side note, I came across a [code sample][0] by [Scott Mason][1] (where's the blog Scott???) that uses an [IOperationBehavior][2] implementation to hook the operation call to provide caching support outside the actual implementation of the service operation. This is a great idea. The only thing I don't like about it is that operation behaviors can't be set via application configuration. They can only be assigned to an operation (WCF method) via a custom attribute on the method or via the behaviors collection at runtime. Pity, it would be so nice if you could plug in different behaviors via configuration.

[0]: http://wcf.netfx3.com/files/folders/operation_invocation/entry3785.aspx
[1]: http://wcf.netfx3.com/user/Profile.aspx?UserID=2149
[2]: http://msdn2.microsoft.com/en-US/library/system.servicemodel.description.ioperationbehavior.aspx
