---
title: TechEd - Morning of Day 2
categories: IT Related
tags: TechEd
date: 2006-08-24 12:36:09 +10:00
---

After my session was finished, I went over to see Payam do his _Windows Communication Foundation: Building Secure Services_ session. I was a little late to this one, but it was a great overview of what you can do with WCF and security. 

Given that I haven't had a lot to do with certificates etc, that part of the session was interesting. It's good so see that in WCF there are a lot of security options available. I like the fact that you can encrypt parts of a message rather than taking the hammer approach and encrypting everything. 

I think it was good that Payam make a clear distinction between scenarios where you want either no security, want to ensure that the message hasn't been tampered with or where you don't want someone to read part or all of a message. I suspect that most people put message tampering and encryption into the same box but they really are different levels of security.

<!--more-->

After the WCF session, I went to _ASP.NET: End-to-End - Building a Complete Web Application Using ASP.NET 2.0, Visual Studio 2005, and IIS 7 (Part 1)_ by [Scott Guthrie][0]. Scott was a great speaker with good stage presence and interactions with the audience. Being a seasoned speaker, I think he handled questions throughout the session really well. He answered a lot of questions during the session (and more afterwards) but he didn't let the questions change the direction of the session.

Because I have spent most of my time developing ASP.Net and the last 18 months or so with VS2005, I didn't learn anything new from the session, but I still really enjoyed it.

After Scott's session, I asked why System.Web.Caching is under System.Web namespace and whether it relies on some of the ASP.Net framework code. There is a lot of cool stuff with the caching support with the cache dependencies and cache expiration that would be really useful outside ASP.Net. He said that it was tied to ASP.Net in some small ways. From memory, he said that they were planning on releasing caching support outside ASP.Net, but there is also caching support today in the [Caching Application Block][1] in the [Enterprise Library][2]. 

Given that Scott used a lot of his break times to answer questions, he didn't have much time before he was supposed to start Part 2 of his session. I was fortunate to have a one-on-one lunch with him before he had to head off for his next session. It was fantastic to chat with Scott about his work at Microsoft. He was interested to hear about .Net adoption in Australia as well. It was a good chat over good food.

[0]: http://weblogs.asp.net/scottgu/
[1]: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnpag2/html/EntLibJan2006_CachingAppBlock.asp
[2]: http://msdn.microsoft.com/library/en-us/dnpag2/html/entlib2.asp
