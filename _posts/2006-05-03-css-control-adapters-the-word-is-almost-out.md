---
title: CSS/Control Adapters - The word is almost out
categories: .Net, IT Related
tags: ASP.Net
date: 2006-05-03 02:27:12 +10:00
---

I have been developing some control adapters over the last month. Control adapters are really cool stuff for ASP.Net. They allow you to modify the rendering behavior of a control in a way that is independent from the control and the web site code. Scott Guthrie posted about control adapters [late last year][0] and [again more recently][1].

One of the greatest uses I have come across for control adapters is for developers to use controls that they are familiar with, such as the intrinsic ASP.Net controls, but modify the html they produce to be more suitable to the application. I am involved in a web project that requires CSS to be used for document layout rather than tables. By using adapters, I can allow the developers to use a FormView control which they are familiar with and the adapter will take over the rendering of that control to ensure that tables are not rendered.

I was going to put out an example of creating adapters, but it looks like the ASP.Net crew might [beat me to it][2]. I kind of suspect their solutions might be different to mine so it will be interesting to see what they come out with.

[0]: http://weblogs.asp.net/scottgu/archive/2005/12/21/433692.aspx
[1]: http://weblogs.asp.net/scottgu/archive/2006/03/30/441465.aspx
[2]: http://www.asp.net/cssadapters/
