---
title: WCF services hosted in IIS
categories : .Net, IT Related
tags : WCF
date: 2006-10-09 22:02:01 +10:00
---

Today I have been playing with a WCF service hosted in IIS. Previously, I have always used the self-hosted services running from a console application.

I am having issues with IIS hosting because although the service code is running correctly, the client is throwing a response error because the connection with the server is being unexpectedly closed. Still haven't figured this one out.

Tonight, I have been doing a little bit of reading about this topic and came across this interesting note from the [Hosting Services page][0] in the SDK.

> _The message-based activation provided for an WCF service by IIS 5.1 on Windows XP blocks any other self-hosted WCF service on the same box from using port 80 to communicate._

The Hosting Services page is a great reference for helping you to figure out the best way of hosting your WCF service. Windows Activation Service seems like the most comprehensive hosting solution, but obviously requires Vista as the platform.

[0]: http://windowssdk.msdn.microsoft.com/en-us/library/ms730158.aspx
