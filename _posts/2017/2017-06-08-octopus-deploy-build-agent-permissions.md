---
title: Octopus Deploy Build Agent Permissions
categories: .Net
tags: 
date: 2017-06-08 12:52:00 +11:00
---

I've been using Octopus Deploy for many years across many companies and projects. I am either connecting to it from a build agent via VSTS or on-premise TFS. From a security point of view we want to have the VSTS/TFS build agent having the least permissions required for it to work with Octopus Deploy. This post lists the permissions required to make this work.

<!--more-->

I use VSTS/TFS build agents to publish packages into the Octopus Deploy in-built package feed and create releases. Everything else is triggered from within Octopus Deploy regarding deployments.

My configuration is to create a new Role in Octopus Deploy called Build Agents. I then add a build service account to Octopus and generate an API key for it. This API key is then stored in VSTS/TFS for communicating with Octopus via a build.

The Build Agents role requires the following permissions

- BuiltInFeedPublish
- DeploymentCreate
- EnvironmentView
- FeedView
- LibraryVariableSetView
- LifecycleView
- MachineView
- ProcessView
- ProjectView
- ReleaseCreate
- ReleaseView
- VariableView
