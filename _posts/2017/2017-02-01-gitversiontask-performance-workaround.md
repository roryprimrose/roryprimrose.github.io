---
title: GitVersionTask performance workaround
categories: .Net
tags: 
date: 2017-02-01
---

I use [GitVersionTask][0] extensively in my projects so that I can get really good automation over my software versioning. I found a performance issue against one repository last year when using GitVersionTask 3.6.0 and greater. The biggest impact this had was on the build agent which provides both validation of the developer commits but also a build for the tester to promote to a test environment for validation. My team was hitting a large bottleneck because of the slow builds.

This performance issue by no means devalues GitVersionTask and we will continue to use it regardless of performance. It also seems like we are an outlier in how GitVersionTask is performing against a specific repository. I have the utmost respect for the people behind GitVersionTask who are frankly a lot smarter than I. This post describes a workaround put in place to address the bottleneck in our build process while the [performance issues][1] are addressed.

<!--more-->

We have a fairly aggressive build trigger that queues new builds for every remote push, pull request and pull request merge. There is something in our repository history that has caused GitVersionTask to take 45 seconds per project on the build server to figure out the version number of the commit. This ballooned our build time from about 10 minutes to a max of 28 minutes with 19 projects in the solution hooked up with GitVersionTask. With many builds in the queue, the tester could be waiting for hours before the build they want is available for testing.

The workaround was to get most of the projects in the build to not run GitVersionTask. Some projects will always require the version metadata because nuget packages are made from those projects. All other projects could avoid this 45 second version calculation by not running GitVersionTask.

The first step was to identify how to avoid GitVersionTask from executing based on some condition. I first defined a variable that controlled whether the GitVersionTask targets file was imported into the project. This was unsuccessful and not nice for upgrading to newer versions of GitVersionTask. The next attempt was to look at what controlled the GitVersionTask tasks and influence that instead. 

GitVersionTask has an MSBuild targets file defines an MSBuild property for each task it executes. These properties define whether the associated task will be executed. The workaround that was successful was to pre-define these three properties based on another property that gave us global control of this process.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
	<PropertyGroup>

		<!-- Temporary GitVersionTask override for performance issues -->
		<ApplyVersioning Condition="'$(ApplyVersioning)' == ''">false</ApplyVersioning>

		<WriteVersionInfoToBuildLog Condition=" '$(ApplyVersioning)' != 'true' ">false</WriteVersionInfoToBuildLog>
		<UpdateAssemblyInfo Condition=" '$(ApplyVersioning)' != 'true' ">false</UpdateAssemblyInfo>
		<GetVersion Condition=" '$(ApplyVersioning)' != 'true' ">false</GetVersion>

	</PropertyGroup>

</Project>
```

The ApplyVersioning property is defaulted to false if there is no prior value. This then controls the GitVersionTask properties. The GitVersionTask targets file then uses the same pattern to predefine these variables if they have not yet been set, which they have been in this case. 

Any project that needs conditional execution of GitVersionTask simply needs to include this targets file above the GitVersionTask targets import in the proj file. This ensures that the properties that GitVersionTask uses are already predefined based on the ApplyVersioning property.

```xml
  <Import Project="..\Solution Items\GitVersionPerf.targets" />

  <Import Project="..\packages\GitVersionTask.4.0.0-beta0009\build\GitVersionTask.targets" Condition="Exists('..\packages\GitVersionTask.4.0.0-beta0009\build\GitVersionTask.targets')" />
  <Target Name="EnsureNuGetPackageBuildImports" BeforeTargets="PrepareForBuild">
    <PropertyGroup>
      <ErrorText>This project references NuGet package(s) that are missing on this computer. Use NuGet Package Restore to download them.  For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {0}.</ErrorText>
    </PropertyGroup>
    <Error Condition="!Exists('..\packages\GitVersionTask.4.0.0-beta0009\build\GitVersionTask.targets')" Text="$([System.String]::Format('$(ErrorText)', '..\packages\GitVersionTask.4.0.0-beta0009\build\GitVersionTask.targets'))" />
  </Target>
```

 There is a circumstance where we want all versioning to work normally on the build agent. This is when we are creating a production release version. We want all binaries to have version metadata in this circumstance. The build definition was updated to include an ApplyVersioning variable that was defaulted to false. Once a production commit is tagged (such as v1.2.3), we can then manually queue that build and set ApplyVersioning to true.

 GitVersionTask is an absolutely fantastic library that I cannot recommend enough. Read about it on [GitHub][0].

[0]: https://github.com/GitTools/GitVersion
[1]: https://github.com/GitTools/GitVersion/issues/1066