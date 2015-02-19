---
title: Executing build tasks without a build server – Example scenario
categories : .Net
tags : Extensibility, TFS, WiX
date: 2011-07-07 00:14:06 +10:00
---

My last few posts have described the process of building an application that can manage custom build tasks without the availability of a full TFS environment ([here][0], [here][1] and [here][2]). This post will look at how I use this application and all its tasks to manage the product version of a Visual Studio solution. The product I originally wrote BuildTaskExecutor (BTE) for is the .Net port of my old VB6 Switch program which is now hosted out on Codeplex ([here][3]).  

The build tasks I want to execute when this solution compiles are:  

* Increment the build number of the product version
* Check out from TFS as required
* Sync updated product version into wix project
* Check out from TFS as required
* Rename wix output to include the product version

The solution for Switch has several projects. These are:

* Neovolve.Switch.Extensibility
* Neovolve.Switch.Skinning
* Neovolve.Switch
* Neovolve.Switch.Deployment
* Unit test projects

![image][4]

All the code projects link in a ProductInfo.cs file so that they all compile with the same product information.  

![image][5]

The important configuration in this file with regard to versioning the product is the AssemblyVersionAttribute. Each project that links in this file will compile a binary with the same version information.  

**Increment the build number of the product version**  

The first task for the custom build actions is to increment the build number of the product before it compiles. This is done by invoking BTE in the pre-build event of the project that will compile first. You can look at the build order of the solution to easily figure out which project this will be.  

![image][6]

The pre-build event of this project can then invoke BTE to increment the build number. BTE will attempt to check out the file from TFS before incrementing the build number as this action will make a change to the file.   

The pre-build event for the Neovolve.Switch.Extensibility project has been changed to invoke the BTE TfsEdit task followed by the IncrementAssemblyVersion task.  

{% highlight text linenos %}
If Not '$(ConfigurationName)' == 'Release' GOTO End

CD "$(SolutionDir)"
CALL bte tfsedit /pattern:"$(SolutionDir)Solution Items\ProductInfo.cs" /i
CALL bte iav /pattern:"$(SolutionDir)Solution Items\ProductInfo.cs" /b

GOTO End

:End
{% endhighlight %}

This script will only execute BTE when the Release build is selected. I didn’t want to increment the build number for Debug build configurations. When it is in a release build, the TfsEdit task has been configured here to ignore any failures to cater for scenarios where someone is building the solution without a TFS workspace mapping for the solution. The script then increments just the build number in the specified file.

**Sync updated product version into wix project**

The next step is to get the generation of the Wix project to use the same version information (which has now been incremented). This is unfortunately not as simple as linking in a common ProductInfo.cs file as it is with the C# projects. Wix projects define the version of the MSI in a version attribute on the Product element. This may also use a wix variable that may be declared in any file in the Wix project.

This is the scenario for the Neovolve.Switch.Deployment project. The beginning of the Product.wxs file defines the product version using a wix variable that is defined in an include file.

{% highlight xml linenos %}
<?xml version="1.0"
      encoding="UTF-8" ?>
<?include Definitions.wxi ?>

<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi"
     xmlns:netfx="http://schemas.microsoft.com/wix/NetFxExtension">
  <Product Id="*"
           Name="$(var.PRODUCTNAME)"
           Language="1033"
           Version="$(var.ProductVersion)"
           Manufacturer="$(var.COMPANYNAME)"
           UpgradeCode="0FD2155F-5676-448F-864A-482BE0C26E97">
{% endhighlight %}

The Neovolve.Switch.Deployment project will also define a pre-build event script like the one defined above for the Neovolve.Switch.Extensibility project.

{% highlight text linenos %}
CD "$(SolutionDir)"
CALL bte tfsedit /pattern:"$(ProjectDir)Definitions.wxi" /i
CALL bte swv /pattern:"$(ProjectPath)" /source:"$(SolutionDir)Neovolve.Switch\$(OutDir)Switch.exe" /M /m /b /r
{% endhighlight %}

The BTE TfsEdit task is used again here to attempt to check out the Definitions.wxi file from TFS as it is this file that contains the product version in a project variable. The BTE SyncWixVersion task then obtains the product version from the compiled Switch application and synchronises it into the Wix project before the MSI is compiled.

One important thing to note here is that the source of the version number is the compiled output of the Switch application (in the current solution build) rather than reading the version from a source file (ProductInfo.cs). This is important because the AssemblyVersionAttribute in ProductInfo.cs may only define 1.0.*, or 1.0.1.*. The full version information for wildcard version numbers is only available after the compiler has generated the application. In this case, the SyncWixVersion (or swv) task is taking all version parts&#160; (/M = major, /m = minor, /b = build, /r = revision) from Switch.exe and pushing them into the wix product version value.

