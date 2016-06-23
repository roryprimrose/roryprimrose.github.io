---
title: TeamBuild: Custom build steps
categories : .Net
tags : Sandcastle, TeamBuild
date: 2009-04-08 16:30:00 +10:00
---

I've been working with team build a fair bit over the last month. Yesterday I was bouncing through the MSDN documentation during my MSBuild travels and I came across a very handy build task. The BuildStep task allows you to create custom entries in the build report.

Take the following build target for example:

<!--more-->

{% highlight csharp %}
<Target Name="RunIntegrationTests">

  <BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
             BuildUri="$(BuildUri)"
             Name="Running integration tests"
             Message="Updating integration test configuration">
    <Output TaskParameter="Id"
            PropertyName="IntegrationTestsBuildStepId" />
  </BuildStep>

  <CallTarget Targets="UpdateIntegrationTestConfiguration" />

  <BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
             BuildUri="$(BuildUri)"
             Id="$(IntegrationTestsBuildStepId)"
             Message="Executing integration tests" />

  <CallTarget Targets="ExecuteIntegrationTests" />

  <BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
             BuildUri="$(BuildUri)"
             Id="$(IntegrationTestsBuildStepId)"
             Status="Succeeded" />

</Target>
{% endhighlight %}

The BuildStep tasks have allowed the build report to include custom steps that are being executed. In this case, it is reporting on the progress of running integration tests.

[![image][1]][0]

You can also see that this report contains a custom build step for working with Sandcastle documentation. 

This is a very easy to use task that adds rich information to your build reports. It is especially useful for when custom targets fail. Take the case of automated deployment that occurs before running integration tests. If the target is invoked by the inbuilt test targets (probably by overriding AfterTest), then it will be unclear which part of the build broken if the deployment target fails without trawling through the build log. With custom build steps, the report can identify that deployment broke rather than because of running tests.

[0]: /files/WindowsLiveWriter/TeamBuildCustombuildsteps_E63E/image_2.png
[1]: /files/WindowsLiveWriter/TeamBuildCustombuildsteps_E63E/image_thumb.png
