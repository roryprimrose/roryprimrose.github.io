---
title: Injecting a version number into WiX via TeamBuild
categories : .Net
tags : TeamBuild, WiX
date: 2009-05-04 10:32:02 +10:00
---

This has been an interesting nut to crack. There are several published ways to inject a version number into WiX as part of a TeamBuild process. This is the way that I have found has the best results.

The following target overrides the ResolveSolutionPathsForEndToEndIteration MSBuild target to inject a $(VersionNumber) value if a custom version number has been defined or calculated in the build script. The default implementation of ResolveSolutionPathsForEndToEndIteration is called if no version number has been defined or calculated.

{% highlight xml linenos %}
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
     
    <PropertyGroup>
     
    <EmptyVersionNumber>0.0.0.0</EmptyVersionNumber>
    <VersionNumber>$(EmptyVersionNumber)</VersionNumber>
     
    </PropertyGroup>
     
    <!-- ResolveSolutionPathsForEndToEndIteration
     
    Override ResolveSolutionPathsForEndToEndIteration to inject VersionNumber property through to Wix Projects
     
    -->
    <Target Name="ResolveSolutionPathsForEndToEndIteration"
            Condition=" '$(IsDesktopBuild)' != 'true' ">
     
    <CallTarget Condition="'$(VersionNumber)' != '$(EmptyVersionNumber)'"
                Targets="ResolveSolutionPathsForEndToEndIterationWithVersionNumber" />
     
    <CallTarget Condition="'$(VersionNumber)' == '$(EmptyVersionNumber)'"
                Targets="ResolveSolutionPathsForEndToEndIterationDefault" />
     
    </Target>
     
    <!-- ResolveSolutionPathsForEndToEndIterationWithVersionNumber
     
    Extends the default ResolveSolutionPathsForEndToEndIteration implementation to inject
    $(VersionNumber) into the solution configuration prior to it being built
     
    -->
    <Target Name="ResolveSolutionPathsForEndToEndIterationWithVersionNumber">
     
    <!-- Ensure SolutionFile property has been set -->
    <Error Condition="'$(SolutionFile)'==''"
            Text="SolutionFile property must be set" />
     
    <Message Text="Reconfiguring solution to accept VersionNumber property. Version Number is $(VersionNumber)" />
     
    <CreateItem
        Include="$(BuildProjectFolderPath)/../../$(SolutionFile)"
        AdditionalMetadata="Properties=VersionNumber=$(VersionNumber);">
        <Output
        TaskParameter="Include"
        ItemName="MySolutionToBuild"/>
    </CreateItem>
     
    <WorkspaceItemConverterTask
        Condition=" '@(MySolutionToBuild)' != '' "
        TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
        BuildUri="$(BuildUri)"
        WorkspaceName="$(WorkspaceName)"
        WorkspaceOwner="$(WorkspaceOwner)"
        ServerItems="@(MySolutionToBuild)">
        <Output TaskParameter="LocalItems"
                ItemName="LocalSolutionToBuild" />
    </WorkspaceItemConverterTask>
     
    <WorkspaceItemConverterTask
        Condition=" '@(SolutionToPublish)' != '' "
        TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
        BuildUri="$(BuildUri)"
        WorkspaceName="$(WorkspaceName)"
        WorkspaceOwner="$(WorkspaceOwner)"
        ServerItems="@(SolutionToPublish)">
        <Output TaskParameter="LocalItems"
                ItemName="LocalSolutionToPublish" />
    </WorkspaceItemConverterTask>
     
    </Target>
     
    <!-- ResolveSolutionPathsForEndToEndIterationDefault
     
    Runs the default ResolveSolutionPathsForEndToEndIteration implementation 
    This is done when no VersionNumber has been defined
     
    -->
    <Target Name="ResolveSolutionPathsForEndToEndIterationDefault">
     
    <WorkspaceItemConverterTask
        Condition=" '@(SolutionToBuild)' != '' "
        TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
        BuildUri="$(BuildUri)"
        WorkspaceName="$(WorkspaceName)"
        WorkspaceOwner="$(WorkspaceOwner)"
        ServerItems="@(SolutionToBuild)">
        <Output TaskParameter="LocalItems"
                ItemName="LocalSolutionToBuild" />
    </WorkspaceItemConverterTask>
     
    <WorkspaceItemConverterTask
        Condition=" '@(SolutionToPublish)' != '' "
        TeamFoundationServerUrl="$(TeamFoundationServerUrl)"
        BuildUri="$(BuildUri)"
        WorkspaceName="$(WorkspaceName)"
        WorkspaceOwner="$(WorkspaceOwner)"
        ServerItems="@(SolutionToPublish)">
        <Output TaskParameter="LocalItems"
                ItemName="LocalSolutionToPublish" />
    </WorkspaceItemConverterTask>
     
    </Target>
     
