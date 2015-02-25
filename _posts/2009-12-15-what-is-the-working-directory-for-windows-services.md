---
title: What is the working directory for Windows services?
categories : .Net
date: 2009-12-15 17:03:00 +10:00
---

I’ve been developing a windows NT service for the last week and I hit an interesting issue today. The configuration for the service contains a relative path which is used in the construction of a class. When this constructor is called, the code checks if the directory exists and creates it if it doesn’t.

The service in question logged errors indicating that there were insufficient privileges for creating the directory. This told me two things. Firstly the directory didn’t exist and secondly there is a permissions problem. I verified that the directory was created by the installer so that means that there is an issue with relative paths.

I quickly realised that the current working directory of a Windows service is probably not the path of the service assembly. Most likely the current directory for a Windows service is C:\Windows\System32. This meant that any work with relative paths was likely to cause grief.

The simple fix for my service is to set the current directory for the process to the directory containing the executing assembly when the service is started.

{% highlight csharp %}
using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
    
internal static class Program
{
    private static void Main()
    {
    	Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    
    	MyService service = new MyService();
    
    	ServiceBase.Run(service);
    }
}
{% endhighlight %}

Now any configured relative path will be relative to the service assembly. Problem solved.


