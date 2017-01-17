---
title: When a bug isn't a bug, but still requires a workaround
categories: .Net
date: 2005-08-15 05:48:00 +10:00
---

I have been playing with Whidbey ASP.Net over the last week, developing some web custom controls that need to publish some resources along with the control. 

I have previously written an article about how to publish resources from the controls assembly in 1.1. This works really well, but requires you to do the resource extraction and HTTP handling yourself. This isn't a huge problem as most of the code can be reused and is compiled into the resource, therefore very portable. The big problem with it is that after the assembly is referenced by the web project, there is a little bit more work to do to get it working. The developer has to add the httpHandler to the web.config so that the site knows how to intepret the request for the resource.

<!--more-->

Whidbey has some new tricks up its sleeve. On the surface, it looks like a great implementation, but I have found some problems with it. There is a [good article][0] on MSDN that gives a great overview of how it all works. It is a little out of date though as there have been some changes.

One of the great changes in the new version is that the httphandler is inbuilt (for the webresource.axd url) as it is defined in the machine.config instead of being required in each web sites web.config. The url to hit the resource is found by calling Page.ClientScript.GetWebResourceURL to which you pass a Type (from which the assembly is obtained) and the name of the resource you want.

Like I said, on the surface, this looks great, but there are a few things I don't like about this implementation. 

Firstly, this implementation requires that you register the resource with the assembly config as one that can be served by webresource.axd. This is for security. It isn't as bad as registering httpHandlers in the sites web.config because this is in the controls assembly and only has to be done in one place. One problem with this is that it is a piece of the puzzle that could be easily over looked or incorrectly typed. It would be nice if this was a property of the resource rather than requiring the developer to enter <code>&lt;Assembly: WebResource(\[Name\], \[ContentType\] /&gt;</code> into AssemblyInfo.vb (which is also hidden from the IDE by default).

Secondly, the compiler prefixes the name of the resource with the root namespace of the assembly. This means, for example, that test.gif now turns into MyRoot.Web.Controls.test.gif. This is the name of the resource that must be not only defined in AssemblyInfo.vb with <code>&lt;Assembly: WebResource("MyRoot.Web.Controls.test.gif", "image/gif") /&gt;</code>, but also when you call Page.ClientScript.GetWebResourceURL. Very intuitive right?

It is the second point that causes me the most grief. Most people will not know that the namespace has been compiled into the resources name and wonder why web resources don't work in Whidbey. I always have a nice namespace hierarchy for my web controls. Each control is under that namespace. This means that when Page.ClientScript.GetWebResourceURL is called, it can be a little more dynamic by saying Page.ClientScript.GetWebResourceURL(Me.GetType(), Me.GetType.Namespace & ".test.gif"). 

What happens when the class that calls this is wrapped in a Namespace statement. The resource won't be found. To avoid that problem, the namespace has to be hardcoded to be Page.ClientScript.GetWebResourceURL(Me.GetType(), "MyRoot.Web.Controls.test.gif"). Similarly, the registration of the resource in AssemblyInfo.vb must be hardcoded with the namespace as a string as it is a compile time configuration.

So what happens when the namespace of the assembly is changed? Broken resources all over the shop.

Many people have tried to highlight this to Microsoft's Product Feedback Center (read [here][1], [here][2] and [here][3]). The first link is the most interesting one. The response from Microsoft is a workaround of removing the root namespace of the assembly, or hardcode the namespace in all of your resource references. This means that each of your classes must be wrapped in Namespace statements, or lose flexibility by hardcoding namespaces.

Why is the resource compiled with the namespace as the prefix to its name? No idea, but what benefit does it bring? I still have no idea. It does have these obvious down sides though. It is a shame that Microsoft have taken the stance on this that they have (read outcome of the first feedback link above).

[0]: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnvs05/html/webresource.asp
[1]: http://lab.msdn.microsoft.com/productfeedback/viewfeedback.aspx?feedbackid=c1ca82bd-ee99-422d-8933-d88e7e7abb7e
[2]: http://lab.msdn.microsoft.com/productfeedback/viewfeedback.aspx?feedbackid=3ea9f497-0edf-415a-9fb2-59d009cf130b
[3]: http://lab.msdn.microsoft.com/productfeedback/viewfeedback.aspx?feedbackid=7ccc7bb3-a6ae-4900-bfb2-2fb46177871a
