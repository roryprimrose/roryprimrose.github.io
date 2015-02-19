---
title: XBAP through a proxy server
categories : .Net, IT Related
tags : WPF
date: 2006-10-06 08:45:59 +10:00
---

I have just tried to view some [XBAP applications][0] using RC1 of [NetFx]. Unfortunately it didn't go so well because I am sitting behind a proxy server. IE has it's connection settings defined for how to go through the proxy, but it seems that the XBAP host isn't doing the same. I am getting this error:

> _System.Net.WebException: The remote server returned an error: (407) Proxy Authentication Required._

I have found some references (such as [here][1] and [here][2]) which mention changes to the machine.config, but this hasn't solved the problem. Has anyone been able to get around this problem?

[0]: http://scorbs.com/2006/06/16/woodgrove-demo
[1]: http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=298524&amp;SiteID=1
[2]: https://blogs.msdn.com/tims/archive/2005/11/28/497492.aspx
