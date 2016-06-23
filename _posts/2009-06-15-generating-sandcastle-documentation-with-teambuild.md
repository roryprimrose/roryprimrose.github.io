---
title: Generating Sandcastle documentation with TeamBuild
categories: .Net
tags: Sandcastle, TeamBuild
date: 2009-06-15 16:00:26 +10:00
---

Automatically generating technical documentation from code comments is really easy with Sandcastle and SHFB. If you are using TeamBuild to provide continuous integration then this is a great place to ensure up to date documentation is being produced. There a two ways that Sandcastle can be used to generate documentation. The first is dynamically without a SHFB project file and the second is with a SHFB project file.

Dynamically creating documentation is an easy solution that essentially documents all dll files found in the build directory with some known exclusions. This has the advantage that you don’t need to manage the documentation configuration as assemblies are added and removed from the solution. The disadvantages is that it potentially generates documentation for more assemblies than intended, namely the dependencies for the solution. The dynamic documentation generation is really good for framework/toolkit type solutions that don’t have external dependencies. The dynamic solution tells SHFB the information that it requires that would otherwise be defined via a project file. The MSBuild script looks something like the following.

<!--more-->

{% highlight xml %}
<Target Name="BuildSandcastleWithDynamicProjectDefinition">
    
    <!-- Uses Sandcastle Help File Builder to build a CHM documentation for all assemblies in the output directory. -->
    <Message Text="Setting empty DocumentationName to ProductName '$(ProductName)'" />
    
    <CreateProperty Value="$(ProductName)"
                    Condition="$(DocumentationName) == ''">
    <Output TaskParameter="Value"
            PropertyName="DocumentationName"/>
    </CreateProperty>
    
    <!-- Find all assemblies in the output directory -->
    <CreateItem Include="$(OutDir)*.dll"
                Exclude="$(OutDir)*Tests.dll;$(OutDir)*_Accessor.dll">
    <Output ItemName="Assemblies"
            TaskParameter="Include" />
    </CreateItem>
    
    <!-- Document assemblies that have corresponding XML documentation files -->
    <CreateItem Include="@(Assemblies)"
                Condition="Exists('%(Assemblies.RelativeDir)%(Assemblies.Filename).xml')">
    <Output ItemName="AssembliesToDocument"
            TaskParameter="Include" />
    </CreateItem>
    
    <!-- Include assemblies that are missing XML documentation files as dependencies -->
    <CreateItem Include="@(Assemblies)"
                Condition="!Exists('%(Assemblies.RelativeDir)%(Assemblies.Filename).xml')">
    <Output ItemName="DependenciesToDocument"
            TaskParameter="Include" />
    </CreateItem>
    
    <!-- Include all files in the \Documentation subdirectory of a project as content -->
    <CreateItem Include="$(SolutionRoot)\**\Documentation\*"
                Exclude="$(SolutionRoot)\**\Thumbs.db">
    <Output ItemName="DocumentationContent"
            TaskParameter="Include" />
    </CreateItem>
    
    <!-- Check if the assemblies for corresponding XML files exist -->
    <Error Text="Assembly not found %(AssembliesToDocument.RelativeDir)%(AssembliesToDocument.Filename).dll, but XML was."
            Condition="!Exists('%(AssembliesToDocument.RelativeDir)%(AssembliesToDocument.Filename).dll')" />
    
    <PropertyGroup>
    <SandcastleBuilderPath>$(ProgramFiles)\EWSoftware\Sandcastle Help File Builder\SandcastleBuilderConsole.exe</SandcastleBuilderPath>
    <SandcastleBuilderArguments>-new @(AssembliesToDocument -> '-assembly=&quot;%(RelativeDir)%(Filename).dll&quot;',' ') @(DependenciesToDocument -> '-dependency=&quot;%(RelativeDir)%(Filename).dll&quot;',' ') @(DocumentationContent -> '-addcontent=&quot;%(Fullpath)*.*,html\Documentation&quot;',' ') -outputpath=&quot;$(OutDir).&quot; -HelpTitle=&quot;Documentation for $(ProductName)&quot; -Language=&quot;en-AU&quot; -HtmlHelpName=&quot;$(DocumentationName)&quot; -FooterText=&quot;Version: $(VersionNumber) <br /> Build Number: $(BuildNumber)&quot; -KeepLogFile=&quot;false&quot; -RootNamespaceContainer=&quot;true&quot; -SyntaxFilters=&quot;CSharp&quot;</SandcastleBuilderArguments>
    </PropertyGroup>
    
    <!-- Execute sandcastle -->
    <Message Text="Running Sandcastle: &quot;$(SandcastleBuilderPath)&quot; $(SandcastleBuilderArguments)" />
    <Exec Command="&quot;$(SandcastleBuilderPath)&quot; $(SandcastleBuilderArguments)" />
    
