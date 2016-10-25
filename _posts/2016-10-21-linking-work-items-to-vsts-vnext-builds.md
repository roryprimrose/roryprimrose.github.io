---
title: Linking work items to VSTS vNext builds
categories: .Net
tags: 
date: 2016-10-25 17:25:00 +10:00
---
One of the benefits of work items in VSTS/TFS is the ability to put metadata into your project system. Associating work items with automated builds is a good example of this. The XAML build system in VSTS/TFS has a handy feature of linking the build number against associated work items as a part of the build process. Unfortunately, there is no out of the box implementation to achieve this in build vNext.

The XAML build workflow stores build numbers in the `Microsoft.VSTS.Build.IntegrationBuild` field to link a work item to the build. The IntegrationBuild field is only visible by default in the Bug work item template however the field still exists for PBI and Task work items. You can  modify the PBI and Task work item templates however to make this field visible.

We can add the functionality for linking work items to builds by executing some PowerShell during a build that will populate the IntegrationBuild field. As this will be a custom script we can also add some additional functionality.

<!--more-->

**Note:** The design of this PowerShell script revolves around Git as the source control mechanism and using [GitVersion][0] for version identification. Most of the code however can be used against TFVC repositories.

Before we get to the PowerShell script, it is helpful to understand some of the background concepts.

## Versioning

A key outcome of software delivery should be to have a link between source control, the build system, a compiled binary, the project system (work items) and the deployment system. There should be a single, unique and consistent build/version number that links all these together for a build. 

GitVersion drives this by coming up with all the version information for a build. Updating the GitVersion configuration to use the ContinuousDeployment mode provides unique NuGet safe version numbers across our branches based on the branch name and Git commit history.

The following GitVersion.yml file in the repository root defines this configuration.

```
mode: ContinuousDeployment
branches: {}
ignore:
  sha: []
```

One of the values GitVersion provides when compiling the binary is `NuGetVersionV2`. This value will be the identifier to link all the system information together for a build. Setting the build number in the build definition to `$(GitVersion_NuGetVersionV2)` gives the build our consistent version number.

![Edit build][1]

GitVersion code generates all the version information into the binary. I use [Octopus Deploy][4] as the deployment system and the build workflow [creates deployable packages][2] and Octopus release configurations using the same NuGetVersionV2 version number. 

So far this configuration has linked source control, the build system, a compiled binary and the deployment system together. The missing piece is linking the build to the project system (work items).

## The requirements

The following are the requirements for getting this to work:

* Update the linked work items with the current build when
  * the IntegrationBuild field is empty; or
  * the build number represents a more mature version number
* Update the parent work items
  * Update PBI and Bug parents only
  * Apply same build number rules as above
* Update the [release version field][3] if supported

Why not just update a work item on every build? There a few reasons to avoid this. 

1. Work item associations with builds using VSTS/TFS and Git seems to be very eager. It tends to link to work items far beyond what seems necessary when looking at the Git history.
2. In the future, a work item could be linked to a build when it is already linked to a previous release build. We don't want to lose the knowledge that this work was completed against a prior release. For example, we don't want to replace `1.2.2` with `1.2.3-beta0001`. This can be caused either by human error, or more likely, #1 above.
3. We don't want team members to be spammed with work item change alert emails. Each change to a work item linked to a build could send an email alert. 

The last point identifies that these requirements can end up adding an additional feature. A developer could receive an email alert when one of their tasks progresses to another branch resulting in a more mature build version for their changes.

## Branching strategy

Several of these requirements are straight forward and mimic the XAML build logic. One big enhancement is to provide some intelligence around updating the work item build association when the build identifies a more mature version number. This refers to being able to tell that code is heading towards a release by watching changes to the version numbers from GitVersion. 

Combining our branching strategy and GitVersion allows us to determine what kind of branch the code is built from. We can do this by looking at the NuGet pre-release tag in the version number. 

