---
title: Psexec hangs in TeamBuild script when invoking msiexec on remote machine
categories : .Net
tags : TeamBuild
date: 2009-04-15 09:15:46 +10:00
---

I've come across this one a few times and I can never remember what the answer is off the top of my head. 

We are getting the msbuild script to execute psexec to invoke msiexec on a host machine to install an msi compiled by the build script. This is done so that the build script can then execute integration and load tests on the deployed application. 

In modifying the build script, it seems like I have accidentally changed the psexec arguments which has caused psexec to hang on the build agent. In this case, it is happening when -accepteula is not passed to psexec.

The build script should look like this for executing psexec -&gt; msiexec.

<!--more-->

{% highlight csharp %}
<BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
            BuildUri="$(BuildUri)"
            Id="$(DeployMsiBuildStepId)"
            Message="Uninstalling previous version from $(DeploymentServer)" />
     
<!-- Uninstall the existing MSI -->
<Exec Command="PsExec.exe -accepteula -s -i \\$(DeploymentServer) $(DeploymentServerCredentials) msiexec /x &quot;$(ProductCode)&quot; /quiet /lv+ &quot;$(DeploymentServerFolderLocal)\Uninstall$(DeploymentFileLog)&quot;"
        IgnoreExitCode="true"
        ContinueOnError="true"
        Timeout="300000" />
     
<BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
            BuildUri="$(BuildUri)"
            Id="$(DeployMsiBuildStepId)"
            Message="Installing new version $(DeploymentServer)" />
     
<!-- Install the MSI -->
<Exec Command="PsExec.exe -accepteula -s -i \\$(DeploymentServer) $(DeploymentServerCredentials) msiexec /i &quot;$(DeploymentServerFolderLocal)\$(DeploymentFile)&quot; TRANSFORMS=&quot;$(DeploymentServerFolderLocal)\$(DeploymentTransformFile)&quot; /quiet /lv+ &quot;$(DeploymentServerFolderLocal)\Install$(DeploymentFileLog)&quot;"
        IgnoreExitCode="false"
        ContinueOnError="false"
        Timeout="300000" />
     
<BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
            BuildUri="$(BuildUri)"
            Id="$(DeployMsiBuildStepId)"
            Status="Succeeded" />    
{% endhighlight %}


