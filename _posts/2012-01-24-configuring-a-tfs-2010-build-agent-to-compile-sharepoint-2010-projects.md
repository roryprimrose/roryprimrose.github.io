---
title: Configuring a TFS 2010 build agent to compile SharePoint 2010 projects
categories : .Net
tags : TeamBuild, TFS, WiX
date: 2012-01-24 12:09:40 +10:00
---

I’ve been doing several TFS consulting gigs over the last couple of years. The one thing that keeps popping up is the requirement to build several types of platforms using TeamBuild/TFSBuild. My preference is to isolate build agents and their customisations. This means for example that I have a mix of build agents like the following:

* Standard (full VS install + any common additions - WiX for example)
* SharePoint
* BizTalk

Tagging the build agents and configuring the build definitions for those tags then allows the build to be directed to the correct build agent.

The next consideration is that the customisations to the build agent will ideally contain just what is required to build solutions and run unit tests. A SharePoint build agent for example should not have a full SharePoint installed on it. Instead it can just have the required assembly dependencies registered on it to allow the build to succeed.

There is a PowerShell script floating around that assists in harvesting SharePoint assemblies from a development server to then install and configure them on the SharePoint build agent. The original script was published by Microsoft (see [here][0]) and subsequently updated by the [SharePoint Developer Team][1] to include additional assemblies. The later post also provides a great walkthrough for creating SharePoint build definitions.

We are finding in my current consultation that the client is referencing even more assemblies. Complicating the matter is that several of the assemblies exist in the GAC as both x86 and x64. The PowerShell script does not handle this case and throws an error.

I have updated the PowerShell script to include the following changes:

* Renders all matching files when multiple files match the file name (used to then retarget the included files, see next point)
* Allowed full file paths to be included (including GAC paths in order to specify a particular GAC entry)
* Skipped harvesting files that have already been harvested

  * Includes skipping the recursive searching for a filename which was the slowest part of the script

* Added defensive code for updating the registry to point to installed assemblies (previous script threw an error if the registry key already existed)

