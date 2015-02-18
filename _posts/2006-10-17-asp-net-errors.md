---
title: ASP.Net errors
categories : .Net, IT Related
tags : ASP.Net
date: 2006-10-17 10:29:53 +10:00
---

Sorry people. This is just a note to myself because every few months I come across the same problems with a new development build.

**Failed to access IIS metabase:**

aspnet_regiis -i

**Mutex Could not be Created:**

Solution posted [here][0] by Joao Morais.

> I have got the same issue. It seems like Visual studio 2005 and the web application pool running ASP.NET 2.0 are having a conflict over the temporary folder.  
> The workaround I have got for now is:  
> - If you have visual studio 2005 is open, close it  
> - Go tot the ASP.NET temporary folder for v2.0 of the framework  
>  <Windows dir&gt;\Microsoft.Net\Framework\v2.0<extra numbers&gt;\Temporary ASpNET pages  
> - Remove the folder for your application (or all of them)  
> - Reset IIS (on a command line window, &gt;iisreset) [not always needed, but I had to use it sometimes]  
> - First Browse your page from IE (http://localhost/your app)  
> - Then reopen Visual studio

[0]: http://forums.asp.net/1109781/ShowPost.aspx
