---
title: Filter environment by version in Release Management
categories: .Net
tags: 
date: 2018-09-10 12:30:00 +10:00
---

I've been using VSTS Release Management more and more recently. One of the issues I had (which took me far too long to solve) is how to restrict the automated trigger of a release into a Production environment based on the version (build quality). This actually turned out to be trivial to implement.

<!--more-->

## Scenario
My scenario is that I use GitVersion as part of my build process to determine what the version of the build should be based on the git history. Anything that is not a production release version will have a version suffix on it, for example ```1.2.3-hotfix-InvalidConfig-0001```. The production version of that software would then be ```1.2.3```.

My desired deployment workflow is that any build can go into the staging environment as soon as the build is successful for any build version however only production version builds can go into the production environment. 

This works in simple scenarios when I am working on my own projects. More complex software or when a larger team is involved might also have a development and testing environments before the staging environment. In that case I automatically deploy every build to the development environment and then allow testers to manually trigger a deploy to a test environment. Both strategies then end in a manual approval process to deploy into higher environments like staging and production.

## Background
Release Management uses triggers to initiate a deployment into any particular environment. For example, an after build trigger would automatically deploy a release into a staging environment after a build is successful. The trigger to the production environment would be for the release to occur after the staging environment. There is usually some kind of manual criteria that would determine when the release should go to the production environment.

The easiest solution to prevent an automated deployment to a production environment is to add an approval restriction. This means that someone needs to explicitly approve the deployment to production. This stops the release from automatically going to production directly after a successful deployment to staging but does not prevent the trigger for the production environment from being attempted. What then happens is that the approval requests for that environment keep piling up for each release. When a production worthy release is then available, each pending approval for prior versions need to be rejected before the desired production release can go into the agent queue.

Release Management also has artefact filters. These can be used to tell Release Management to not trigger a release for an environment if the artefact filter does not match. An artefact filter can be against a branch or build tag. You could, for example, only allow releases to trigger for the production environment where the branch name matches ```release/*```.

## Solution
The desired outcome is that a production release trigger does not execute if the build version should never go to production. The answer is to use an artefact filter against a build tag. The way this works is that the build workflow needs to add a tag to the build result which can then be defined as an artefact filter for the production environment.

I use a PowerShell step in my build that looks at the build number to see if it is in the format of ```[Major].[Minor].[Patch]```. This matches the production version number defined by GitVersion. The execution of GitVersion in the build workflow also sets the build number to the version it determines. The script will add a ```Production Ready``` tag to the build if the build number looks like a production version.

```powershell
# Determine if the build number is a production build version
if ($env:BUILD_BUILDNUMBER -match "^\d+\.\d+\.\d+$")
{
    Write-Host "This is a production ready build"

    # Set the build tag to be picked up by release management
    Write-Host "##vso[build.addbuildtag]Production Ready"
}
else
{
    Write-Host "This build is not ready for production"
}
```

The next step is to configure an artefact filter against the production environment and specify the ```Production Ready``` build tag.

The only other thing to consider is the reusability of the above PowerShell. I wrap that task up in a task group so it can be used on any build workflow.

Now every build will go to the staging environment, but the production environment will only trigger (with approval) for a production build version.