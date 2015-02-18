---
title: TFS Build Remote Database Deployment Pain
tags : TFS
date: 2012-11-02 13:43:00 +10:00
---

I've been taking the pain today with an issue that provided a lot of misdirection. The setup is a build agent that uses a remote PowerShell session to install a setup package from the build output. The setup package also includes the facility to remote deploy a database to a data tier machine.

It always worked from my local machine and manually from the deploy machine. I never worked from the build workflow.

The most common failure from executing SqlPackage.exe was a message that said Error SQL72018: Function could not be imported but one or more of these objects exist in your source. Other failures were StackOverflowException and OutOfMemoryException. There is basically no information out there about what could be happening here and SqlPackage provides only the most basic output.

OutOfMemoryException seemed like the most likely issue that may be causing the other issues. The web server (deploy target) was not running out of memory however. That lead to thoughts about memory restrictions for remote PowerShell sessions. It turns out that this is the case. Remote PowerShell sessions only get assigned 150Mb.

The way to get past the failures is to bump up the memory allocation on the deploy target. The PowerShell script you can run (on x64 PowerShell) is:

{% highlight powershell linenos %}
winrm set winrm/config/winrs `@`{MaxMemoryPerShellMB =`"512`"`}

restart-service winrm
{% endhighlight %}

