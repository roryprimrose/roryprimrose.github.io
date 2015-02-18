---
title: Visual Studio Addin - No such interface supported
categories : .Net, My Software
tags : Extensibility
date: 2008-07-08 16:00:54 +10:00
---

I'm working on a new Visual Studio addin that launches a profiling application that in turn runs unit and load tests. I can't recall how I created the project, but it must not have been by using the addin wizard. When I debugged the addin in another instance of Visual Studio, I got an error indicating that the addin couldn't be loaded or threw an exception. The additional information it gave was &quot;No such interface supported&quot;.

After pulling my hair out for quite a while, the answer was that the AssemblyInfo.cs contained the following:

    {% highlight csharp linenos %}
    // Setting ComVisible to false makes the types in this assembly not visible 
    // to COM components.  If you need to access a type in this assembly from 
    // COM, set the ComVisible attribute to true on that type.
    [assembly: ComVisible(false)]
    
    {% endhighlight %}

Visual Studio is written mostly in the COM world, including the functionality that resolves and loads addins. This attribute hides the addin from Visual Studio. It therefore can't find the interface it is expecting when it attempts to load the addin from the configured type information. Removing this attribute from AssemblyInfo.cs (it is included by default) or setting its value to true will allow Visual Studio to correctly load the addin.


