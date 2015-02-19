---
title: How TFS stores workspace information
categories : .Net, Applications
tags : TFS
date: 2007-04-18 08:46:24 +10:00
---

Given the recent problems on CodePlex, I have had to change the TFS server that one of my projects is hosted on. I found this to be a problem because I wanted to use the same directory for the source on my local drive. Even after removing the source control bindings and removing the TFS server settings, Visual Studio still complained that the directory is configured for the old server address.

TFS stores workspace directory mapping information in a _VersionControl.config_ file located in _C:\Documents and Settings\[YOUR PROFILE]\Local Settings\Application Data\Microsoft\Team Foundation\1.0\Cache\_. The old server isn't removed from this configuration when you remove the server from TFS in Visual Studio. This bit, you have to do yourself.

