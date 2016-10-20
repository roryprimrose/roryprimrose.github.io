---
title: Fixing New Relic Nuget package for Azure
categories: .Net
tags: Azure
date: 2013-02-07 21:55:39 +10:00
---

I tried to install the [New Relic][0] Nuget package for an Azure solution. Unfortunately the Nuget install script failed to update the ServiceDefinition.csdef file and provided the following error. 

> Unable to find the ServiceDefinition.csdef file in your solution, please make sure your solution contains an Azure deployment project and try again.

One of the good things about Nuget is that the install scripts are easily available to the solution.

<!--more-->

The install script was trying to add the following xml fragment to the start of the web role definition in ServiceDefinition.csdef file.

```xml
<Startup>
    <Task commandLine="newrelic.cmd" executionContext="elevated" taskType="simple">
    <Environment>
        <Variable name="EMULATED">
        <RoleInstanceValue xpath="/RoleEnvironment/Deployment/@emulated"/>
        </Variable>
    </Environment>
    </Task> 
</Startup>
```

Adding this manually and we are good to go.

[0]: http://www.newrelic.com
