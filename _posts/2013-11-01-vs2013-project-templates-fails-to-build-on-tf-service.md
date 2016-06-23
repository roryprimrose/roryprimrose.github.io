---
title: VS2013 project templates fails to build on TF Service
categories: .Net
tags: Azure
date: 2013-11-01 16:16:09 +10:00
---

My upgrade pain with VS2013 and the Azure SDK 2.2 continues. Hosted build now fails with the following error:

{% highlight text %}
The task factory "CodeTaskFactory" could not be loaded from the assembly "C:\Program Files (x86)\MSBuild\12.0\bin\amd64\Microsoft.Build.Tasks.v4.0.dll". Could not load file or assembly 'file:///C:\Program Files (x86)\MSBuild\12.0\bin\amd64\Microsoft.Build.Tasks.v4.0.dll' or one of its dependencies. The system cannot find the file specified.
{% endhighlight %}

While my Polish is non-existent, the answer can be found at [http://www.benedykt.net/2013/10/10/the-task-factory-codetaskfactory-could-not-be-loaded-from-the-assembly/][0]. The project templates for the web and worker role projects uses ToolsVersion=”12.0”. This needs to be changed to ToolsVersion=”4.0” for hosted build to be successful.

[0]: http://www.benedykt.net/2013/10/10/the-task-factory-codetaskfactory-could-not-be-loaded-from-the-assembly/