The following is an overview of how we are using branching and the kind of version numbers that GitVersion determines.

| Branch | From | Version Format |
| --- | --- | --- |
| master | - | 1.2.3-ci0001 |
| develop | master | 1.2.3-unstable0001 |
| feature-PBI987-AddUserEmail | develop | 1.2.3-pbi987-adduserem0001 |
| release-1.2 | master | 1.2.3-beta0001 (no version tag)<br />1.2.3 (with v1.2.3 Git tag) |
| hotfix-Bug321-PrivacyLink | release-* | 1.2.4-beta0002 |

## Branch priorities

Consider a PBI that contains several commits. Those commits go through the following branches:

* feature
* develop (using pull request with squash merge)
* master
* release (without a version tag)
* release (with v1.2.3 version tag)

The alternative flow is a production hotfix for a release branch. In this scenario, the flow of branches is:

* hotfix
* release (using pull request with squash merge, no version tag)
* release (with v1.2.3 version tag)

There is a priority to apply to the branches to identify the flow of changes between them. We can use that priority to calculate the maturity of the build/version number.

| Branch | Pre-release tag format | Priority |
| --- | --- | --- |
| master | ci* | 0 |
| feature | [branchname]* | 0 |
| develop | unstable* | 1 |
| hotfix | beta* | 1 |
| release (without tag) | beta* | 1 |
| release (with version tag) |  | 2 |

Per the above requirements, we want to update the work item build association each time a build occurs for the first time against a branch that results in a higher branch priority compared to what is stored against the work item.

### Example

Multiple builds against a feature branch would result in build numbers like `1.2.3-pbi987-adduserem0001`, then `1.2.3-pbi987-adduserem0002`. The work items for the build should be initial set as `1.2.3-pbi987-adduserem0001` and then not change for subsequent builds against the feature branch.

Those changes go through a pull request process and end up as a squashed merge down on the develop branch. At this point the version for the build will be something like `1.2.3-unstable0001`. We want to update the associated work items with this new build number as the develop branch has a higher priority than the feature branch. We consider that the develop branch build number is more mature than the feature branch build number.

The changes will end up going over to a release branch. When this code is stable and ready for release, we tag the commit in Git with the final version number. The build associated with that tag will have a build number of `1.2.3` which will then be applied to the work item. As this has the highest branch priority, it will never be updated again.

## PowerShell

We now have all the requirements and knowledge in place to figure out how to associate work items with the build. 

The following is the full PowerShell script to satisfy the above requirements. There are a lot of support functions in the script but it is the `Set-BuildVersionInfo` function at the bottom that drives the workflow to match the requirements.

