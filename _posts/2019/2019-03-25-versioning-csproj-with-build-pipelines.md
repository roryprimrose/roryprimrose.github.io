---
title: Versioning C# projects in Azure DevOps build pipeline
categories: .Net
tags: 
date: 2019-03-25 19:27:00 +11:00
---

I'm a big fan of using GitVersion to calculate an application version based on git history. I'm also a big fan of the new csproj format because of its simplicity. GitVersion provides a build task for Azure DevOps build pipelines but unfortunately it does not yet support [setting project versions for the new csproj format][0]. This is my workaround until it is supported out of the box.

<!--more-->

The first step is to install an appropriate [GitVersion build task][1] from the Visual Studio Marketplace. Add this build task to your build workflow, followed by a PowerShell build step. This does mean that this will only work on Windows build agents. The next step is to add the following inline script to the PowerShell task.

```powershell
$nuGetVersion = $env:GitVersion_NuGetVersionV2
$sourcesDirectory = $env:BUILD_SOURCESDIRECTORY

Write-Host "Searching for projects under $($sourcesDirectory)"

# Find all the csproj files
if ($nuGetVersion -eq $null) {
    Write-Error ("GitVersion_NuGetVersionV2 environment variable is missing.")
    exit 1
}

if ($env:GitVersion_AssemblySemVer -eq $null) {
    Write-Error ("GitVersion_AssemblySemVer environment variable is missing.")
    exit 1
}

if ($env:GitVersion_MajorMinorPatch -eq $null) {
    Write-Error ("GitVersion_MajorMinorPatch environment variable is missing.")
    exit 1
}

if ($env:GitVersion_InformationalVersion -eq $null) {
    Write-Error ("GitVersion_InformationalVersion environment variable is missing.")
    exit 1
}

if ($sourcesDirectory -eq $null) {
    Write-Error ("BUILD_SOURCESDIRECTORY environment variable is missing.")
    exit 1
}

Function Set-NodeValue($rootNode, [string]$nodeName, [string]$value)
{   
    $nodePath = "PropertyGroup/$($nodeName)"
    
    $node = $rootNode.Node.SelectSingleNode($nodePath)

    if ($node -eq $null) {
        Write-Host "Adding $($nodeName) element to existing PropertyGroup"

        $group = $rootNode.Node.SelectSingleNode("PropertyGroup")
        $node = $group.OwnerDocument.CreateElement($nodeName)
        $group.AppendChild($node) | Out-Null
    }

    $node.InnerText = $value

    Write-Host "Set $($nodeName) to $($value)"
}

# This code snippet gets all the files in $Path that end in ".csproj" and any subdirectories.
Get-ChildItem -Path $sourcesDirectory -Filter "*.csproj" -Recurse -File | 
    ForEach-Object { 
        
        Write-Host "Found project at $($_.FullName)"

        $projectPath = $_.FullName
        $project = Select-Xml $projectPath -XPath "//Project"
        
        Set-NodeValue $project "Version" $nuGetVersion
        Set-NodeValue $project "AssemblyVersion" $env:GitVersion_AssemblySemVer
        Set-NodeValue $project "FileVersion" $env:GitVersion_MajorMinorPatch
        Set-NodeValue $project "InformationalVersion" $env:GitVersion_InformationalVersion 

        $document = $project.Node.OwnerDocument
        $document.PreserveWhitespace = $true

        $document.Save($projectPath)

        Write-Host ""
    }

Write-Host "##vso[build.updatebuildnumber]$($nuGetVersion)"
```

The build workflow will now identify all csproj files and enter the version information from GitVersion. After these two steps should come the ```dotnet build``` task.

I like to re-use these two steps together across many build definitions. You can select both tasks in the build workflow and create a task group which you can reuse. The disadvantage of this is that Azure DevOps identifies the versioning variables in the PowerShell and creates them as parameters to the build task group. The solution to cleaning this up is

- Export (download) the task group
- Delete the task group and remove it from the build workflow
- Edit the downloaded json file to remove the task group parameters
- Import the task group
  - Edit the task group name but **do not** select any of the tasks as this will add the parameters back
  - Save the task group
- Add the task group back to the build workflow, now without annoying parameters

[0]: https://github.com/GitTools/GitVersion/issues/1611
[1]: https://marketplace.visualstudio.com/publishers/gittools