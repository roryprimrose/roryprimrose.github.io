---
title: How to stream data in WCF service operations
categories : .Net
tags : ASP.Net, WCF
date: 2009-06-02 01:09:44 +10:00
---

[Bruce Zhang][0] has put together a great post about [stream operations in WCF][1]. 

I have never liked the limitations imposed with streaming in WCF although I do agree with the design. The biggest issue for me is that WCF needs to be configured for a maximum transfer size. The main problem here is that a service and a client will probably not know the maximum size of data that a service could process. To know the maximum size would require a business rule in the service design and that would not be very common for a service that handles large amounts of data via a stream.

![][2]

I prefer a chunking service design as you get around all of these limitations with the one disadvantage of creating a chatty service. This introduces a little overhead but I think it is worth it for the benefit of avoiding WCF streaming restrictions.

[0]: http://weblogs.asp.net/brucezhang
[1]: http://weblogs.asp.net/brucezhang/archive/2009/06/01/stream-operation-in-wcf.aspx
[2]: http://weblogs.asp.net/aggbug.aspx?PostID=7105005