```powershell
[string] $CollectionUri = "$env:SYSTEM_TEAMFOUNDATIONCOLLECTIONURI"
[string] $project = "$env:SYSTEM_TEAMPROJECT"
[string] $BuildId = "$env:BUILD_BUILDID"
[string] $BuildNumber = "$env:BUILD_BUILDNUMBER"
[string] $AuthType = "Bearer"
[string] $ReleaseVersionField = "Auspex.ReleaseVersion"

Add-Type -TypeDefinition @"
   public enum VersionType
   {
      Unknown = 0,
      Alpha,
      Beta,
      Release
   }
"@

function Get-VersionType([string] $version)
{
    if ([string]::IsNullOrEmpty($version))
    {
        return [VersionType]::Unknown
    }

    $releaseExpression = "^\d+(\.\d+){2}$"

    if ($version -match $releaseExpression)
    {
        return [VersionType]::Release
    }

    $betaExpression = "^\d+(\.\d+){2}-(unstable|beta).+$"
     
    if ($version -match $betaExpression)
    {
        return [VersionType]::Beta
    }

    $alphaExpression = "^\d+(\.\d+){2}-.+$"
     
    if ($version -match $alphaExpression)
    {
        return [VersionType]::Alpha
    }
     
    return [VersionType]::Unknown
}

function Get-Headers
{
    $authorization = "$AuthType $env:SYSTEM_ACCESSTOKEN"
    
    return @{
        Authorization = $authorization
    }
}

function Get-BuildWorkItemIds([string] $collectionUri, [string] $projectName, [string] $buildId)
{
    $headers = Get-Headers
    $restUri = $collectionUri + $projectName + "/_apis/build/builds/" + $buildId + "/workitems?api-version=2.0"
    [string[]] $buildWorkItemIds = @()
 
    $response = Invoke-RestMethod -Uri $restUri -ContentType "application/json" -headers $headers -Method GET
 
    Write-Debug (ConvertTo-Json $response -Depth 100)
 
    $itemCount = $response.count
    
    for($index = 0; $index -lt $itemCount ; $index++)
    {
        $buildWorkItem = $response.value[$index]
        $workItemId = $buildWorkItem.id

        Write-Verbose "Found work item $workItemId linked to build $buildId"

        $buildWorkItemIds += $workItemId
    }

    return $buildWorkItemIds
}

function Get-BuildWorkItems([string] $collectionUri, [string] $projectName, [string] $buildId)
{
    $buildWorkItemIds = @(Get-BuildWorkItemIds $collectionUri $projectName $buildId)
    
    if ($buildWorkItemIds.Length -eq 0)
    {
        return @()
    }

    return Get-WorkItems $collectionUri $buildWorkItemIds
}

function Get-WorkItems([string] $collectionUri, [string[]] $workItemIds)
{
    $headers = Get-Headers
    $ids = ($workItemIds -join ",")
    $restUri = $collectionUri + "_apis/wit/workitems?ids=" + $ids + "&api-version=1.0&`$expand=relations"

    $response = Invoke-RestMethod -Uri $restUri -ContentType "application/json" -headers $headers -Method GET
 
    Write-Debug (ConvertTo-Json $response -Depth 100)

    if ($response.value -eq $null)
    {
        return @()
    }

    return $response.value;
}

function Get-WorkItem([string] $collectionUri, [string] $workItemId)
{
    Write-Debug "Getting work item $workItemId"

    $headers = Get-Headers
    $restUri = $collectionUri + "_apis/wit/workitems/" + $workItemId + "?api-version=1.0&`$expand=relations"

    $response = Invoke-RestMethod -Uri $restUri -ContentType "application/json" -headers $headers -Method GET
 
    Write-Debug (ConvertTo-Json $response -Depth 100)

    return $response;
}

function Get-ParentWorkItemId([object] $workItem)
{
    if ($workItem -eq $null)
    {
        return $null;
    }
    
    if ($workItem.relations -eq $null)
    {
        Write-Debug "$($workItem.fields."System.WorkItemType") #$($workItem.id) does not have any relationships"

        return $null;
    }
    
    $relation = $workItem.relations | Where-Object {$_.rel -eq "System.LinkTypes.Hierarchy-Reverse" }

    if ($relation -eq $null)
    {
        Write-Debug "$($workItem.fields."System.WorkItemType") #$($workItem.id) does not have a parent work item"

        return $null
    }

    $workItemUri = $relation.url
    
    $found = $workItemUri -match "\d+$"
    
    if ($found) 
    {
        $parentWorkItemId = $matches[0]

        Write-Verbose "Found work item $parentWorkItemId as a parent of $($workItem.fields."System.WorkItemType") #$($workItem.id)"
        
        return $parentWorkItemId
    }

    Write-Debug "No parent work item found for $($workItem.fields."System.WorkItemType") #$($workItem.id)"

    return $null
}

function Get-ParentWorkItem([string] $collectionUri, [object] $workItem)
{
    $parentWorkItemId = Get-ParentWorkItemId $workItem

    if ($parentWorkItemId -eq $null)
    {
        return $null
    }

    return Get-WorkItem $collectionUri $parentWorkItemId
}