</Target>
    
{% endhighlight %}

Dynamically creating documentation is easy, but the resultant documentation can become dirty as external dependencies appear in the build directory. This is when using a SHFB project file becomes an advantage. This allows the solution to contain the specific definition of what gets included in the Sandcastle generated documentation. The best option for managing the SHFB project file is to add it as a solution item in Visual Studio. The MSBuild script for using the SHFB project file looks like the following.

{% highlight xml %}
<Target Name="BuildSandcastleProjectFile">
     
    <Message Text="Building Sandcastle documentation using the project '$(SandcastleProjectFilePath)'" />
     
    <Message Text="Setting empty DocumentationName to ProductName '$(ProductName)'" />
     
    <CreateProperty Value="$(ProductName)"
                    Condition="$(DocumentationName) == ''">
    <Output TaskParameter="Value"
            PropertyName="DocumentationName"/>
    </CreateProperty>
     
    <PropertyGroup>
    <SandcastleBuilderArguments>-outputpath=&quot;$(OutDir).&quot; -HelpTitle=&quot;Documentation for $(ProductName)&quot; -Language=&quot;en-AU&quot; -HtmlHelpName=&quot;$(DocumentationName)&quot; -FooterText=&quot;Build Number: $(BuildNumber)&quot; -KeepLogFile=&quot;false&quot; -RootNamespaceContainer=&quot;true&quot; -SyntaxFilters=&quot;CSharp&quot;</SandcastleBuilderArguments>
    </PropertyGroup>
     
    <!-- Execute sandcastle -->
    <Message Text="Running Sandcastle: &quot;$(SandcastleBuilderPath)&quot; &quot;$(SandcastleProjectFilePath)&quot; $(SandcastleBuilderArguments)" />
    <Exec Command="&quot;$(SandcastleBuilderPath)&quot; &quot;$(SandcastleProjectFilePath)&quot; $(SandcastleBuilderArguments)" />
     
</Target>
    
{% endhighlight %}

These targets run the implementation for generating the documentation. The following targets are used to orchestrate this work.

{% highlight xml %}
<PropertyGroup>
     
    <ProductName></ProductName>
    
    <!-- Enables/Disables generation of Sandcastle Documentation -->
    <BuildDocumentation>true</BuildDocumentation>
     
    <!-- 
    Defines the name of the Sandcastle project file to build 
    If not defined, a search is run for *.shfb files. The build fails if multiple project definitions are found.
    -->
    <SandcastleProjectFile></SandcastleProjectFile>
    <SandcastleProjectFilePath Condition="'$(SandcastleProjectFile)' != ''">$(SolutionRoot)\$(SandcastleProjectFile)</SandcastleProjectFilePath>
     
    <!-- Defines the documentation name -->
    <DocumentationName></DocumentationName>
     
    <SandcastleBuilderPath>$(ProgramFiles)\EWSoftware\Sandcastle Help File Builder\SandcastleBuilderConsole.exe</SandcastleBuilderPath>
     
    <!-- Used to store the updated version number for generating Sandcastle documentation -->
    <EmptyVersionNumber>0.0.0.0</EmptyVersionNumber>
    <VersionNumber>$(EmptyVersionNumber)</VersionNumber>
     
</PropertyGroup>
     
