---
title: Databinding not firing
categories : .Net, IT Related
tags : ASP.Net
date: 2006-11-21 14:28:00 +10:00
---

Here's a little tip. If your databound control is not firing it's databinding, make sure that the control and all of its parents are visible on the page. If it isn't visible, OnPreRender doesn't fire and therefore your databinding won't either. Makes a lot of sense. There is no point having the overhead of databinding if you are not going to display anything. That just wasted a couple of hours on a really simple mistake.