function Get-WorkItemFound($workItems, [string] $workItemId)
{
    foreach ($workItem in $workItems)
    {
        if ($workItem.id -eq $workItemId)
        {
            return $true
        }
    }

    return $false
}

function Get-IsSupportedParent($workItem)
{
    if ($workItem -eq $null)
    {
        return $false
    }

    $workItemType = $workItem.fields."System.WorkItemType"

    Write-Debug "Work item type is $workItemType"

    if ($workItemType -eq "Product Backlog Item")
    {
        return $true
    }
    
    if ($workItemType -eq "Bug")
    {
        return $true
    }

    return $false
}

function Get-RelatedWorkItems([string] $collectionUri, [string] $projectName, [string] $buildId)
{
    $workItems = @(Get-BuildWorkItems $collectionUri $projectName $buildId)

    Write-Verbose "Found $($workItems.length) work items directly related to build $buildId"

    foreach ($workItem in $workItems)
    {
        $parentWorkItemId = Get-ParentWorkItemId $workItem

        if ($parentWorkItemId -eq $null)
        {
            continue
        }

        if (Get-WorkItemFound $workItems $parentWorkItemId -eq $true)
        {
            Write-Debug "Skipping $parentWorkItemId because it has already been included"

            continue
        }

        $parentWorkItem = Get-WorkItem $collectionUri $parentWorkItemId
        $isParentWorkItemSupported = Get-IsSupportedParent $parentWorkItem

        if ($isParentWorkItemSupported -eq $false)
        {
            Write-Debug "Skipping $($parentWorkItem.fields."System.WorkItemType") #$($parentWorkItem.id) because it is not a PBI or Bug"

            continue
        }

        Write-Verbose "Adding $($parentWorkItem.fields."System.WorkItemType") #$($parentWorkItem.id) to the related work items to process"
    
        $workItems += $parentWorkItem
    }
    
    if ($workItems.length -gt 0)
    {
        Write-Host "Found the following work items related to build $buildId"

        foreach ($workItem in $workItems)
        {
            Write-Host "`t$($workItem.fields."System.WorkItemType") #$($workItem.id)"
        }
    }

    return $workItems
}

function Set-FieldOperation([string[]] $operations, [string] $buildNumber, [PSObject] $workItem, [string] $fieldName, [boolean] $setReleaseTypeOnly)
{
    if ([string]::IsNullOrEmpty($fieldName))
    {
        return $operations
    }

    $fieldValue = $workItem.fields.$fieldName
    $fieldVersion = Get-VersionType $fieldValue
    $buildVersion = Get-VersionType $buildNumber
    
    if ($fieldVersion -ge $buildVersion)
    {
        Write-Verbose "Skipping $($workItem.fields."System.WorkItemType") #$($workItem.id) $fieldName as it is currently ($fieldVersion) $fieldValue"

        return $operations
    }
        
    if ($setReleaseTypeOnly -eq $true -and $buildVersion -ne [VersionType]::Release)
    {
        Write-Verbose "Skipping $($workItem.fields."System.WorkItemType") #$($workItem.id) $fieldName as it is currently ($fieldVersion) $fieldValue and the field only supports Release versions"

        return $operations
    }

    $operations += "{`"op`": `"add`", `"path`": `"/fields/$fieldName`", `"value`": `"$buildNumber`"}"
    Write-Host "Setting $($workItem.fields."System.WorkItemType") #$($workItem.id) $fieldName from ($fieldVersion) $fieldValue to ($buildVersion) $buildNumber"
        
    return $operations
}

