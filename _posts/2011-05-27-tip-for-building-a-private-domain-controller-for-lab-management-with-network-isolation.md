---
title: Tip for building a private domain controller for Lab Management with Network Isolation
categories: .Net, IT Related
date: 2011-05-27 13:02:21 +10:00
---

There are obvious benefits by using Lab Management for testing your software. It is a fantastic environment for test teams to test the software written by a development team.   

The requirement I had with my test labs was that I need to use domain controlled security within the test lab as this is what is used in production. I also do not want any impact on the development or production domains. The solution is to use a domain controller (DC) within the lab environment rather than reference the domain hosting the lab environment.  

Having a test DC means that it needs to be isolated from the hosting network. This avoids AD, DNS and DHCP conflicts between the development and test networks. Lab management can be configured for network isolation to get around this problem. This means that the private DC will have a network connection that is private to the lab, while all the other machines in the lab will have one NIC for the private lab network and a second NIC for access out to the hosting environment. This setup can be seen in the SCVMM network diagram below with the machine at the top of the diagram being the private DC.   

<!--more-->

[![][1]][0]

The problem I had for several weeks was that the private DC lost its Windows activation when it was stored into the VMM library for deployment out to a lab environment. You are restricted to phone activation in this case because once the stored VM is put into a lab with network isolation there is no internet support for automatic activation on the DC. This then needs to be done every time you deploy a lab environment.

I followed [the MSDN article][2] steps that describe how to create a private DC for labs but there was nothing specific about how to handle this scenario. The step in question is at the bottom of the article where it says:

> _6\. Shut down the virtual machine, and store it in the SCVMM library._    
    _a. Do not turn off the Active Directory VM. You have to shut it down correctly._    
    _b. Do not generalize the Active Directory VM, either by running Sysprep or by storing the virtual machine as a template in SCVMM._

I followed this step to the letter and stored my DC in the VMM library for use in labs. This was the step that caused the VM to lose its Windows activation. I happened to stumble across the solution to this problem as I had to rebuild the test DC yesterday. The answer is to clone the DC rather than store it.  

![][3]

The Clone wizard provides the option of where to place the clone VM. You want to select _Store the virtual machine in the library_.   

![][4]

The private DC can be deployed out to a lab environment now that it is stored in the library. The Windows activation is retained using this technique so the private DC should be ready for immediate use in the lab.

[0]: /files/image_99.png
[1]: /files/image_thumb_1.png
[2]: http://msdn.microsoft.com/en-us/library/dd380770.aspx
[3]: /files/image_100.png
[4]: /files/image_101.png