<Target Name="GenerateDocumentation"
        Condition="$(BuildDocumentation)">
     
    <GetBuildProperties TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
                        BuildUri="$(BuildUri)">
    <Output TaskParameter="TestSuccess"
            PropertyName="TestSuccess" />
    </GetBuildProperties>
     
    <Message Text="Skipping Sandcastle documentation generation"
            Condition="$(TestSuccess) == false" />
     
    <CallTarget Targets="FindSandcastleProjectFile"
                Condition="$(TestSuccess)" />
     
    <CallTarget Targets="GenerateSandcastleDocumentation"
                Condition="$(TestSuccess)" />
     
</Target>
     
<Target Name="FindSandcastleProjectFile"
        Condition="'$(SandcastleProjectFilePath)' == ''">
     
    <Message Text="No Sandcastle project defined. Searching %24(SolutionRoot)\**\*.shfb" />
     
    <ItemGroup>
     
    <SandcastleProjectDefinitionFiles Include="$(SolutionRoot)\**\*.shfb" />
     
    </ItemGroup>
     
    <Message Text="Searching failed to find a Sandcastle project file."
            Condition="'@(SandcastleProjectDefinitionFiles)' == ''" />
    <Message Text="Found the following Sandcastle project files: %0D%0A @(SandcastleProjectDefinitionFiles -> '%(FullPath)', '%0D%0A')"
            Condition="'@(SandcastleProjectDefinitionFiles)' != ''" />
     
    <StringComparison Comparison="Contains"
                    Param1="@(SandcastleProjectDefinitionFiles)"
                    Param2=";"
                    Condition="'@(SandcastleProjectDefinitionFiles)' != ''">
    <Output TaskParameter="Result"
            ItemName="MultipleSandcastleProjectsFound" />
    </StringComparison>
     
    <Error Text="Multiple Sandcastle project definitions found."
            Condition="'@(SandcastleProjectDefinitionFiles)' != '' And @(MultipleSandcastleProjectsFound) == true" />
     
    <PropertyGroup>
     
    <SandcastleProjectFilePath>@(SandcastleProjectDefinitionFiles)</SandcastleProjectFilePath>
     
    </PropertyGroup>
     
</Target>
     
<Target Name="GenerateSandcastleDocumentation">
    
    <BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
                BuildUri="$(BuildUri)"
                Name="Generating Sandcastle documentation"
                Message="Generating Sandcastle documentation">
    <Output TaskParameter="Id"
            PropertyName="SandcastleBuildStepId" />
    </BuildStep>
     
    <!-- Check if builder and files exist -->
    <Error Text="Sandcastle Help File Builder is not found at $(SandcastleBuilderPath)."
            Condition="!Exists('$(SandcastleBuilderPath)')"  />
     
    <Error Text="No ProductName property value has been defined."
            Condition="'$(ProductName)' == ''" />
     
    <Error Text="Sandcastle project file '$(SandcastleProjectFilePath)' does not exist"
            Condition="'$(SandcastleProjectFilePath)' != '' And !Exists('$(SandcastleProjectFilePath)')" />
     
    <BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
                BuildUri="$(BuildUri)"
                Id="$(SandcastleBuildStepId)"
                Message="Compiling Sandcastle documentation project"
                Condition="'$(SandcastleProjectFilePath)' != ''" />
     
    <CallTarget Targets="BuildSandcastleProjectFile"
                Condition="'$(SandcastleProjectFilePath)' != ''" />
     
    <BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
                BuildUri="$(BuildUri)"
                Id="$(SandcastleBuildStepId)"
                Message="Compiling Sandcastle documentation without a project"
                Condition="'$(SandcastleProjectFilePath)' == ''" />
     
    <CallTarget Targets="BuildSandcastleWithDynamicProjectDefinition"
                Condition="'$(SandcastleProjectFilePath)' == ''" />
     
    <BuildStep TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
                BuildUri="$(BuildUri)"
                Id="$(SandcastleBuildStepId)"
                Status="Succeeded" />
     
</Target>    
{% endhighlight %}


