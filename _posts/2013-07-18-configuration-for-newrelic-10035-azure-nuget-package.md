---
title: Configuration for NewRelic 1.0.0.35 Azure Nuget package
categories : .Net
tags : Azure
date: 2013-07-18 08:31:30 +10:00
---

I’ve previously posted about Nuget package update pain with the NewRelic Azure package ([here][0] and [here][1]). I’ve had issues again when updating to the latest version. This time I’ve added in a Worker role and found that the PowerShell script that installs the package did not configure the Worker role. I have gone through the PowerShell scripts to manually ensure that the configuration files match what they should be. 

Here are the ServiceDefinition.csdef configurations for the two types of roles. The setup of this configuration seems to be the most common point of failure in the installation scripts.

**Web Role**

    <WebRole&gt;
      <Startup&gt;
        <Task commandLine=&quot;newrelic.cmd&quot; executionContext=&quot;elevated&quot; taskType=&quot;simple&quot;&gt;
          <Environment&gt;
            <Variable name=&quot;EMULATED&quot;&gt;
              <RoleInstanceValue xpath=&quot;/RoleEnvironment/Deployment/@emulated&quot; /&gt;
            </Variable&gt;
            <Variable name=&quot;IsWorkerRole&quot; value=&quot;false&quot; /&gt;
          </Environment&gt;
        </Task&gt;
      </Startup&gt;
    </WebRole&gt;{% endhighlight %}

**Worker Role**

    <WorkerRole&gt;
      <Startup&gt;
        <Task commandLine=&quot;newrelic.cmd&quot; executionContext=&quot;elevated&quot; taskType=&quot;simple&quot;&gt;
          <Environment&gt;
            <Variable name=&quot;EMULATED&quot;&gt;
              <RoleInstanceValue xpath=&quot;/RoleEnvironment/Deployment/@emulated&quot; /&gt;
            </Variable&gt;
            <Variable name=&quot;IsWorkerRole&quot; value=&quot;true&quot; /&gt;
          </Environment&gt;
        </Task&gt;
      </Startup&gt;
      <Runtime&gt;
        <Environment&gt;
          <Variable name=&quot;COR_ENABLE_PROFILING&quot; value=&quot;1&quot; /&gt;
          <Variable name=&quot;COR_PROFILER&quot; value=&quot;{FF68FEB9-E58A-4B75-A2B8-90CE7D915A26}&quot; /&gt;
          <Variable name=&quot;NEWRELIC_HOME&quot; value=&quot;D:\ProgramData\New Relic\.NET Agent\&quot; /&gt;
        </Environment&gt;
      </Runtime&gt;
    </WorkerRole&gt;{% endhighlight %}

[0]: /post/2013/02/07/Fixing-New-Relic-Nuget-package-for-Azure.aspx
[1]: /post/2013/04/03/NewRelic-Azure-NuGet-package-update-pain-again.aspx