**Rename wix output to include the product version**

Lastly, I want the output of the Wix project to include the version number. This task could update the output file name in the project property prior to compiling the MSI. This would however be messy for two reasons:

1. Changing the project during compilation will cause Visual Studio to want to reload the project. This is not really a problem but is a bad user experience as reload project dialogs will get in the way.
1. The next time the project compiles, the task will have to deal with having a prior version number in the output name that it will need to parse out and replace. Easily achievable with a regular expression, but messy.

The simpler solution is to just rename the output of the project once compilation has completed.

A post-build event script is added to the Neovolve.Switch.Deployment project in order to resolve the output of the Wix project and rename it.

{% highlight text linenos %}
CD "$(SolutionDir)"
CALL bte wov /pattern:"$(ProjectPath)"
{% endhighlight %}

BTE invokes the WixOutputVersion task against the wix project. It will look at all the build configuration for the project and rename as many files as possible that match the output name of the project. It will use the product version information that is defined against the Wix project that was synchronised in the pre-build event script.

**The result**

When this solution is compiled, the following is seen in the build log (severely cut down for brevity).

{% highlight text linenos %}
------ Rebuild All started: Project: Neovolve.Switch.Extensibility, Configuration: Release Any CPU ------
  BuildTaskExecutor - Neovolve Build Task Executor 1.0.0.29135
  
  Executing task 'TfsCheckout'.
  Using 'C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\tf.exe' to check out files.
  Searching for matching files under 'D:\Codeplex\Neovolve.Switch\Solution Items\'.
  Checking out file 'D:\Codeplex\Neovolve.Switch\Solution Items\ProductInfo.cs'.
  Solution Items:
  ProductInfo.cs
  
  BuildTaskExecutor - Neovolve Build Task Executor 1.0.0.29135
  
  Executing task 'IncrementAssemblyVersion'.
  Searching for matching files under 'D:\Codeplex\Neovolve.Switch\Solution Items\'.
  Updating file 'D:\Codeplex\Neovolve.Switch\Solution Items\ProductInfo.cs' from version '2.0.2.0' to '2.0.3.0'.
  elapsed time: 134.0076ms
  elapsed time: 239.0136ms
  
  Microsoft (R) .NET Framework Strong Name Utility  Version 3.5.30729.1
  Copyright (c) Microsoft Corporation.  All rights reserved.
  
  Assembly 'obj\Release\Neovolve.Switch.Extensibility.dll' successfully re-signed
  Neovolve.Switch.Extensibility -> D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Extensibility\bin\Release\Neovolve.Switch.Extensibility.dll
------ Rebuild All started: Project: Neovolve.Switch.Skinning, Configuration: Release Any CPU ------

  Microsoft (R) .NET Framework Strong Name Utility  Version 3.5.30729.1
  Copyright (c) Microsoft Corporation.  All rights reserved.
  
  Assembly 'obj\Release\Neovolve.Switch.Skinning.dll' successfully re-signed
  Neovolve.Switch.Skinning -> D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Skinning\bin\Release\Neovolve.Switch.Skinning.dll
------ Rebuild All started: Project: Neovolve.Switch.Extensibility.UnitTests, Configuration: Release Any CPU ------
  
  Microsoft (R) .NET Framework Strong Name Utility  Version 3.5.30729.1
  Copyright (c) Microsoft Corporation.  All rights reserved.
  
  Assembly 'obj\Release\Neovolve.Switch.Extensibility.UnitTests.dll' successfully re-signed
  Neovolve.Switch.Extensibility.UnitTests -> D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Extensibility.UnitTests\bin\Release\Neovolve.Switch.Extensibility.UnitTests.dll
------ Rebuild All started: Project: Neovolve.Switch, Configuration: Release x86 ------
  
  Microsoft (R) .NET Framework Strong Name Utility  Version 3.5.30729.1
  Copyright (c) Microsoft Corporation.  All rights reserved.
  
  Assembly 'obj\x86\Release\Switch.exe' successfully re-signed
  Neovolve.Switch -> D:\Codeplex\Neovolve.Switch\Neovolve.Switch\bin\Release\Switch.exe
------ Rebuild All started: Project: Neovolve.Switch.UnitTests, Configuration: Release Any CPU ------
  Neovolve.Switch.UnitTests -> D:\Codeplex\Neovolve.Switch\Neovolve.Switch.UnitTests\bin\Release\Neovolve.Switch.UnitTests.dll
------ Rebuild All started: Project: Neovolve.Switch.Deployment, Configuration: Release x86 ------
        CD "D:\Codeplex\Neovolve.Switch\"
