---
title: TFVC Build Versioning
categories: .Net
tags: 
date: 2017-06-08 13:02:00 +11:00
---

These days I'm typically using Git for source control. Some of my consulting engagements take me back into the world of Team Foundation Version Control (TFVC). I have used several techniques over the years for getting build versioning working in an automated build against TFVC. These days the easiest solution is a little powershell.

<!--more-->

This script will take in the BuildID from TFS Build and the path to the AssemblyInfo.cs. It will determine the current version from the version attributes and append the BuildID to create a new unique version for the build. It will also update the build number to the same value to make it easy to track the builds.

```powershell
Param(
  [Parameter(Mandatory=$true)][string]$buildId,
  [Parameter(Mandatory=$true)][string]$assemblyInfoPath
)

function Get-Version([string]$path)
{    
    if (!(Test-Path $assemblyInfoPath))
    {
        throw [System.IO.FileNotFoundException] "'$path' was not found."
    }

    $assemblyInfo = Get-Content -Path $path

    if (!$assemblyInfo)
    {
        throw [System.InvalidOperationException] "The file '$path' does not have any contents."
    }                

    # Attempt to get the version information from the AssemblyVersionAttribute first
    $versionMatches = [regex]::Match($assemblyInfo, "\[assembly\:\s*AssemblyVersion(Attribute)?\(`"(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(\.\d+)?`"\)\]")

    if ($versionMatches.Success)
    {
        # Parse out the AssemblyVersionAttribute value
        $major = $versionMatches.Groups["Major"]
        $minor = $versionMatches.Groups["Minor"]
        $patch = $versionMatches.Groups["Patch"]

        return "$major.$minor.$patch" 
    }
    
    # AssemblyVersionAttribute wasn't available, try for the AssemblyFileVersionAttribute
    $versionFileMatches = [regex]::Match($assemblyInfo, "\[assembly\:\s*AssemblyFileVersion(Attribute)?\(`"(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(\.\d+)?`"\)\]")
    
    if ($versionFileMatches.Success)
    {
        # Parse out the AssemblyVersionAttribute value
        $major = $versionFileMatches.Groups["Major"]
        $minor = $versionFileMatches.Groups["Minor"]
        $patch = $versionFileMatches.Groups["Patch"]

        return "$major.$minor.$patch" 
    }
    
    throw [System.InvalidOperationException] "Unable to load version information from '$path'."
}

function Get-FileEncoding
{
    [CmdletBinding()] Param (
     [Parameter(Mandatory = $True, ValueFromPipelineByPropertyName = $True)] [string]$Path
    )
 
    [byte[]]$byte = get-content -Encoding byte -ReadCount 4 -TotalCount 4 -Path $Path
    #Write-Host Bytes: $byte[0] $byte[1] $byte[2] $byte[3]

    # EF BB BF (UTF8)
    if ( $byte[0] -eq 0xef -and $byte[1] -eq 0xbb -and $byte[2] -eq 0xbf )
    { 
        return 'UTF8' 
    }

    # 00 00 FE FF (UTF32 Big-Endian)
    elseif ($byte[0] -eq 0 -and $byte[1] -eq 0 -and $byte[2] -eq 0xfe -and $byte[3] -eq 0xff)
    { 
        return 'BigEndianUTF32'
    }

    # 2B 2F 76 (38 | 38 | 2B | 2F)
    elseif ($byte[0] -eq 0x2b -and $byte[1] -eq 0x2f -and $byte[2] -eq 0x76 -and ($byte[3] -eq 0x38 -or $byte[3] -eq 0x39 -or $byte[3] -eq 0x2b -or $byte[3] -eq 0x2f) )
    { 
        return 'UTF7'
    }

    return 'ASCII'
}

function Set-Version([string]$path, [string]$newVersion)
{    
    if (!(Test-Path $assemblyInfoPath))
    {
        throw [System.IO.FileNotFoundException] "'$path' was not found."
    }

    $encoding = Get-FileEncoding $path
    $assemblyInfo = Get-Content -Path $path -Encoding $encoding | Out-String
    
    if (!$assemblyInfo)
    {
        throw [System.InvalidOperationException] "The file '$path' does not have any contents."
    }                
    
    $versionExpression = "\[assembly\:\s*AssemblyVersion(Attribute)?\(`"\d+(\.\d+){2,3}`"\)\]"
    $versionFileExpression = "\[assembly\:\s*AssemblyFileVersion(Attribute)?\(`"\d+(\.\d+){2,3}`"\)\]"
    
    $assemblyInfo = [regex]::Replace($assemblyInfo, $versionExpression, "[assembly: AssemblyVersion(`"$newVersion`")]")  
    $assemblyInfo = [regex]::Replace($assemblyInfo, $versionFileExpression, "[assembly: AssemblyFileVersion(`"$newVersion`")]")
    
    Set-Content $path $assemblyInfo -Encoding $encoding
}

Write-Host "Assigning version with BuildId $buildId"

$existingVersion = Get-Version $assemblyInfoPath

Write-Host "Found version from '$assemblyInfoPath' as $existingVersion"

$newVersion = "$existingVersion.$buildId"

Write-Host "Assigning new version as $newVersion"
Set-Version $assemblyInfoPath $newVersion

Write-Host "Setting build number to $newVersion."
Write-Host "##vso[build.updatebuildnumber]$newVersion"
```