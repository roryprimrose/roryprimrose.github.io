---
title: VS2013 won’t start debugging
tags : Azure
date: 2013-10-31 12:53:37 +10:00
---

I hit this one a couple of days ago and it had me scratching my head for a while.![The debugger cannot continue running the process. Unable to start debugging.][0]

  
I thought it was an issue with the tooling, perhaps something I uninstalled or installed. I had installed VS2013 with Azure SDK 2.1, then updated with 2.2 when it came out but I had also uninstalled some packages related to VS2010 which I have used for years.

Turns out that this error presents itself when the solution doesn’t have something to debug. The message is a little misleading though. 

My solution starts multiple projects on F5. One project is an Azure cloud project with web and worker roles (debugger attached) while the other is a local STS website (no debugger attached), all of which run in IIS Express. This error popped up when there were either no projects set to run or when the STS project was set to launch without the debugger and the cloud project was set to None for the multiple project start. Either of these cases causes VS not to debug because there is nothing that is configured for it to attach too.

[0]: //blogfiles/image_158.png
