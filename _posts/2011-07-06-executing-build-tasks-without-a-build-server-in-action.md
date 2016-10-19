---
title: Executing build tasks without a build server – In action
categories: .Net, My Software
tags: TFS, WiX
date: 2011-07-06 23:02:00 +10:00
---

The [previous post][0] provided the implementation of the BTE application. It described how the implementation of BTE is able to satisfy the [original design requirements][1] by providing an extensible task execution model.

The following are some examples of BTE running on the command line. I am using a batch file in order to shorten BuildTaskExecutor.exe down to bte.

<!--more-->

1. Executing BTE without arguments
[![image][3]][2]
1. Executing the help task
[![image][5]][4]
1. Executing the help task for a specific task name (or alias in this case)
[![image][7]][6]
1. Executing with verbose logging
[![image][9]][8]

The first version of BTE provides the following tasks built into the application (as defined by the help output):

```text
BuildTaskExecutor BinaryOutputVersion|bov
Renames the project output to include the product version of the project output.

BuildTaskExecutor Help|/?
Displays help information about the available tasks

BuildTaskExecutor IncrementAssemblyVersion|iav
Increments version number parts in AssemblyVersionAttribute entries in code files

BuildTaskExecutor SyncWixVersion|swv
Synchronizes the product version in a Wix project to the version of a binary file.

BuildTaskExecutor TfsCheckout|tfsedit
Checks out files from TFS based on a search pattern. The checkout will use default credentials resolved for the identified files.

BuildTaskExecutor WixOutputVersion|wov
Renames the Wix project output to include the wix product version.
```

The next post will provide a scenario where these tasks are used to manage the version of a solution without a full TFS environment.

[0]: /2011/07/03/Executing-build-tasks-without-a-build-server-%E2%80%93-Implementation/
[1]: /2011/07/01/Executing-build-tasks-without-a-build-server-%E2%80%93-Design/
[2]: /files/image%5B8%5D.png
[3]: /files/image%5B8%5D_thumb.png
[4]: /files/image%5B11%5D.png
[5]: /files/image%5B11%5D_thumb.png
[6]: /files/image%5B14%5D.png
[7]: /files/image%5B14%5D_thumb.png
[8]: /files/image%5B17%5D.png
[9]: /files/image%5B17%5D_thumb.png