</Project>
    
{% endhighlight %}

The only disadvantage of this solution is that the WiX project needs to be manually updated to include the VersionNumber property. Once this is done, the VersionNumber needs to be defined in the DefineConstants property which is available in the project properties GUI. For example:

{% highlight xml linenos %}
<Project DefaultTargets="Build"
            xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <ProductVersion>3.0</ProductVersion>
    <ProjectGuid>{1169cfc1-c022-40ec-b4b0-665acc3d45f2}</ProjectGuid>
    <SchemaVersion>2.0</SchemaVersion>
    <OutputName>MyProject.Deployment</OutputName>
    <OutputType>Package</OutputType>
    <WixTargetsPath Condition=" '$(WixTargetsPath)' == '' ">$(MSBuildExtensionsPath)\Microsoft\WiX\v3.0\Wix.targets</WixTargetsPath>
    <WixToolPath>$(ProgramFiles)\Windows Installer XML v3\bin\</WixToolPath>
    <SccProjectName>SAK</SccProjectName>
    <SccProvider>SAK</SccProvider>
    <SccAuxPath>SAK</SccAuxPath>
    <SccLocalPath>SAK</SccLocalPath>
    <Name>MyProject.Deployment</Name>
    <RunPostBuildEvent>OnBuildSuccess</RunPostBuildEvent>
    <VersionNumber>0.0.0.0</VersionNumber>
    </PropertyGroup>
    <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|x86' ">
    <DefineConstants>Debug;VersionNumber=$(VersionNumber)</DefineConstants>
    </PropertyGroup>
    <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x86' ">
    <DefineConstants>Debug;VersionNumber=$(VersionNumber)</DefineConstants>
    </PropertyGroup>
    <PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
    <OutputPath>bin\Debug\</OutputPath>
    <IntermediateOutputPath>obj\Debug\</IntermediateOutputPath>
    <DefineConstants>Debug</DefineConstants>
    <SuppressIces>
    </SuppressIces>
    <SuppressValidation>True</SuppressValidation>
    <CompilerAdditionalOptions>
    </CompilerAdditionalOptions>
    <LinkerAdditionalOptions>
    </LinkerAdditionalOptions>
    </PropertyGroup>
    <PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <OutputPath>bin\Release\</OutputPath>
    <IntermediateOutputPath>obj\Release\</IntermediateOutputPath>
    <SuppressIces>
    </SuppressIces>
    <SuppressValidation>True</SuppressValidation>
    <CompilerAdditionalOptions>
    </CompilerAdditionalOptions>
    <LinkerAdditionalOptions>
    </LinkerAdditionalOptions>
    <Cultures>
    </Cultures>
    <DefineConstants>Debug</DefineConstants>
    <LeaveTemporaryFiles>False</LeaveTemporaryFiles>
    <LibBindFiles>False</LibBindFiles>
    <SuppressPdbOutput>False</SuppressPdbOutput>
    <SuppressSpecificWarnings>
    </SuppressSpecificWarnings>
    <TreatWarningsAsErrors>True</TreatWarningsAsErrors>
    <VerboseOutput>False</VerboseOutput>
    <WixVariables>
    </WixVariables>
    <SuppressAllWarnings>False</SuppressAllWarnings>
    <Pedantic>False</Pedantic>
    </PropertyGroup>
</Project>
    
{% endhighlight %}

This solution makes the version number available to the WiX script.

{% highlight xml linenos %}
<?xml version="1.0" encoding="utf-8"?>
<?define COMPANY = "MyCompany" ?>
<?define PRODUCTNAME = "MyProject" ?>
<?define PRODUCTNAME_SHORT = "MyProject" ?>
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi"
        xmlns:iis="http://schemas.microsoft.com/wix/IIsExtension"
        xmlns:util="http://schemas.microsoft.com/wix/UtilExtension">
    <Product Id="9e97adf1-8bd1-4b2b-a016-7fceecd408e1"
            Name="$(var.PRODUCTNAME)"
            Language="1033"
            Version="$(var.VersionNumber)"
            Manufacturer="$(var.COMPANY)"
            UpgradeCode="85a0e6ea-87b4-4f75-8a2d-21c2b78fa773">
    <Package InstallerVersion="200"
                Compressed="yes"
                Comments="$(var.VersionNumber)" />
    </Product>
</Wix>
    
{% endhighlight %}

This method allows the build script to pass a version number into WiX as a build property. A better solution to this is to extract the version number from the AssemblyInfo.cs from a project in the solution rather than manually providing the version number as a build property. Ideally the projects should reference a common ProductInfo.cs that contains the version number to apply to all projects in the solution. This process can then be extended so that the version number in this common file is updated by the build script before the solution is built.


