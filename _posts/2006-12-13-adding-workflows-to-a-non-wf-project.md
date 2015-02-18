---
title: Adding workflows to a non-WF project
categories : .Net, IT Related
tags : WF
date: 2006-12-13 09:37:42 +10:00
---

I have been building up a project that I need to add workflows to, only I didn't create the project as a workflow project. This means that when I go to add a new item, I get the standard options along with WPF file types and even an option for a WCF service, but no workflow options. 

After using [WinMerge][0] to compare the project file with a workflow project file, these are the actions I took:

1. Add references to System.Workflow.Runtime, System.Workflow.ComponentModel and System.Workflow.Activities.
1. Open the project file in notepad and make the following changes
1. Add the following to the first Project/ProjectGroup element (it should contain the assembly details) :  
  
<ProjectTypeGuids&gt;{14822709-B5A1-4724-98CA-57A101D1B079};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuids&gt;  
  
Given the name of the ProjectTypeGuids element, I am guessing that the guids should be the same for everyone, but you might have to compare the guids found in a new workflow project to you can create.
1. Add the following after the CSharp.targets Import element under the Project element:  
  
<Import Project="$(MSBuildExtensionsPath)\Microsoft\Windows Workflow Foundation\v3.0\Workflow.Targets" /&gt;

[0]: http://winmerge.org/
