---
title: Creating Sentry releases from VSTS Release Management
categories: .Net
tags: 
date: 2018-05-18 13:54:00 +10:00
---

[Sentry.io][0] is a wonderful platform for capturing, alerting and reporting on application exceptions. One of its features is to track releases of your software. This is handy so that you can not only associate an application exception with the version of the software it occurred in, but you can also indicate which version of the software fixes the issue. This post demonstrates how to create a Sentry release when using VSTS Release Management (or Builds).

<!--more-->

The set up here uses a custom task in VSTS for running inline PowerShell scripts as part of build and release workflows. The custom task is by Peter Groenewegen and found on the [Visual Studio Marketplace][1]. The reason for using this task is because the out of the box PowerShell tasks provided by Microsoft only allow for executing a PowerShell file under source control rather than an inline script.

The PowerShell script for creating a Sentry release must take in some variables in order for the process to be re-used across projects. The script needs the Sentry API Key, organisation name, project name and the release number. The script uses these variables to make the appropriate HTTP requests out to the Sentry API.

```powershell
Param(
    [string]$sentryApiKey = "$(Sentry.ApiKey)",
    [string]$organisation = "$(Sentry.Organisation)",
    [string]$project = "$(Sentry.Project)",
    [string]$releaseNumber = "$(Build.BuildNumber)",
    [boolean]$resolveIssues = $(Sentry.ResolveIssues)
)

Write-Host "Creating Sentry release $releaseNumber for $organisation $project"

$url = "https://sentry.io/api/0/projects/$organisation/$project/releases/"

$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Authorization", "Bearer $sentryApiKey")

$body = @{ "version" = $releaseNumber }

$body = ConvertTo-Json $body

Try
{
    $response = Invoke-RestMethod -Method Post -Uri "$url" -Body $body -Headers $headers -ContentType "application/json"
    Write-Host $response
}
Catch [System.Net.WebException] 
{
    Write-Host $_
    if($_.Exception.Response.StatusCode.Value__ -ne 400)
    {
        Throw
    }
}

if ($resolveIssues)
{
    $resolveBody = '{"status":"resolved"}'
    $url = "https://sentry.io/api/0/projects/$organisation/$project/groups/"

    $response = Invoke-RestMethod -Method Put -Uri "$url" -Body $resolveBody -Headers $headers -ContentType "application/json"
    Write-Host $response
}
```
The easiest way to make this script reusable across builds and releases is to create a Task Group from the task when it is first added to a build or release workflow. I then defined the Task Group parameters as the following:

- Sentry.ApiKey - (no default) - The Sentry API Key used to create the release
- Sentry.Organisation - (no default) - The Sentry organisation related to the release
- Sentry.Project - (no default) - The Sentry project related to the release
- Sentry.ResolveIssues - $False - Resolves all outstanding issues

No parameter is required for the release number because this is taken from the ```$(Build.BuildNumber)``` variable which is automatically handled by VSTS.

I then configure this custom task group only against deployments to my production environments to avoid pushing a lot of unnecessary releases to my Sentry projects.

[0]: https://sentry.io/
[1]: https://marketplace.visualstudio.com/items?itemName=petergroenewegen.PeterGroenewegen-Xpirit-Vsts-Build-InlinePowershell