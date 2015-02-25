---
title: Creating a simple bootstrapper - Project template
categories : .Net, My Software
tags : Extensibility, WiX
date: 2011-07-20 14:35:34 +10:00
---

The [previous post][0] looked at a proof of concept for building a simple bootstrapper solution to elevate the execution of an MSI package. This post will outline how this proof of concept was converted into a VSIX project template.

Using a project template was going to be the best way to achieve portability and reusability of this bootstrapper concept. The initial attempts at producing a project template were to use the inbuilt File –&gt; Export template function in Visual Studio on the proof of concept project. This was very cumbersome and difficult to debug. I came across [a post][1] by Aaron Marten that explains how to use the Visual Studio SDK tools to make this a smooth development process. Now that we can actually produce a VSIX package, the main issue from the previous post needs to be addressed. 

The biggest limitation with the proof of concept project was that the resource file xml needs to reference the MSI package with a static location. This is going to be a problem when the MSI package source is the output of a WiX project in the same solution. The WiX project by default will either output the MSI to bin\Debug or bin\Release depending on the current build configuration. This is going to cause problems with the resource file needing a single location for the MSI package.

The answer to this was to change the proj file in the template to provide some custom MSBuild logic to move the MSI package from the WiX target directory to a known location for the resource file. The resource xml file in the project template was updated to point to the obj directory as the static source.

{% highlight xml %}
<data name="Package" type="System.Resources.ResXFileRef, System.Windows.Forms">
    <value>..\obj\Package;System.Byte[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
</data>
{% endhighlight %}

The project file in the template then had its BeforeBuild target changed to ensure that the package is copied to the resource xml static file location before the bootstrapper project is compiled.

{% highlight xml %}
<Target Name="BeforeBuild">
    <PropertyGroup>
    <PackagePath>$packagePath$</PackagePath>
    </PropertyGroup>
    <ItemGroup>
    <PackageSourceFiles Include="$(PackagePath)" />
    </ItemGroup>
    <ConvertToAbsolutePath Paths="@(PackageSourceFiles)">
    <Output TaskParameter="AbsolutePaths" ItemName="AbsoluteFiles" />
    </ConvertToAbsolutePath>
    <ItemGroup>
    <PackageDestinationFiles Include="$(ProjectDir)$(BaseIntermediateOutputPath)Package" />
    </ItemGroup>
    <Message Condition="Exists(@(AbsoluteFiles))" Text="Found package at path '@(AbsoluteFiles)'" />
    <Error Condition="Exists(@(AbsoluteFiles)) == false" Text="The package at path '@(AbsoluteFiles)' was not found" />
    <Copy SourceFiles="@(AbsoluteFiles)" DestinationFiles="@(PackageDestinationFiles)" OverwriteReadOnlyFiles="true" Condition="Exists($(PackagePath))" />
</Target>
{% endhighlight %}

The $packagePath$ variable is populated by a wizard in the project template. The wizard will determine this path based on user input. The path may end up having $(OutputPath) substituted into it. This will then allow the bootstrapper project respond to changes of build configuration. In doing this, the project is able to take a dynamic output path of a WiX project and copy the file into the static location for the resource xml file to reference. The rest of the MSBuild script copies the MSI package if it is available or throws a build failure if it is not found. 

There is still a downside with this implementation. There is an assumption that the $(OutputPath) of the bootstrapper project is the same as the WiX project. This is fairly safe as these are the default values when creating these types of projects. The bootstrapper project will fail the build where the MSI package is not found.![image][2]

The project template includes an IWizard implementation to make creating the bootstrapper project a little easier. The bootstrapper project template needs to know where to find the MSI package to include in its compilation process. There were two possibilities for this to happen. The MSI would either be available somewhere on the file system and not related to the current solution, or the MSI was going to be a compilation output of another project in the solution.

The wizard looks at the current solution and determines whether there is a project that compiles an MSI file. Realistically, only WiX projects (tested with 3.5) are supported for this. If no projects are available, then the user is only able to select an existing MSI from the file system.![image][3]

If the solution does have an MSI project, then the first option is enabled and the dropdown list contains the suitable matches.![image][4]

Once a project is selected or a file path is defined, the wizard displays the path that will be put into the $packagePath$ variable in the bootstrapper project BeforeBuild MSBuild target. The wizard will calculate this value as a relative path where possible. It will also determine if the path contains bin\Debug or bin\Release and allow $(OutputPath) to be substituted in. The MSBuild task will then calculate this value at runtime.

If an existing solution project is selected as the MSI package source, the wizard then creates a build dependency between the bootstrapper and the WiX project. ![image][5]

This will ensure that the WiX project will be built first when the bootstrapper project is built. This will therefore ensure that the WiX project output (the MSI package) will be available for the MSBuild target to copy to the static path expected by the bootstrapper resource xml file.

The bootstrapper project can then be changed by the developer according to their requirements once it is added to the solution. The project template includes some logic to install the MSI package (the default action), uninstall the MSI and to simply extract the MSI package. The install and uninstall tasks also cater for additional MSI arguments provided to the bootstrapper over the command line.

The bootstrapper can be compiled immediately to produce the bootstrapper application.![image][6]

Executing this application will elevate the application as is defined by the app.manifest.![image][7]

Once elevated, the bootstrapper extracts and executes the MSI package.![image][8]

Easy done. 

You can download this template from the Visual Studio gallery at [here][9]. The code for this template can be found on CodePlex in the [NeovolveX project][10].

[0]: /2011/07/19/creating-a-simple-bootstrapper-proof-of-concept/
[1]: http://blogs.msdn.com/b/visualstudio/archive/2010/03/04/creating-and-sharing-project-item-templates.aspx
[2]: /files/image_124.png
[3]: /files/image_125.png
[4]: /files/image_126.png
[5]: /files/image_127.png
[6]: /files/image_128.png
[7]: /files/image_129.png
[8]: /files/image_130.png
[9]: http://visualstudiogallery.msdn.microsoft.com/01e9dab4-73de-4126-91c0-2a207c52b9cd
[10]: http://neovolvex.codeplex.com
