---
title: UnityServiceBehavior updated to support named resolutions
categories: .Net
tags: Unity, WCF
date: 2010-07-12 22:43:00 +10:00
---

I [previously posted][0] about how to get Unity support in WCF services via a ServiceBehavior. I found a glaring omission in this implementation when preparing for my [.Net Users Group presentation][1]. The existing code supports named configuration sections and named containers. Unfortunately it left out the more common named instance resolutions.

I have checked in a new code version that supports this. See [UnityServiceElement][2], [UnityServiceBehavior][3] and [UnityInstanceProvider][4] for the updated code line.

[0]: /2010/09/29/custom-windows-workflow-activity-for-dependency-resolutione28093part-2/
[1]: /2010/07/12/canberra-net-users-group-presentation-next-week/
[2]: http://neovolve.codeplex.com/SourceControl/changeset/view/62517#1216930
[3]: http://neovolve.codeplex.com/SourceControl/changeset/view/62517#1149361
[4]: http://neovolve.codeplex.com/SourceControl/changeset/view/62517#1149347
