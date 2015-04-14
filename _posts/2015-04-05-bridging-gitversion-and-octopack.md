---
title: Bridging GitVersion and OctoPack
categories: .Net
tags: 
date: 2015-04-05 00:05:00 +11:00
---

I've started converting projects from TFSC to Git in VSO. As part of this process I am working out the best way to achieve semantic versioning. [GitVersion][0] looks like a great product to assist in this. By default however the version numbers that it produces are not supported by NuGet.

The particular problem with NuGet version numbers can be read in more detail [here][1].

<!--more-->

More specifically, I am using OctoPack to generate a NuGet package as part of a build and publish it to an [Octopus Deploy][2] instance. I need to get the generated version number from GitVersion and pipe it into the OctoPackPackageVersion MsBuild property prior to OctoPack being run. 

If you look at the targets file for OctoPack, it overrides and appends OctoPack to the BuildDependsOn MsBuild property. We can do the same by overriding BuildDependsOn before OctoPack overrides it. This will allow us to update the OctoPackPackageVersion property.

I updated the proj file to include a new BuildDependsOn property just before the OctoPack target is imported. The bottom of the proj file now contains this customisation, the OctoPack target and the GitVersionTask target.

{% highlight xml %}

<!-- Hook into the AfterBuild activity -->
<PropertyGroup>
<BuildDependsOn>
    $(BuildDependsOn);
    SetOctoPackVersion
</BuildDependsOn>
</PropertyGroup>

<!-- 
Create Octopus Deploy package
-->
<Target Name="SetOctoPackVersion">
<PropertyGroup>
    <OctoPackPackageVersion>$(GfvNuGetVersion)</OctoPackPackageVersion>
</PropertyGroup>
</Target>
  
<Import Project="..\packages\OctoPack.3.0.42\tools\OctoPack.targets" Condition="Exists('..\packages\OctoPack.3.0.42\tools\OctoPack.targets')" />
<Target Name="EnsureOctoPackImported" BeforeTargets="BeforeBuild" Condition="'$(OctoPackImported)' == ''">
<Error Condition="!Exists('..\packages\OctoPack.3.0.42\tools\OctoPack.targets') And ('$(RunOctoPack)' != '' And $(RunOctoPack))" Text="You are trying to build with OctoPack, but the NuGet targets file that OctoPack depends on is not available on this computer. This is probably because the OctoPack package has not been committed to source control, or NuGet Package Restore is not enabled. Please enable NuGet Package Restore to download them. For more information, see http://go.microsoft.com/fwlink/?LinkID=317567." HelpKeyword="BCLBUILD2001" />
<Error Condition="Exists('..\packages\OctoPack.3.0.42\tools\OctoPack.targets') And ('$(RunOctoPack)' != '' And $(RunOctoPack))" Text="OctoPack cannot be run because NuGet packages were restored prior to the build running, and the targets file was unavailable when the build started. Please build the project again to include these packages in the build. You may also need to make sure that your build server does not delete packages prior to each build. For more information, see http://go.microsoft.com/fwlink/?LinkID=317568." HelpKeyword="BCLBUILD2002" />
</Target>
<Import Project="..\packages\GitVersionTask.2.0.1\Build\GitVersionTask.targets" Condition="Exists('..\packages\GitVersionTask.2.0.1\Build\GitVersionTask.targets')" />
<Target Name="EnsureNuGetPackageBuildImports" BeforeTargets="PrepareForBuild">
<PropertyGroup>
    <ErrorText>This project references NuGet package(s) that are missing on this computer. Enable NuGet Package Restore to download them.  For more information, see http://go.microsoft.com/fwlink/?LinkID=322105. The missing file is {0}.</ErrorText>
</PropertyGroup>
<Error Condition="!Exists('..\packages\GitVersionTask.2.0.1\Build\GitVersionTask.targets')" Text="$([System.String]::Format('$(ErrorText)', '..\packages\GitVersionTask.2.0.1\Build\GitVersionTask.targets'))" />
</Target>
{% endhighlight %}

BuildDependsOn is overriden to call SetOctoPackVersion. OctoPack then overrides this to call OctoPack. This means that the execution flow will be the original BuildDependsOn targets -> SetOctoPackVersion -> OctoPack.

The SetOctoPackVersion gets the NuGet safe version calculated by GitVersion (via GitVersionTask) and pushes it into the version property that OctoPack uses. The build will now succeed and we have a good solution for SemVer with NuGet/OctopusDeploy.

[0]: https://github.com/ParticularLabs/GitVersion
[1]: https://github.com/ParticularLabs/GitVersion#nuget-compatibility
[2]: http://www.octopusdeploy.com