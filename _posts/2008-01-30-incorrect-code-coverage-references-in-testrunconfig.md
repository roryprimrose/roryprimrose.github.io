---
title: Incorrect code coverage references in testrunconfig
categories : .Net
date: 2008-01-30 09:59:07 +10:00
---

I have been having a problem with testrunconfig files recently. 

Lets say I have developed a project with which I also have a unit test project in the solution. I have done this using the Debug build configuration. I set up the code coverage and everything is fine. I then make some changes to the code and also happen to change the build configuration. If I rerun the unit tests, Visual Studio starts freaking out. 

When you select an assembly for code coverage, a relative path to that assembly is stored in the testrunconfig file. The issue is that the relative is specific to a build configuration. In this case, the testrunconfig points code coverage to the assemblies in bin\Debug rather than the path related to my current build configuration. But I have changed the code so that the assemblies in bin\Debug are out of date and don't match the code being executed by the unit test under the current build configuration.

After talking to Microsoft about it, their workaround was to replace bin\Debug with the %OutDir% macro in the relative paths in the testrunconfig file. This macro gets evaluated and the correct build configuration path gets added into the path when the assembly path is resolved. This is not supported in the IDE so you will have to open the testrunconfig using the xml editor and modify the relative path by hand.