Here is my version of the SharePoint harvest script.

    #==============================================================================
    # This script collects and installs the files required to build SharePoint
    # projects on a TFS build server. It supports both TFS 2010 and TFS 2008.
    #
    # Step 1. Run the script on a developer machine where VS 2010 and SP 2010 are installed.
    #         Use the &quot;-Collect&quot; parameter to collect files required for installation.
    #
    # Step 2. Copy the script and folder of collected files to a TFS build server.
    #         Use the &quot;-Install&quot; parameter to install the files.
    #
    # Copyright (c) 2010 Microsoft Corp.
    #==============================================================================
    
    param (
      [switch]$Collect, 
      [switch]$Install, 
      [switch]$Quiet
    )
    
    #==============================================================================
    # Script variables
    #==============================================================================
    
    # Name of the script file
    $MyScriptFileName = Split-Path $MyInvocation.MyCommand.Path -Leaf
    
    # Directory from which script is run
    $MyScriptDirectory = Split-Path $MyInvocation.MyCommand.Path
    
    function InitializeScriptVariables()
    {
      # Program Files path is different on 32- and 64-bit machines.
      $Script:ProgramFilesPath = GetProgramFilesPath
      
      # SharePoint assemblies that can be referenced from SharePoint projects.
      # If you need more assemblies, add them here.
      $Script:SharePoint14ReferenceAssemblies = @( 
    &quot;Microsoft.Office.Policy.dll&quot;, 
    &quot;Microsoft.Office.Server.dll&quot;, 
    &quot;C:\Windows\Assembly\GAC_64\Microsoft.Office.Server.Search\14.0.0.0__71e9bce111e9429c\Microsoft.Office.Server.Search.dll&quot;,
    &quot;Microsoft.Office.Server.UserProfiles.dll&quot;, 
    &quot;Microsoft.Office.Workflow.Feature.dll&quot;,
    &quot;C:\Windows\assembly\GAC_MSIL\Microsoft.Office.Workflow.Feature\14.0.0.0__71e9bce111e9429c\Microsoft.Office.Workflow.Feature.dll&quot;,
    &quot;Microsoft.SharePoint.Client.dll&quot;, 
    &quot;Microsoft.SharePoint.Client.Runtime.dll&quot;, 
    &quot;Microsoft.SharePoint.Client.ServerRuntime.dll&quot;, 
    &quot;Microsoft.SharePoint.dll&quot;, 
    &quot;Microsoft.SharePoint.IdentityModel.dll&quot;,
    &quot;Microsoft.SharePoint.Linq.dll&quot;, 
    &quot;Microsoft.SharePoint.Portal.dll&quot;, 
    &quot;Microsoft.SharePoint.Publishing.dll&quot;,
    &quot;C:\Windows\Assembly\GAC_64\Microsoft.SharePoint.Search\14.0.0.0__71e9bce111e9429c\Microsoft.SharePoint.Search.dll&quot;, 
    &quot;Microsoft.SharePoint.Security.dll&quot;, 
    &quot;Microsoft.SharePoint.Taxonomy.dll&quot;, 
    &quot;Microsoft.SharePoint.WorkflowActions.dll&quot;, 
    &quot;Microsoft.Web.CommandUI.dll&quot;,
    &quot;System.Web.DataVisualization.dll&quot;
      )  
    
      # Destination path where SharePoint14 assemblies will be copied
      $Script:SharePoint14ReferenceAssemblyPath = Join-Path $ProgramFilesPath &quot;Reference Assemblies\Microsoft\SharePoint14&quot;
    
      # Folder where we collect assemblies and use as a source for installation
      $Script:FilesFolder = &quot;Files&quot;
      $Script:FilesPath = Join-Path $MyScriptDirectory $FilesFolder
    
      # Path to .NET Framework 2.0 GAC
      $Script:Gac20Path = Join-Path $Env:SystemRoot &quot;Assembly&quot;
    
      # Path to .NET Framework 4.0 GAC
      $Script:Gac40Path = Join-Path $Env:SystemRoot &quot;Microsoft.NET\Assembly&quot;
    
      # SharePoint project MSBuild extensions.
      $Script:MSBuildSharePointTools = @(
        &quot;Microsoft.VisualStudio.SharePoint.targets&quot;,
        &quot;Microsoft.VisualStudio.SharePoint.Tasks.dll&quot;
      )
    
      # SharePoint project MSBuild extensions path.
      $Script:MSBuildSharePointToolsPath = Join-Path $ProgramFilesPath &quot;MSBuild\Microsoft\VisualStudio\v10.0\SharePointTools&quot;
    
      # SharePoint project assemblies required to create WSP files.
      $Script:SharePointProjectAssemblies = @(
        &quot;Microsoft.VisualStudio.SharePoint.dll&quot;,
        &quot;Microsoft.VisualStudio.SharePoint.Designers.Models.dll&quot;,
        &quot;Microsoft.VisualStudio.SharePoint.Designers.Models.Features.dll&quot;,
        &quot;Microsoft.VisualStudio.SharePoint.Designers.Models.Packages.dll&quot;
      )
    
      # Assemblies required for DSL. It is a dependency of the SharePoint Tools model assemblies.
      # They are present on TFS 2010 Build. We need them only for TFS 2008 Build.
      $Script:DslAssemblies = @(
        &quot;Microsoft.VisualStudio.Modeling.Sdk.10.0.dll&quot;
      )
    
      # Name of the gacutil config file.
      $Script:GacUtilConfigFileName = &quot;gacutil.exe.config&quot;
    
      # Files needed to install assemblies to the GAC on TFS build machine.
      $Script:GacUtilFiles = @(
        &quot;gacutil.exe&quot;,
        $GacUtilConfigFileName
      )
    
      # Path where the GacUtils.exe can be found on the development machine
      $Script:GacUtilFilePath = Join-Path $ProgramFilesPath &quot;Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools&quot;
    
      # TfsBuildService.exe.config file name to update MSBuildPath for TFS 2008 
      $Script:TfsBuildServiceConfigFileName = &quot;TfsBuildService.exe.config&quot;
    
      # TfsBuildService.exe.config full file path to update MSBuildPath for TFS 2008 
      $Script:TfsBuildServiceConfigFilePath = Join-Path $ProgramFilesPath &quot;Microsoft Visual Studio 9.0\Common7\IDE\PrivateAssemblies\$TfsBuildServiceConfigFileName&quot;
    }
    
    #==============================================================================
    # Top-level script routines
    #==============================================================================
    
    # Install SharePoint project files on TFS build server
    function InstallSharePointProjectFiles() {
      WriteText &quot;Installing SharePoint project files for TFS Build.&quot;
      WriteText &quot;&quot;
    
      WriteText &quot;Checking that the .Net Framework 4.0 is installed:&quot;
      WriteText &quot;Found .Net Framework version $(GetDotNet40Version)&quot;
      WriteText &quot;&quot;
    
      if (TestSharePoint14) {
        WriteText &quot;SharePoint 14 is installed on this machine.&quot;
        WriteText &quot;Skipping installation of SharePoint 14 reference assemblies.&quot;
        WriteText &quot;&quot;
      } else {
        WriteText &quot;Copying the SharePoint reference assemblies:&quot;
        $SharePoint14ReferenceAssemblies | CopyFileToFolder $SharePoint14ReferenceAssemblyPath
        WriteText &quot;&quot;
    
        WriteText &quot;Setting the SharePoint reference assemblies path in the registry:&quot;
        AddSharePoint14ReferencePathToRegistry
        WriteText &quot;&quot;
      }
    
      WriteText &quot;Copying the SharePoint project MSBuild extensions:&quot;
      $MSBuildSharePointTools | CopyFileToFolder $MSBuildSharePointToolsPath
      WriteText &quot;&quot;
    
      WriteText &quot;Installing the SharePoint project assemblies in the GAC:&quot;
      FixGacUtilConfigVersion
      $SharePointProjectAssemblies | InstallAssemblyToGac40
      WriteText &quot;&quot;
    
      WriteText &quot;Installing the DSL assemblies in the GAC:&quot;
      $DslAssemblies | InstallAssemblyToGac40 -SkipIfExists
      WriteText &quot;&quot;
    
      WriteText &quot;Fixing MSBuildPath in $TfsBuildServiceConfigFileName for TFS 2008:&quot;
      FixTfsBuildServiceConfigMSBuildPath
      WriteText &quot;&quot;
    
      WriteText &quot;Installation is complete.&quot;
      WriteText &quot;&quot;
    }
    
    # Collect SharePoint project files on developement machine for TFS build server.
    function CollectSharePointProjectFiles() {
      WriteText &quot;Collecting SharePoint project files for TFS Build.&quot;
      WriteText &quot;&quot;
    
      WriteText &quot;Checking that the .Net Framework 4.0 is installed:&quot;
      WriteText &quot;Found .Net Framework version: $(GetDotNet40Version)&quot;
      WriteText &quot;&quot;
    
      WriteText &quot;Creating the $FilesFolder directory:&quot;  
      $FilesPathInfo = [System.IO.Directory]::CreateDirectory($FilesPath)
      WriteText $FilesPathInfo.FullName
      WriteText &quot;&quot;
    
      WriteText &quot;Collecting the DSL assemblies:&quot;
      $DslAssemblies | CopyFileFromFolder $Gac40Path -Recurse
      WriteText &quot;&quot;
    
      WriteText &quot;Collecting the SharePoint reference assemblies:&quot;
      $SharePoint14ReferenceAssemblies | CopyFileFromFolder $Gac20Path -Recurse
      WriteText &quot;&quot;
    
      WriteText &quot;Collecting the SharePoint project MSBuild extensions:&quot;
      $MSBuildSharePointTools | CopyFileFromFolder $MSBuildSharePointToolsPath
      WriteText &quot;&quot;
    
      WriteText &quot;Collecting the SharePoint project assemblies:&quot;
      $SharePointProjectAssemblies | CopyFileFromFolder $Gac40Path -Recurse
      WriteText &quot;&quot;
    
      WriteText &quot;Collecting the gacutil.exe files:&quot;
      $GacUtilFiles | CopyFileFromFolder $GacUtilFilePath
      WriteText &quot;&quot;
    
      WriteText &quot;Colection is complete.&quot;
      WriteText &quot;&quot;
    }
    
    # Writes instructions how to use the script.
    # If -Quiet flag is provided it throws an exception.
    function WriteHelp() {
      if ($Quiet) {
        throw &quot;Specify either the -Install or -Collect parameter.&quot;
      } else {
        Write-Host &quot;&quot;
        Write-Host &quot;This script collects and installs the files to build SharePoint projects on a TFS build server (2010 or 2008).&quot;
        Write-Host &quot;&quot;
        Write-Host &quot;Parameters:&quot;
        Write-Host &quot;  -Collect   - Collect files into the $FilesFolder folder on a development machine.&quot;
        Write-Host &quot;  -Install   - Install files from the $FilesFolder folder on a TFS build server.&quot;
        Write-Host &quot;  -Quiet     - Suppress messages. Errors will still be shown.&quot;
        Write-Host &quot;&quot;
        Write-Host &quot;Usage:&quot;
        Write-Host &quot;&quot;
        Write-Host &quot;  .\$MyScriptFileName -Collect&quot;
        Write-Host &quot;     - Collects files into the $FilesFolder folder on a development machine.&quot;
        Write-Host &quot;&quot;
        Write-Host &quot;  .\$MyScriptFileName -Install&quot;
        Write-Host &quot;     - Installs files from the $FilesFolder folder on a TFS build server.&quot;
        Write-Host &quot;&quot;
      }
    }
    
    #==============================================================================
    # Utility functions
    #==============================================================================
    
    # Writes to Host if -Quiet flag is not provided
    function WriteText($Value) {
      if (-Not $Quiet) {
        Write-Host $Value
      }
    }
    
    # Gets the Program Files (x86) path
    function GetProgramFilesPath() {
      if (TestWindows64) {
        return ${Env:ProgramFiles(x86)}
      } else {
        return $Env:ProgramFiles
      }
    }
    
    # Installs assembly to the GAC
    function InstallAssemblyToGac40([switch]$SkipIfExists) {
      begin {
        $GacUtilCommand = Join-Path $FilesPath &quot;gacutil.exe&quot;
      }
      process {
        $FileName = $_
        if ($SkipIfExists -and (TestAssemblyInGac40 $FileName)) {
          WriteText &quot;Assembly $FileName is already in the GAC ($Gac40Path).&quot;
          WriteText &quot;Skipping installation in the GAC.&quot;
          return
        }
        
        $FileFullPath = Join-Path $FilesPath $FileName
        & &quot;$GacUtilCommand&quot; /i &quot;$FileFullPath&quot; /nologo
    
        # Verify that assembly was installed to the GAC
        if (-not (TestAssemblyInGac40 $FileName)) {
          throw &quot;Assembly $FileName was not installed in the GAC ($Gac40Path)&quot;
        }
    
        WriteText &quot;$FileName [is installed in ==&gt;] GAC 4.0&quot;
      }
    }
    
    # Test if the assembly file name is already in the GAC
    function TestAssemblyInGac40($AssemblyFileName) {
      $FileInfo = Get-ChildItem $Gac40Path -Recurse | Where { $_.Name -eq $AssemblyFileName }
      return ($FileInfo -ne $null)
    }
    
    # Copies file from the specified path or its sub-directory to the $FilesPath
    function CopyFileFromFolder([string]$Path, [switch]$Recurse) {
      process {
    
        $SourcePath = $_
        $FileName = [System.IO.Path]::GetFileName($_)
        $DestinationPath = Join-Path $FilesPath $FileName
    
        if (-Not (Test-Path $DestinationPath))
        {
            # Check if a full file path has been provided as the source
            if (-not (Test-Path $SourcePath))
            {
                $FileInfo = Get-ChildItem $Path -Recurse:$Recurse | Where { $_.Name -eq $FileName }
    
                if ($FileInfo -eq $null) {
                  throw &quot;File $FileName was not found in $Path&quot;
                }
    
                if ($FileInfo -is [array]) {
                
                    WriteText &quot;Multiple files found matching search critiera of '$FileName'&quot;
                    
                    foreach ($match in $FileInfo)
                    {      
                        WriteText $match.FullName
                    }
                                    
                  throw &quot;Multiple instances of $FileName were found in $Path&quot;
                }
            
                $SourcePath = $FileInfo.FullName 
            }
    
            Copy-Item $SourcePath $FilesPath
            WriteText &quot;$($SourcePath) [was copied to ==&gt;] $FilesPath&quot;
        }
        else
        {
            WriteText &quot;$($DestinationPath) has already been identified, harvesting was skipped&quot;
        }
      }
    }
    
    # Copies file to the specified path from the $FilesPath
    function CopyFileToFolder([string]$Path) {
      begin {
        # Ensure that the folder exists
        $null = [System.IO.Directory]::CreateDirectory($Path)
      }
      process {
      # Get just the file name of the current context
        $FileName = [System.IO.Path]::GetFileName($_)
        $FileInfo = Get-ChildItem (Join-Path $FilesPath $FileName)
    
        if ($FileInfo -eq $null) {
          throw &quot;File $FileName was not found in $FilesPath&quot;
        }
    
        Copy-Item $FileInfo.FullName $Path
        WriteText &quot;$($FileInfo.FullName) [was copied to ==&gt;] $Path&quot;
      }
    }
    
    # Adds folder with SharePoint reference libraries to Registry to be found by MSBuild
    function AddSharePoint14ReferencePathToRegistry() {
      $RegistryValue = $SharePoint14ReferenceAssemblyPath + &quot;\&quot;
      if (TestWindows64) {
        $RegistryPath = &quot;HKLM:\SOFTWARE\Wow6432Node\Microsoft\.NETFramework\v2.0.50727\AssemblyFoldersEx\SharePoint14&quot;
      } else {
        $RegistryPath = &quot;HKLM:\SOFTWARE\Microsoft\.NETFramework\v2.0.50727\AssemblyFoldersEx\SharePoint14&quot;
      }
      # Do not set value if it is already exists and has the same value
      if (-Not (Test-Path $RegistryPath))
      {
          WriteText &quot;Writing new registry key '$RegistryPath' as '$RegistryValue'&quot;
        $null = New-Item -ItemType String $RegistryPath -Value $RegistryValue
      }
      else 
      {
          if (-Not ((Get-ItemProperty $RegistryPath).&quot;(default)&quot; -Eq $RegistryValue))
          {
              WriteText &quot;Updating registry key '$RegistryPath' to '$RegistryValue'&quot;
              Set-Item -Path $RegistryPath -Value $RegistryValue
          }
          else
          {
              WriteText &quot;Registry key '$RegistryPath' ignored as it already has the correct value of '$RegistryValue'&quot;
          }
      }
    }
    
    # Returns full path for the .Net 4.0 framework installation
    function GetDotNet40Directory() {
      # Find a folder in Microsoft.NET that starts with 'v4.0'
      $DirectoryInfo = Get-ChildItem (Join-Path $Env:SystemRoot &quot;Microsoft.NET\Framework&quot;) `
        | Where { $_ -is [System.IO.DirectoryInfo] } `
        | Where { $_.Name -like 'v4.0*' }
    
      if ($DirectoryInfo -eq $null) {
        throw &quot;.Net 4.0 is not found on this machine.&quot;
      }
    
      return $DirectoryInfo
    }
    
    # Returns version of installed .Net 4.0 framework
    function GetDotNet40Version() {
      return (GetDotNet40Directory).Name
    }
    
    # Returns $true if this is 64-bit version of Windows
    function TestWindows64() {
      return (Get-WmiObject Win32_OperatingSystem | Select OSArchitecture).OSArchitecture.StartsWith(&quot;64&quot;)
    }
    
    # Returns $true if machine has SharePoint 14 installed
    function TestSharePoint14() {
      return Test-Path &quot;HKLM:\SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\14.0&quot;
    }
    
    # Changes .Net framework version in gacutil.exe.config to match current .Net framework version.
    # It is required for gacutil.exe to work.
    function FixGacUtilConfigVersion() {
      $ConfigFilePath = Join-Path $FilesPath $GacUtilConfigFileName
      if (-not (Test-Path $ConfigFilePath)) {
        throw &quot;File $ConfigFilePath was not found&quot;
      }
    
      $DotNet40Version = GetDotNet40Version
      $ConfigDotNet40Version = GetGacUtilConfigVersion
      if ($DotNet40Version -ne $ConfigDotNet40Version) {
        # Create a backup file
        $ConfigBackupFilePath = $ConfigFilePath + &quot;.bak&quot;
        Copy-Item $ConfigFilePath $ConfigBackupFilePath
    
        # Make the file writable
        $ConfigFileInfo = Get-ChildItem $ConfigFilePath
        $ConfigFileInfo.IsReadOnly = $false
    
        # Read the file content, change the version and override the original file.
        # Need a variable for content to avoid simultaneous read-write of the same file.
        $ConfigFileContent = Get-Content $ConfigFilePath
        $null = $ConfigFileContent `
          | ForEach { $_ -replace '&quot;v4\.0.*?&quot;', &quot;&quot;&quot;$DotNet40Version&quot;&quot;&quot; } `
          | Out-File $ConfigFilePath -Encoding UTF8
        WriteText &quot;.Net version in file $ConfigFilePath changed from '$ConfigDotNet40Version' to '$(GetGacUtilConfigVersion)'&quot;
      }
    }
    
    # Returns version of .Net Framework stored in gacutil.exe.config
    function GetGacUtilConfigVersion() {
      $ConfigFilePath = Join-Path $FilesPath $GacUtilConfigFileName
      $ConfigVersionMatch = Get-Content $ConfigFilePath | Where { $_ -match '&quot;(v4\.0.*?)&quot;' } 
      if ($ConfigVersionMatch -eq $null) {
        throw &quot;No version info found in file $ConfigFilePath&quot;
      }
      return $Matches[1]
    }
    
    # Changes MSBuild .Net framework version in TfsBuildService.exe.config to 
    # match current .Net framework version.
    function FixTfsBuildServiceConfigMSBuildPath() {
      if (-not (Test-Path $TfsBuildServiceConfigFilePath)) {
        WriteText &quot;$TfsBuildServiceConfigFilePath was not found.&quot;
        WriteText &quot;This is needed only for TFS 2008.&quot;
        return
      }
    
      $DotNet40Directory = (GetDotNet40Directory).FullName
      $ConfigMSBuildPath = GetTfsBuildServiceConfigMSBuildPath
      if ($DotNet40Directory -ne $ConfigMSBuildPath) {
        # Create a backup file
        $TfsBuildServiceConfigBackupFilePath = $TfsBuildServiceConfigFilePath + &quot;.bak&quot;
        Copy-Item $TfsBuildServiceConfigFilePath $TfsBuildServiceConfigBackupFilePath
    
        # Make the file writable
        $ConfigFileInfo = Get-ChildItem $TfsBuildServiceConfigFilePath
        $ConfigFileInfo.IsReadOnly = $false
    
        # Read the file content, change the MSBuildPath and override the original file.
        # Need a variable for content to avoid simultaneous read-write of the same file.
        $ConfigFileContent = Get-Content $TfsBuildServiceConfigFilePath
        $null = $ConfigFileContent `
          | ForEach { $_ -replace 'key=&quot;MSBuildPath&quot;\s+value=&quot;.*?&quot;', &quot;key=&quot;&quot;MSBuildPath&quot;&quot; value=&quot;&quot;$DotNet40Directory&quot;&quot;&quot; } `
          | Out-File $TfsBuildServiceConfigFilePath -Encoding UTF8
        WriteText &quot;MSBuildPath in file $TfsBuildServiceConfigFilePath changed from '$ConfigMSBuildPath' to '$(GetTfsBuildServiceConfigMSBuildPath)'&quot;
    
        WriteText &quot;&quot;
    
        Restart-Service 'VSTFBUILD'
        WriteText &quot;'VSTFBUILD' service was restarted to apply changes in the $TfsBuildServiceConfigFileName file&quot;
      }
    }
    
    # Returns the .Net Framework path stored in TfsBuildService.exe.config
    function GetTfsBuildServiceConfigMSBuildPath() {
      $ConfigVersionMatch = Get-Content $TfsBuildServiceConfigFilePath `
        | Where { $_ -match 'key=&quot;MSBuildPath&quot;\s+value=&quot;(.*?)&quot;' } 
    
      if ($ConfigVersionMatch -eq $null) {
        throw &quot;No MSBuildPath key was found in file $TfsBuildServiceConfigFilePath&quot;
      }
      return $Matches[1]
    }
    
    #==============================================================================
    # Main script code
    #==============================================================================
    
    InitializeScriptVariables
    
    if ($Install) {
      InstallSharePointProjectFiles
    } elseif ($Collect) {
      CollectSharePointProjectFiles
    } else {
      WriteHelp
    }{% endhighlight %}

[0]: http://code.msdn.microsoft.com/SPPwrShllTeamBuild
[1]: http://blogs.msdn.com/b/sharepointdev/archive/2011/08/25/creating-your-first-tfs-build-process-for-sharepoint-projects.aspx
