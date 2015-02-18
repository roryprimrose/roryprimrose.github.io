---
title: TeamBuild: Custom build steps
categories : .Net
tags : Sandcastle, TeamBuild
date: 2009-04-08 16:30:00 +10:00
---

<p>I've been working with team build a fair bit over the last month. Yesterday I was bouncing through the MSDN documentation during my MSBuild travels and I came across a very handy build task. The BuildStep task allows you to create custom entries in the build report.</p>  <p>Take the following build target for example:</p>  <div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:1ebb6d01-8050-4e6a-8ab3-930b5eeeae69" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}&lt;Target Name="RunIntegrationTests"&gt;
 
  &lt;BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
             BuildUri="$(BuildUri)"
             Name="Running integration tests"
             Message="Updating integration test configuration"&gt;
    &lt;Output TaskParameter="Id"
            PropertyName="IntegrationTestsBuildStepId" /&gt;
  &lt;/BuildStep&gt;
  
  &lt;CallTarget Targets="UpdateIntegrationTestConfiguration" /&gt;
 
  &lt;BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
             BuildUri="$(BuildUri)"
             Id="$(IntegrationTestsBuildStepId)"
             Message="Executing integration tests" /&gt;
 
  &lt;CallTarget Targets="ExecuteIntegrationTests" /&gt;
 
  &lt;BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
             BuildUri="$(BuildUri)"
             Id="$(IntegrationTestsBuildStepId)"
             Status="Succeeded" /&gt;
 
&lt;/Target&gt;
{% endhighlight %}</div>

<p>The BuildStep tasks have allowed the build report to include custom steps that are being executed. In this case, it is reporting on the progress of running integration tests.</p>

<p><a href="//blogfiles/WindowsLiveWriter/TeamBuildCustombuildsteps_E63E/image_2.png"><img style="border-right-width: 0px; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px" border="0" alt="image" src="//blogfiles/WindowsLiveWriter/TeamBuildCustombuildsteps_E63E/image_thumb.png" width="324" height="107" /></a> </p>

<p>You can also see that this report contains a custom build step for working with Sandcastle documentation. </p>

<p>This is a very easy to use task that adds rich information to your build reports. It is especially useful for when custom targets fail. Take the case of automated deployment that occurs before running integration tests. If the target is invoked by the inbuilt test targets (probably by overriding AfterTest), then it will be unclear which part of the build broken if the deployment target fails without trawling through the build log. With custom build steps, the report can identify that deployment broke rather than because of running tests.</p>
