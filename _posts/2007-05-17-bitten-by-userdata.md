---
title: Bitten by UserData
categories: .Net
tags: WF
date: 2007-05-17 17:01:22 +10:00
---

I have written some custom activities for WF, but have recently found a bit of a problem with my implementation. I have wanted to persist information against the workflow instance that contains my custom activity (or many instances of the custom activity). I thought I was being very clever by resolving the owning workflow and then storing my information against the workflows [UserData][0] IDictionary property.

Even the description of the UserData property sounded appropriate according to my good intentions.

> _Gets an IDictionary that associates custom data with this class instance._

The problem that came up was that the data put into this property was persisted between multiple calls to execute the workflow type in the workflow runtime across any user that called it. I have had to change my implementation to store the data elsewhere to avoid this issue.

[0]: http://msdn2.microsoft.com/en-us/library/system.workflow.componentmodel.dependencyobject.userdata
