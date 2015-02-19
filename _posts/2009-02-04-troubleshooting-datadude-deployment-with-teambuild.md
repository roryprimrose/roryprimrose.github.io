---
title: Troubleshooting DataDude deployment with TeamBuild
categories : .Net
tags : TeamBuild, TFS
date: 2009-02-04 11:28:10 +10:00
---

I'm working through the best way of getting DataDude to build and deploy a database using TeamBuild. When I kicked off a build, the build script failed with the following error:

> _Task &quot;SqlBuildTask&quot;   
> &#160; Building deployment script for [DatabaseName] : AlwaysCreateNewDatabase, EnableFullTextSearch, BlockIncrementalDeploymentIfDataLoss   
> MSBUILD : Build error TSD158: Cannot open user default database. Login failed.   
> MSBUILD : Build error TSD158: Login failed for user '[TeamBuildUserName]'.   
> Done executing task &quot;SqlBuildTask&quot; -- FAILED._

The problem here is that the logs don't contain the information about the target database or the connection string used to deploy the database. This means that I can't be certain that the properties defined for the project and build script are getting correctly propagated through the build process. 

The solution is to modify the DataDude build script to include a log message that outputs the required property values. 

1. Open the DataDude build target file on the build server (found at _C:\Program Files\MSBuild\Microsoft\VisualStudio\v9.0\TeamData\Microsoft.VisualStudio.TeamSystem.Data.Tasks.targets)_
1. Find the SqlBuild target. It looks like the following
> <Target Name=&quot;SqlBuild&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; DependsOnTargets=&quot;$(SqlBuildDependsOn)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; Inputs=&quot;@(SqlBuildInputItems)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; Outputs=&quot;@(SqlBuildOutputItems)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; &gt;&#160;   
> &#160; <SqlBuildTask   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; SourceItems=&quot;@(Build)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; PostDeployItems=&quot;@(PostDeploy)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; PreDeployItems=&quot;@(PreDeploy)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; TargetConnectionString=&quot;$(TargetConnectionString)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; TargetDatabase=&quot;$(TargetDatabase)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; OutputPath=&quot;$(OutDir)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; BuildScriptName=&quot;$(BuildScriptName)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; ReferenceAssemblyName=&quot;$(ReferenceAssemblyName)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; ProjectPath=&quot;$(MSBuildProjectFullPath)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; References=&quot;@(ReferencePath)&quot;

1. Add a message task to the start of the target so it looks like the following
> <Target Name=&quot;SqlBuild&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; DependsOnTargets=&quot;$(SqlBuildDependsOn)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; Inputs=&quot;@(SqlBuildInputItems)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; Outputs=&quot;@(SqlBuildOutputItems)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; &gt; 

> &#160; **> <Message Text=&quot;Deploying database $(TargetDatabase) with connection $(TargetConnectionString)&quot; /&gt;**  
> &#160; <SqlBuildTask   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; SourceItems=&quot;@(Build)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; PostDeployItems=&quot;@(PostDeploy)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; PreDeployItems=&quot;@(PreDeploy)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; TargetConnectionString=&quot;$(TargetConnectionString)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; TargetDatabase=&quot;$(TargetDatabase)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; OutputPath=&quot;$(OutDir)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; BuildScriptName=&quot;$(BuildScriptName)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; ReferenceAssemblyName=&quot;$(ReferenceAssemblyName)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; ProjectPath=&quot;$(MSBuildProjectFullPath)&quot;   
> &#160;&#160;&#160;&#160;&#160;&#160;&#160; References=&quot;@(ReferencePath)&quot;

1. Kick off another build. The database name and the connection string used to deploy the database will be rendered to the build log.

**WARNING:** If the connection string is using username/password authentication then these credentials will be rendered to the build log. It is advisable that this message task be commented out once the debugging requirements of the build have been satisfied.