CALL bte tfsedit /pattern:"D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\Definitions.wxi" /i
CALL bte swv /pattern:"D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\Neovolve.Switch.Deployment.wixproj" /source:"D:\Codeplex\Neovolve.Switch\Neovolve.Switch\bin\Release\Switch.exe" /M /m /b /r
        BuildTaskExecutor - Neovolve Build Task Executor 1.0.0.29135
        Executing task 'TfsCheckout'.
        Using 'C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\tf.exe' to check out files.
        Searching for matching files under 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\'.
        Checking out file 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\Definitions.wxi'.
        Neovolve.Switch.Deployment:
        Definitions.wxi
        BuildTaskExecutor - Neovolve Build Task Executor 1.0.0.29135
        Executing task 'SyncWixVersion'.
        Searching for matching files under 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\'.
        Updating wix project 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\Neovolve.Switch.Deployment.wixproj' from version '2.0.2.0' to '2.0.3.0'.

        CD "D:\Codeplex\Neovolve.Switch\"
CALL bte wov /pattern:"D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\Neovolve.Switch.Deployment.wixproj"
        BuildTaskExecutor - Neovolve Build Task Executor 1.0.0.29135
        Executing task 'WixOutputVersion'.
        Searching for matching files under 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\'.
        Moving 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\bin\Release\Neovolve Switch.msi' to 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\bin\Release\Neovolve Switch 2.0.3.0.msi'.
        Moving 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\bin\Release\Neovolve Switch.wixpdb' to 'D:\Codeplex\Neovolve.Switch\Neovolve.Switch.Deployment\bin\Release\Neovolve Switch 2.0.3.0.wixpdb'.
========== Rebuild All: 6 succeeded, 0 failed, 0 skipped ==========

Build Summary
-------------
00:13.492 - Success - Release Any CPU - Neovolve.Switch.Extensibility\Neovolve.Switch.Extensibility.csproj
00:12.825 - Success - Release x86 - Neovolve.Switch\Neovolve.Switch.csproj
00:12.793 - Success - Release x86 - Neovolve.Switch.Deployment\Neovolve.Switch.Deployment.wixproj
00:07.458 - Success - Release Any CPU - Neovolve.Switch.Skinning\Neovolve.Switch.Skinning.csproj
00:07.067 - Success - Release Any CPU - Neovolve.Switch.Extensibility.UnitTests\Neovolve.Switch.Extensibility.UnitTests.csproj
00:01.879 - Success - Release Any CPU - Neovolve.Switch.UnitTests\Neovolve.Switch.UnitTests.csproj

Total build time: 00:55.555

========== Rebuild All: 6 succeeded or up-to-date, 0 failed, 0 skipped ==========
{% endhighlight %}

You can see from the logs that BTE has done the following throughout the build of the solution:

* Checked out ProductInfo.cs from TFS
* Identified the existing version as 2.0.2.0
* Incremented version to 2.0.3.0
* Synchronised the Wix project to 2.0.3.0
* Moved the Wix project output to the same filename with the addition of the version number.

The solution now contains two files that are checked out. These are ProductInfo.cs and Definitions.wxi.
      
![image][7]

The target directory of the Neovolve.Switch.Deployment project now contains nicely renamed output.
      
![image][8]

Running all the BTE executions in the project build events is a batch file called bte.bat. This is located in the solution root and makes it easy for any project in the solution to invoke BTE with minimal noise in the build event script window. The batch file simply invokes BTE with all the command line parameters and manages the return code.

{% highlight text linenos %}
@ECHO OFF
References\Neovolve\BuildTaskExecutor\BuildTaskExecutor.exe %*

If errorlevel 1 GOTO BuildTaskFailed
GOTO BuildTaskSuccessful

:BuildTaskFailed
exit 1
:BuildTaskSuccessful
{% endhighlight %}

You have seen here how a flexible little tool like BTE can integrate into a solution build to achieve valuable results when you don’t have access to a TFS build infrastructure.
      
The next post will provide an example of how to create a custom task for BTE to pick up and execute.

[0]: /2011/07/01/Executing-build-tasks-without-a-build-server-%E2%80%93-Design/
[1]: /2011/07/03/Executing-build-tasks-without-a-build-server-%E2%80%93-Implementation/
[2]: /2011/07/06/Executing-build-tasks-without-a-build-server-%E2%80%93-In-action/
[3]: http://switch.codeplex.com/
[4]: /files/image_110.png
[5]: /files/image_111.png
[6]: /files/image_112.png
[7]: /files/image_113.png
[8]: /files/image_114.png
