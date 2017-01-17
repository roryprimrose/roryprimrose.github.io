---
title: Workaround for Azure SDK Invalid access to memory location error
categories: .Net
tags: Azure
date: 2012-03-19 13:38:12 +10:00
---

There is an issue with the Azure 1.6 SDK that I often hit when I run a cloud project. I get around 3-5 debug sessions with my cloud project before Visual Studio starts throwing an Invalid access to memory location error.![image][0]

This issue seems like it is hitting many [other people][1] as well. This is a significant pain point for working with Azure projects and there is currently no suitable workaround or fix that I can find. Sometimes shutting down the emulator and trying again works. Most often however, the IDE needs to be recycled. Having to do this each fifth F5 is a productivity killer.

<!--more-->

It seems that the most reliable information out there is that the issue is caused by addins/extensions in Visual Studio. This is consistent with my experience of running automated integration tests as defined in [my previous posts][2]. I quickly encounter this problem running F5 from the IDE, yet launching Azure and IISExpress for integration tests does not suffer from this error. This leads me to a reliable workaround for this issue based on my integration test implementation using the External Tools support in Visual Studio to manually launch the emulator. 

## Configuring External Tools

We need to add three External Tools entries. These are for launching the compute emulator in the Debug configuration, another one for the Release configuration and finally the storage emulator.![image][3]

Click Tools –&gt; External Tools![image][4]

First up is to configure launching the emulator for the debug configuration.

1. Click Add
1. Enter the title for running the compute under debug
1. Enter the command as _C:\Program Files\Windows Azure Emulator\emulator\csrun.exe_
1. Enter the arguments as /run:&quot;$(SolutionDir)**[YourCloudProjectName]**\csx\Debug&quot;;&quot;$(SolutionDir)**[YourCloudProjectName]**\bin\Debug\ServiceConfiguration.cscfg&quot; /launchBrowser
1. Setting the Initial directory is optional, but $(SolutionDir) is as good as any setting
1. Check Use Output Window and click Apply

Unfortunately the ExternalTools window does not process the $(ConfigurationName) macro like the build events function does. This is why we need an entry for each build configuration (typically Debug and Release).

Create a new External Tools entry for the compute emulator to load the Release build. It will have the same configuration except that references to Debug are replaced with Release.

Create a new External Tools entry for the storage emulator. The arguments for this field are /devstore:start![image][5]

The requirement for a separate entry for the storage emulator is unfortunate. It is required because the csrun.exe does not support command line parameters for both the compute emulator and the storage emulator in the one execution.

## Attaching the debugger

You can configure csrun to attach to the debugger using the /launchDebugger switch. I did not want to use this switch because it will open new instances of the debugger (typically devenv.exe) for each role instance (web and worker). This is unhelpful because I want to use the existing Visual Studio instance. I also found that the browser could not hit the compute instance anyway when the debugger was attached in this way (this could be just a timing issue though).

The easiest way around this is to just attach to the process manually.

Click Debug –&gt; Attach to Process…![image][6]

Find w3wp.exe and click Attach.![image][7]

## Notes

This workaround only spins up the compute emulator or the storage emulator. It does not do any of the following:

* Build/Rebuild your projects
* Request shutdowns of the emulators
* Attach a debugger

[0]: /files/image_135.png
[1]: http://social.msdn.microsoft.com/Forums/en-US/windowsazuredevelopment/thread/d152fe9c-5f44-4776-bf18-ede89d871904
[2]: /2012/03/18/integration-testing-with-azure-development-fabric-and-iisexpress/
[3]: /files/image_136.png
[4]: /files/image_137.png
[5]: /files/image_138.png
[6]: /files/image_139.png
[7]: /files/image_140.png
