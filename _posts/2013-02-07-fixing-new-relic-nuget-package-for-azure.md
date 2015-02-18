---
title: Fixing New Relic Nuget package for Azure
categories : .Net
tags : Azure
date: 2013-02-07 21:55:39 +10:00
---

I tried to install the [New Relic][0] Nuget package for an Azure solution. Unfortunately the Nuget install script failed to update the ServiceDefinition.csdef file and provided the following error. 

> Unable to find the ServiceDefinition.csdef file in your solution, please make sure your solution contains an Azure deployment project and try again.

One of the good things about Nuget is that the install scripts are easily available to the solution.

The install script was trying to add the following xml fragment to the start of the web role definition in ServiceDefinition.csdef file.

    <Startup&gt;
      <Task commandLine=&quot;newrelic.cmd&quot; executionContext=&quot;elevated&quot; taskType=&quot;simple&quot;&gt;
        <Environment&gt;
          <Variable name=&quot;EMULATED&quot;&gt;
            <RoleInstanceValue xpath=&quot;/RoleEnvironment/Deployment/@emulated&quot;/&gt;
          </Variable&gt;
        </Environment&gt;
      </Task&gt; 
    </Startup&gt;{% endhighlight %}

Adding this manually and we are good to go.

[0]: http://www.newrelic.com
