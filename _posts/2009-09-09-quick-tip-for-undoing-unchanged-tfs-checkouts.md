---
title: Quick tip for undoing unchanged TFS checkouts
categories : .Net
tags : TFS
date: 2009-09-09 10:13:00 +10:00
---

Reviewing changesets with a large number of files that haven’t changed can be a significant waste of time. Sliming changesets down to only changed files is a great idea before checking in files. 

I have previously been doing this manually which in itself is time consuming. Fortunately the TFS power toys package comes with a handy utility to automatically uncheck out any files that haven’t changed. Running TFPT.exe uu /r will do the trick. Running up an instance of the Visual Studio command prompt and entering in this command can also be a hassle. Thankfully Visual Studio makes this easy with the support for external tools.

To set this up, go to Tools –&gt; External Tools which will display the following dialog:![External Tools][0]

Click Add and enter the following settings (full Command value is C:\Program Files\Microsoft Team Foundation Server 2008 Power Tools\TFPT.exe)![image][1]

Sending the output to the Output Window makes this setup really easy to review the changes.

The Tools menu should now contain the Undo Null Changes command.![image][2]

Enjoy.

Update: Set initial directory to $(SolutionDir) as sometimes the workspace can’t be determined depending on the context of how the command is launched.

Update #2: Add /noget to the end of the arguments to prevent the local workspace being updated with the latest version before checking for null changes.

[0]: /files/image_3.png
[1]: /files/image_6.png
[2]: /files/image_5.png
