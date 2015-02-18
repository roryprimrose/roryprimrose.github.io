---
title: Tip for building a private domain controller for Lab Management with Network Isolation
categories : .Net, IT Related
date: 2011-05-27 13:02:21 +10:00
---

<p>There are obvious benefits by using Lab Management for testing your software. It is a fantastic environment for test teams to test the software written by a development team. </p>  <p>The requirement I had with my test labs was that I need to use domain controlled security within the test lab as this is what is used in production. I also do not want any impact on the development or production domains. The solution is to use a domain controller (DC) within the lab environment rather than reference the domain hosting the lab environment.</p>  <p>Having a test DC means that it needs to be isolated from the hosting network. This avoids AD, DNS and DHCP conflicts between the development and test networks. Lab management can be configured for network isolation to get around this problem. This means that the private DC will have a network connection that is private to the lab, while all the other machines in the lab will have one NIC for the private lab network and a second NIC for access out to the hosting environment. This setup can be seen in the SCVMM network diagram below with the machine at the top of the diagram being the private DC. </p>  <p><a href="//blogfiles/image_99.png"><img style="background-image: none; border-right-width: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_thumb_1.png" width="416" height="772" /></a></p>  <p>The problem I had for several weeks was that the private DC lost its Windows activation when it was stored into the VMM library for deployment out to a lab environment. You are restricted to phone activation in this case because once the stored VM is put into a lab with network isolation there is no internet support for automatic activation on the DC. This then needs to be done every time you deploy a lab environment.&#160;&#160; </p>  <p>I followed <a href="http://msdn.microsoft.com/en-us/library/dd380770.aspx" target="_blank">the MSDN article</a> steps that describe how to create a private DC for labs but there was nothing specific about how to handle this scenario. The step in question is at the bottom of the article where it says:</p>  <blockquote>   <p><em>6. Shut down the virtual machine, and store it in the SCVMM library.</em></p>    <p><em>&#160;&#160;&#160; a. Do not turn off the Active Directory VM. You have to shut it down correctly.</em></p>    <p><em>&#160;&#160;&#160; b. Do not generalize the Active Directory VM, either by running Sysprep or by storing the virtual machine as a template in SCVMM.</em></p> </blockquote>  <p>I followed this step to the letter and stored my DC in the VMM library for use in labs. This was the step that caused the VM to lose its Windows activation. I happened to stumble across the solution to this problem as I had to rebuild the test DC yesterday. The answer is to clone the DC rather than store it.</p>  <p><img style="background-image: none; border-right-width: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_100.png" width="417" height="517" /></p>  <p>The Clone wizard provides the option of where to place the clone VM. You want to select <em>Store the virtual machine in the library</em>. </p>  <p><img style="background-image: none; border-right-width: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_101.png" width="822" height="698" /></p>  <p>The private DC can be deployed out to a lab environment now that it is stored in the library. The Windows activation is retained using this technique so the private DC should be ready for immediate use in the lab.</p>