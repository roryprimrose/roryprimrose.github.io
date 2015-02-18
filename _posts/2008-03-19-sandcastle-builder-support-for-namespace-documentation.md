---
title: SandCastle Builder Support for Namespace Documentation
categories : .Net, Applications
tags : Sandcastle, TeamBuild
date: 2008-03-19 10:35:32 +10:00
---

At work we have recently integrated building SandCastle documentation into our TeamBuild process using the [SandCastle Help File Builder][0] (SHFB) application. I created a wrapper application to achieve the same thing as a local dev process to help authoring the help contents.

One of the issues we had was how to add namespace documentation. The process we were using was to pass all the required information to SHFB instead of using a project file. The SandCastle project file is where the namespace documentation would normally be stored. Instead, we added the namespace documentation to an xml file stored in the appropriate documented Visual Studio project with a known file name format. The xml file was marked as to be copied to the build directory which would also make it available to team build. This file is then passed to SHFB using the _-comment_ switch.

This may change in the near future as it looks like Eric has checked in a change to the latest SHFB beta that will do the same as the old NDoc model for namespace documentation (see [here][1]). Soon we will be able to create an internal class called NamespaceDoc in a namespace and SHFB will pull it out for us. This allows us to have documentation nicely stored in the code along with everything else.

[0]: http://www.codeplex.com/SHFB
[1]: http://www.codeplex.com/SHFB/WorkItem/View.aspx?WorkItemId=15516
