---
title: Code Contracts and side by side Azure 1.6 and 1.7 SDK
categories : .Net, Applications
tags : Azure
date: 2012-06-08 22:42:05 +10:00
---



I have installed the latest SDK that came out this morning and I hit a compilation hurdle straight away.  

My solution is starting to get to be a decent size with many third party technologies involved, Code Contracts being just one of them. My website project is failing to compile with an issue raised by the code contracts rewriter.  

![image][0]

This appears to be a bit concerning because the project itself compiled fine, but the rewriter failed at the tail end of the project compilation. The output window does shed some light on the issue though. 

{% highlight text %}
Reading assembly 'Microsoft.WindowsAzure.ServiceRuntime' from 'C:\Program Files\Windows Azure SDK\v1.6\ref\Microsoft.WindowsAzure.ServiceRuntime.dll' resulted in errors.
    Assembly reference not resolved: msshrtmi, Version=1.6.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35.
    Could not resolve type reference: [msshrtmi]Microsoft.WindowsAzure.ServiceRuntime.Internal.InteropRoleUpdates.
    Could not resolve type reference: [msshrtmi]Microsoft.WindowsAzure.ServiceRuntime.Internal.RoleStatus.
{% endhighlight %}

While there is side by side support for 1.6 and 1.7 SDK versions, this is clearly referencing the old SDK when I want it to reference the latest bits. The 1.7 SDK readme also provides some further insight.

> _To avoid confusion when you install both versions, the public versions of the .NET assemblies have been changed. Assemblies from November 2011 are typically marked with version 1.0 or 1.1. The June 2012 release assemblies are labeled with version 1.7. This change will require you to recompile applications or to use assembly binding redirects to pick up the new June 2012 assemblies. For more information, see [Assembly Binding Redirection][1]._

The part that got my attention was “binding redirects”. Ok, so something is forcing the project to go to the 1.6 SDK. The most likely cause is the specific version property on the reference in the project.

![image][2]

Setting the Specific Version property to False then automatically picks up the 1.7 SDK.

![image][3]

I don’t know why the Code Contracts rewriter doesn’t like the side by side installation, but the project now compiles successfully.

[0]: /files/image_141.png
[1]: http://msdn.microsoft.com/en-us/library/2fc472t2(v=vs.90).aspx
[2]: /files/image_142.png
[3]: /files/image_143.png
