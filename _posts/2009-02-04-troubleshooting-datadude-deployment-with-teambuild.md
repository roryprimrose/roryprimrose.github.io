---
title: Troubleshooting DataDude deployment with TeamBuild
categories : .Net
tags : TeamBuild, TFS
date: 2009-02-04 11:28:10 +10:00
---

I'm working through the best way of getting DataDude to build and deploy a database using TeamBuild. When I kicked off a build, the build script failed with the following error:

{% highlight text linenos %}
Task "SqlBuildTask"   
  Building deployment script for [DatabaseName] : AlwaysCreateNewDatabase, EnableFullTextSearch, BlockIncrementalDeploymentIfDataLoss   
MSBUILD : Build error TSD158: Cannot open user default database. Login failed.   
MSBUILD : Build error TSD158: Login failed for user '[TeamBuildUserName]'.   
Done executing task "SqlBuildTask" -- FAILED.
{% endhighlight %}

The problem here is that the logs don't contain the information about the target database or the connection string used to deploy the database. This means that I can't be certain that the properties defined for the project and build script are getting correctly propagated through the build process. 

The solution is to modify the DataDude build script to include a log message that outputs the required property values. 

Open the DataDude build target file on the build server (found at _C:\Program Files\MSBuild\Microsoft\VisualStudio\v9.0\TeamData\Microsoft.VisualStudio.TeamSystem.Data.Tasks.targets)_

Find the SqlBuild target. It looks like the following

{% highlight text linenos %}
> <Target Name="SqlBuild"   
>         DependsOnTargets="$(SqlBuildDependsOn)"   
>         Inputs="@(SqlBuildInputItems)"   
>         Outputs="@(SqlBuildOutputItems)"   
>         &gt;    
>   <SqlBuildTask   
>         SourceItems="@(Build)"   
>         PostDeployItems="@(PostDeploy)"   
>         PreDeployItems="@(PreDeploy)"   
>         TargetConnectionString="$(TargetConnectionString)"   
>         TargetDatabase="$(TargetDatabase)"   
>         OutputPath="$(OutDir)"   
>         BuildScriptName="$(BuildScriptName)"   
>         ReferenceAssemblyName="$(ReferenceAssemblyName)"   
>         ProjectPath="$(MSBuildProjectFullPath)"   
>         References="@(ReferencePath)"
{% endhighlight %}

Add a message task to the start of the target so it looks like the following
    
{% highlight text linenos %}
> <Target Name="SqlBuild"   
>         DependsOnTargets="$(SqlBuildDependsOn)"   
>         Inputs="@(SqlBuildInputItems)"   
>         Outputs="@(SqlBuildOutputItems)"   
>         >

>   **> <Message Text="Deploying database $(TargetDatabase) with connection $(TargetConnectionString)" />**  
>   <SqlBuildTask   
>         SourceItems="@(Build)"   
>         PostDeployItems="@(PostDeploy)"   
>         PreDeployItems="@(PreDeploy)"   
>         TargetConnectionString="$(TargetConnectionString)"   
>         TargetDatabase="$(TargetDatabase)"   
>         OutputPath="$(OutDir)"   
>         BuildScriptName="$(BuildScriptName)"   
>         ReferenceAssemblyName="$(ReferenceAssemblyName)"   
>         ProjectPath="$(MSBuildProjectFullPath)"   
>         References="@(ReferencePath)"
{% endhighlight %}

1. Kick off another build. The database name and the connection string used to deploy the database will be rendered to the build log.

**WARNING:** If the connection string is using username/password authentication then these credentials will be rendered to the build log. It is advisable that this message task be commented out once the debugging requirements of the build have been satisfied.