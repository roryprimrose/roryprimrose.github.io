---
title: NewRelic Azure NuGet package update pain again
categories: .Net
tags: Azure
date: 2013-04-03 14:10:37 +10:00
---

I [previously posted][0] about issues I found when updating to the latest Azure NuGet package published by NewRelic. Unfortunately the install PowerShell script for the latest package now has more issues than the previous version. Here are the issues I found and how to fix them.

<!--more-->

1. newrelic.cmd not updated   
I believe the issue here (unconfirmed) is that the file is not updated because it has been changed as part of the previous package installation. The fix is to uninstall the previous package first and then manually delete the newrelic.cmd file. The uninstall won’t remove the file for the same reason.
1. The license key is not written to newrelic.cmd   
This is a bug in the newrelic.cmd file published with the package. The install script is expect the file to have the line **SET LICENSE_KEY=REPLACE_WITH_LICENSE_KEY** so that the installer can substitute the value. Unfortunately the cmd file in the package has an actual license key rather than the expected placeholder. This means someone else’s license key will be used rather than your own.
1. Same as my previous experience, ServiceDefinition.csdef was not found. You need to manually update this file to contain the following XML (slightly different to the previous version).

```xml
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
```

Note that this XML is for a WebRole only. The WorkerRole XML has additional elements and attributes that you will need to add according to the PowerShell script.

[0]: /2013/02/07/Fixing-New-Relic-Nuget-package-for-Azure/
