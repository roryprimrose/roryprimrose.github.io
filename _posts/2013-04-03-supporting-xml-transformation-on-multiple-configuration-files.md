---
title: Supporting XML transformation on multiple configuration files
categories : .Net
tags : ASP.Net
date: 2013-04-03 09:26:01 +10:00
---

Visual Studio has a great feature for web.config files where XML transformations can be done based on the current configuration. This is typically actioned when the web application is published. Unfortunately the MSBuild scripts only cater for web.config. This is a problem when you start to break up your configuration into multiple files and link them back using the configSource attribute.

    <system.diagnostics configSource=&quot;system.diagnostics.config&quot; /&gt;{% endhighlight %}

One of the great things about MSBuild is that you can change it if you don’t like it. The solution to this issue is to open the proj file and add the following at the end of the file.

    <?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
    <Project ToolsVersion=&quot;4.0&quot; DefaultTargets=&quot;Build&quot; xmlns=&quot;http://schemas.microsoft.com/developer/msbuild/2003&quot;&gt;
    
      <!-- Existing proj content is here --&gt;
    
      <PropertyGroup&gt;
        <TransformWebConfigCoreDependsOn&gt;
          $(TransformWebConfigCoreDependsOn);
          TransformAdditionalConfigurationFiles;
        </TransformWebConfigCoreDependsOn&gt;
      </PropertyGroup&gt;
      <UsingTask TaskName=&quot;GetTransformFiles&quot; TaskFactory=&quot;CodeTaskFactory&quot; AssemblyFile=&quot;$(MSBuildToolsPath)\Microsoft.Build.Tasks.v4.0.dll&quot;&gt;
        <ParameterGroup&gt;
          <ProjectRoot ParameterType=&quot;System.String&quot; Required=&quot;true&quot; /&gt;
          <BuildConfiguration ParameterType=&quot;System.String&quot; Required=&quot;true&quot; /&gt;
          <BaseNames ParameterType=&quot;System.String[]&quot; Output=&quot;true&quot; /&gt;
        </ParameterGroup&gt;
        <Task&gt;
          <Reference Include=&quot;System.IO&quot; /&gt;
          <Using Namespace=&quot;System.IO&quot; /&gt;
          <Code Type=&quot;Fragment&quot; Language=&quot;cs&quot;&gt;<![CDATA[
     
        var configSuffix = &quot;.&quot; + BuildConfiguration + &quot;.config&quot;;
        var matchingConfigurations = Directory.GetFiles(ProjectRoot, &quot;*&quot; + configSuffix);
        var files = new List<String&gt;();
            
        foreach (String item in matchingConfigurations)
        {
            var filename = Path.GetFileName(item);
            var suffixIndex = filename.IndexOf(configSuffix);
            var prefix = filename.Substring(0, suffixIndex);
                
            if (prefix.Equals(&quot;Web&quot;, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            var targetFile = prefix + &quot;.config&quot;;
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
          ]]&gt;</Code&gt;
        </Task&gt;
      </UsingTask&gt;
      <UsingTask TaskName=&quot;TransformXml&quot; AssemblyFile=&quot;$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v11.0\Web\Microsoft.Web.Publishing.Tasks.dll&quot; /&gt;
      <Target Name=&quot;TransformAdditionalConfigurationFiles&quot;&gt;
        <Message Text=&quot;Searching for configuration transforms in $(ProjectDir).&quot; /&gt;
        <GetTransformFiles ProjectRoot=&quot;$(ProjectDir)&quot; BuildConfiguration=&quot;$(Configuration)&quot;&gt;
          <Output TaskParameter=&quot;BaseNames&quot; ItemName=&quot;BaseNames&quot; /&gt;
        </GetTransformFiles&gt;
        <TransformXml Source=&quot;%(BaseNames.Identity).config&quot; Transform=&quot;%(BaseNames.Identity).$(Configuration).config&quot; Destination=&quot;%(BaseNames.Identity).config&quot; Condition=&quot;'@(BaseNames)' != ''&quot; /&gt;
        <Message importance=&quot;high&quot; Text=&quot;Transformed %(BaseNames.Identity).config using %(BaseNames.Identity).$(Configuration).config.&quot; Condition=&quot;'@(BaseNames)' != ''&quot; /&gt;
      </Target&gt;
    </Project&gt;{% endhighlight %}

This script hooks into the process that runs the TransformXml task on web.config and runs it on all other *.Configuration.config files that it can find.


