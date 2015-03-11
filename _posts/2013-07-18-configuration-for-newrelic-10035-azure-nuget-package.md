---
title: Configuration for NewRelic 1.0.0.35 Azure Nuget package
categories : .Net
tags : Azure
date: 2013-07-18 08:31:30 +10:00
---

I’ve previously posted about Nuget package update pain with the NewRelic Azure package ([here][0] and [here][1]). I’ve had issues again when updating to the latest version. This time I’ve added in a Worker role and found that the PowerShell script that installs the package did not configure the Worker role. I have gone through the PowerShell scripts to manually ensure that the configuration files match what they should be. 

<!--more-->

Here are the ServiceDefinition.csdef configurations for the two types of roles. The setup of this configuration seems to be the most common point of failure in the installation scripts.

**Web Role**

{% highlight xml %}
<WebRole>
    <Startup>
    <Task commandLine="newrelic.cmd" executionContext="elevated" taskType="simple">
        <Environment>
        <Variable name="EMULATED">
            <RoleInstanceValue xpath="/RoleEnvironment/Deployment/@emulated" />
        </Variable>
        <Variable name="IsWorkerRole" value="false" />
        </Environment>
    </Task>
    </Startup>
</WebRole>
{% endhighlight %}

**Worker Role**

{% highlight xml %}
<WorkerRole>
    <Startup>
    <Task commandLine="newrelic.cmd" executionContext="elevated" taskType="simple">
        <Environment>
        <Variable name="EMULATED">
            <RoleInstanceValue xpath="/RoleEnvironment/Deployment/@emulated" />
        </Variable>
        <Variable name="IsWorkerRole" value="true" />
        </Environment>
    </Task>
    </Startup>
    <Runtime>
    <Environment>
        <Variable name="COR_ENABLE_PROFILING" value="1" />
        <Variable name="COR_PROFILER" value="{FF68FEB9-E58A-4B75-A2B8-90CE7D915A26}" />
        <Variable name="NEWRELIC_HOME" value="D:\ProgramData\New Relic\.NET Agent\" />
    </Environment>
    </Runtime>
</WorkerRole>
{% endhighlight %}

[0]: /2013/02/07/fixing-new-relic-nuget-package-for-azure/
[1]: /2013/04/03/newrelic-azure-nuget-package-update-pain-again/
