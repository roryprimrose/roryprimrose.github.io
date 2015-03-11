---
title: Creating a simple bootstrapper - Proof of concept
categories : .Net, Applications
tags : WiX
date: 2011-07-19 17:00:59 +10:00
---

The [previous post][0] discussed the pain points of putting validation into the UI sequence of an MSI package that require elevation. The outcome of some research was that a bootstrapper was the best way to get around this.

To recap, my design requirements for a bootstrapper solution were:

* Seamless Visual Studio integration and compilation
* Bootstrapper must be completely self-contained
* Bootstrapper must be quiet (no UI) and simply execute the MSI package
* Automatically elevate the packaged MSI
* Support change/repair/remove actions on the package
* Flexible design to add/remove bootstrapper functionality

<!--more-->

I figured that I could achieve this by writing a simple project in C#. 

**Seamless Visual Studio integration and compilation**

The bootstrapper project would reference the output of a WiX project in the same solution and add its MSI as a resource. Using a project in this way had the following issues:

* Adding the output of the WiX project to the bootstrapper resource designer caused the package to be copied to the bootstrapper project 
  * Manually editing the resource xml could then point a relative path back to the WiX output directory
  * Resource xml must look at a specific path, so bin\Release or bin\Debug for different build configurations would not be supported
* Project dependencies needed to ensure that the bootstrapper project was compiled after the WiX project

At this point, the bootstrapper project would seamlessly participate in the Visual Studio solution build. 

**Bootstrapper must be completely self-contained**

The Bootstrapper executable now contains the MSI as an internal resource. This leaves the bootstrapper output to be completely self-contained. A user can then copy the bootstrapper application around and still have the MSI package. 

**Bootstrapper must be quiet (no****UI****) and simply execute the MSI package**

I added some code to the bootstrapper Main method to extract the resource out to a specific location on the file system and then execute the MSI using Process.Start. My original attempt had the bootstrapper project as a Console project. This unfortunately threw up an empty console window during the execution of the MSI package. As the console UI was not required, I switched the application type over to be a Winforms executable that simply did not display a form.

**Automatically elevate the packaged MSI**

This is actually really easy to achieve in a Visual Studio project. You can add an application manifest to the project and set the requestedExecutionLevel to requireAdministrator.

{% highlight xml %}
<?xml version="1.0" encoding="utf-8"?>
<asmv1:assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1" xmlns:asmv1="urn:schemas-microsoft-com:asm.v1" xmlns:asmv2="urn:schemas-microsoft-com:asm.v2" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <assemblyIdentity version="1.0.0.0" name="MyApplication.app"/>
    <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
        <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
        <!-- UAC Manifest Options
            If you want to change the Windows User Account Control level replace the 
            requestedExecutionLevel node with one of the following.
    
        <requestedExecutionLevel  level="asInvoker" uiAccess="false" />
        <requestedExecutionLevel  level="requireAdministrator" uiAccess="false" />
        <requestedExecutionLevel  level="highestAvailable" uiAccess="false" />
    
            Specifying requestedExecutionLevel node will disable file and registry virtualization.
            If you want to utilize File and Registry Virtualization for backward 
            compatibility then delete the requestedExecutionLevel node.
        -->
        <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
        </requestedPrivileges>
    </security>
    </trustInfo>  
</asmv1:assembly>
{% endhighlight %}

The app.manifest file will then cause Windows to automatically elevate the application when it is started. At this point, all other downstream applications (the MSI execution) will also be elevated. The app.manifest is compiled into the assembly which still leaves the bootstrapper being completely self-contained.

**Support change/repair/remove actions on the package**

From my research on bootstrappers, there are circumstances that require msiexec to have access to the original MSI media for doing some change/repair/remove functions. If the original media is not available then the user will get a file dialog asking them to select the location of the MSI. The simple answer to this was to get the bootstrapper to extract the MSI to a location that will remain available to msiexec after the package has completed installation.

For this purpose I chose the extraction location as _%CommonProgramFiles%\CompanyName\BootstrapperName BootstrapperVersion.msi_ where CompanyName and BootstrapperVersion are extracted from the FileVersionInfo of the bootstrapper application itself.

**Flexible design to add/remove bootstrapper functionality**

Being a C# project, the bootstrapper could be changed at any point to add or remove functionality. For example, a UI could be added to the bootstrapper or support for custom actions based on command line parameters.

**Outcome**

In its basic form, this proof of concept project was a success. The bootstrapper was able to compile within a Visual Studio solution and build a portable bootstrapper package that satisfied all the design requirements.

There were some limitations to the project as it was. It was not easily portable or repeatable between Visual Studio solutions. It also was not flexible enough to support multiple build configurations. The next post will look at how these obstacles were overcome by using a VSIX project template.

[0]: /2011/07/19/creating-a-simple-bootstrapper-introduction/