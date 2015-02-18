---
title: Lack of softie unit testing
categories : .Net, IT Related
tags : ASP.Net, Unit Testing, WF
date: 2006-04-07 02:26:00 +10:00
---

 I have been trying to pull off something very interesting in recent days. I need to hijack the rendering of a DetailsView control so that it renders different html rather than the inbuilt table element structure. 

 I have mostly pulled this off by using browser files and control adapters to hook into the rendering process at which time I can get the control to render into a custom HtmlTextWriter. This is great because the page developer just needs to use Microsoft&#39;s DetailsView rather than a custom one. 

 There is a hitch though. In an attempt to allow certain fields to not render in certain DetailsView modes, I need to render additional information into the html which the HtmlTextWriter can pick up so it will know what HTML to ignore and what HTML to render. 

 Overriding the RenderBeforeTag and RenderAfterTag of the HtmlTextWriter looked like the go. Unfortunately there is a really simple bug in the framework which I can&#39;t do anything about. See [here][0] for more info. 

 Simple unit testing should have picked this one up. This puts a serious road-block in my path. 

[0]: http://lab.msdn.microsoft.com/ProductFeedback/viewFeedback.aspx?feedbackId=FDBK48311
