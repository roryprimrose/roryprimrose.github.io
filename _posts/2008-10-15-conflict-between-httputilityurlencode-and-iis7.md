---
title: Conflict between HttpUtility.UrlEncode and IIS7
categories : .Net
tags : BlogEngine.Net
date: 2008-10-15 12:03:05 +10:00
---

I have [previously posted][0] about the issues I had with CS on IIS7 with + characters in urls. Under the default configuration, IIS7 returns a 404 error because it is concerned about double escaping. With my migration from CS to [BE], I wanted to maintain existing incoming CS links. This means that I still needed to support + characters in urls.

I also found that the TagCloud control in [BE] renders + characters as well. After looking at the code, it eventually calls down into HttpUtility.UrlEncode. This code uses + instead of %20 for encoding spaces. This is a big problem. Any code that uses the HttpUtility.UrlEncode (which is very common), will prevent pages being served correctly on IIS7 without using the workaround indicated in the previous post.

[0]: /post/2008/02/25/fixed-cs-url-encoding-problem-on-vista.aspx