function Set-BuildVersionInfo
{
    $buildVersion = Get-VersionType $BuildNumber

    Write-Host "Build version is ($buildVersion) $BuildNumber"

    $headers = Get-Headers
    $buildWorkItems = Get-RelatedWorkItems $CollectionUri $project $BuildId
    $integratedInField = "Microsoft.VSTS.Build.IntegrationBuild"

    foreach ($workItem in $buildWorkItems)
    {
        [string[]] $operations = @()

        $operations = Set-FieldOperation $operations $BuildNumber $workItem $integratedInField $false
        
        $isValidParentWorkItem = Get-IsSupportedParent $workItem

        if ($isValidParentWorkItem -eq $true)
        {
            # We will only set the release version field for PBI and Bug work item types
            $operations = Set-FieldOperation $operations $BuildNumber $workItem $ReleaseVersionField $true
        }
        
        if ($operations.Length -eq 0)
        {
            Write-Host "No changes are being made to $($workItem.fields."System.WorkItemType") #$($workItem.id)"

            continue
        }

        $body = "[" + ($operations -join ",") + "]"
        $restUri = $workItem.url + "?api-version=1.0"
        
        Write-Debug "Updating $($workItem.fields."System.WorkItemType") #$($workItem.id) [$restUri] with the following operations: $body"
        
        try
        {
            $response = Invoke-RestMethod -Uri $restUri -Body $body -ContentType "application/json-patch+json" -headers $headers -Method Patch
            
            Write-Debug (ConvertTo-Json $response -Depth 100)
        }
        catch
        {    
            Write-Error "StatusCode: $_.Exception.Response.StatusCode.value__"
            Write-Error "StatusDescription: $_.Exception.Response.StatusDescription"
            Write-Error $_
        }
    }
}

#________________________________________
#
#      Run the script
#________________________________________

#$VerbosePreference = "Continue"
#$DebugPreference = "SilentlyContinue"

Set-BuildVersionInfo
```

## Build configuration

There are a couple of ways you can execute this script in a build. The script can be placed in a ps1 file and put under source control. You could then use the PowerShell build step to execute the ps1.

Using multiple repositories makes this messy as the ps1 would be spread around several repositories. The alternative is to use the [Inline PowerShell][5] extension that is available on the VSTS marketplace. This is a little easier to manage. As for source control, this is provided by the build vNext configuration out of the box.

This script relies on access to the OAuth token so that it can make Bearer calls out to the VSTS API. You will need to edit your build configuration to allow this token to be made available to the build process.

![Allow oauth][6]

Once the script is in place and the OAuth token is available, work items will start getting updated and linked to builds. Your build logs will also contain some information from the script about the changes that were applied.

```
2016-10-21T05:59:36.0849204Z ##[command]& 'C:\Windows\ServiceProfiles\NetworkService\AppData\Local\Temp\tmpEB3B.ps1' 
2016-10-21T05:59:36.2568355Z Build version is (Beta) 1.2.0-unstable0005
2016-10-21T05:59:37.8506902Z Found the following work items related to build 374
2016-10-21T05:59:37.8506902Z 	Task #5472
2016-10-21T05:59:37.8506902Z 	Product Backlog Item #4812
2016-10-21T05:59:37.8976813Z Setting Task #5472 Microsoft.VSTS.Build.IntegrationBuild from (Alpha) 1.2.0-pbi4812-emailver0009 to (Beta) 1.2.0-unstable0005
2016-10-21T05:59:38.4757320Z Setting Product Backlog Item #4812 Microsoft.VSTS.Build.IntegrationBuild from (Alpha) 1.2.0-pbi4812-emailver0009 to (Beta) 1.2.0-unstable0005
```

[0]: https://github.com/GitTools/GitVersion/
[1]: /files/2016/10/2016-10-24 11_38_25-Edit-Build.png
[2]: /2015/04/04/bridging-gitversion-and-octopack/
[3]: /2016/10/19/generating-release-notes-in-vsts/
[4]: https://octopus.com
[5]: https://marketplace.visualstudio.com/items?itemName=petergroenewegen.PeterGroenewegen-Xpirit-Vsts-Build-InlinePowershell
[6]: /files/2016/10/2016-10-24 13_21_40-Allow-oauth.png