---
title: Supporting XML transformation on multiple configuration files
categories : .Net
tags : ASP.Net
date: 2013-04-03 09:26:01 +10:00
---

Visual Studio has a great feature for web.config files where XML transformations can be done based on the current configuration. This is typically actioned when the web application is published. Unfortunately the MSBuild scripts only cater for web.config. This is a problem when you start to break up your configuration into multiple files and link them back using the configSource attribute.

<!--more-->

{% highlight xml %}
<system.diagnostics configSource="system.diagnostics.config" />
{% endhighlight %}

One of the great things about MSBuild is that you can change it if you don’t like it. The solution to this issue is to open the proj file and add the following at the end of the file.

{% highlight xml %}
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    
    <!-- Existing proj content is here -->
    
    <PropertyGroup>
    <TransformWebConfigCoreDependsOn>
        $(TransformWebConfigCoreDependsOn);
        TransformAdditionalConfigurationFiles;
    </TransformWebConfigCoreDependsOn>
    </PropertyGroup>
    <UsingTask TaskName="GetTransformFiles" TaskFactory="CodeTaskFactory" AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.v4.0.dll">
    <ParameterGroup>
        <ProjectRoot ParameterType="System.String" Required="true" />
        <BuildConfiguration ParameterType="System.String" Required="true" />
        <BaseNames ParameterType="System.String[]" Output="true" />
    </ParameterGroup>
    <Task>
        <Reference Include="System.IO" />
        <Using Namespace="System.IO" />
        <Code Type="Fragment" Language="cs"><![CDATA[
     
    var configSuffix = "." + BuildConfiguration + ".config";
    var matchingConfigurations = Directory.GetFiles(ProjectRoot, "*" + configSuffix);
    var files = new List<String>();
            
    foreach (String item in matchingConfigurations)
    {
        var filename = Path.GetFileName(item);
        var suffixIndex = filename.IndexOf(configSuffix);
        var prefix = filename.Substring(0, suffixIndex);
                
        if (prefix.Equals("Web", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }
            
        var targetFile = prefix + ".config";
        var targetPath = Path.Combine(ProjectRoot, targetFile);
        var targetInfo = new FileInfo(targetPath);
            
        if (targetInfo.Exists == false)
        {
            continue;
        }
            
        if (targetInfo.IsReadOnly)
        {
            targetInfo.IsReadOnly = false;
        }
                
        files.Add(prefix);
    }
     
    BaseNames = files.ToArray();
        ]]></Code>
    </Task>
    </UsingTask>
    <UsingTask TaskName="TransformXml" AssemblyFile="$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v11.0\Web\Microsoft.Web.Publishing.Tasks.dll" />
    <Target Name="TransformAdditionalConfigurationFiles">
    <Message Text="Searching for configuration transforms in $(ProjectDir)." />
    <GetTransformFiles ProjectRoot="$(ProjectDir)" BuildConfiguration="$(Configuration)">
        <Output TaskParameter="BaseNames" ItemName="BaseNames" />
    </GetTransformFiles>
    <TransformXml Source="%(BaseNames.Identity).config" Transform="%(BaseNames.Identity).$(Configuration).config" Destination="%(BaseNames.Identity).config" Condition="'@(BaseNames)' != ''" />
    <Message importance="high" Text="Transformed %(BaseNames.Identity).config using %(BaseNames.Identity).$(Configuration).config." Condition="'@(BaseNames)' != ''" />
    </Target>
</Project>
{% endhighlight %}

This script hooks into the process that runs the TransformXml task on web.config and runs it on all other *.Configuration.config files that it can find.


