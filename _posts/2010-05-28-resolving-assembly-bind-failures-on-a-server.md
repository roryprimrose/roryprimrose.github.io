---
title: Resolving assembly bind failures on a server
categories: .Net
tags: ASP.Net, Sync Framework, WCF
date: 2010-05-28 11:59:00 +10:00
---

Sometimes things just don’t go to plan went you deploy your software to a server. Unfortunately servers don’t come with all your dev tooling so it can be hard to figure out what is going wrong when provided some cryptic exception message.

My current issue is testing deployment of a 4.0 framework built WCF service to a newly provisioned Windows Server 2008 R2 machine. I get the classic WCF error message saying that the service type could not be found for the endpoint. It doesn’t tell me why and I know the service assemblies are in place. Given prior experience I am guessing that the most likely cause is that other dependencies of my service assemblies are not on the machine.

I look to FUSLOGVW.exe to find out these answers. The issue is that this great utility is not on the server image as it comes out of the .Net SDK. With a bit of trickery, it can be used on a server. These are the steps to get this done.

<!--more-->

1. Copy FUSLOGVW.exe from your dev machine to the server (found somewhere around C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin)
2. Run cmd.exe with elevated privileges to execute _*reg add HKLM\Software\Microsoft\Fusion /v EnableLog /t REG_DWORD /d 1*_
  (Kudos to [Bart][0] for this one)
3. Create a logs directory (I tend to use C:\Temp\Logs)
4. Run FUSLOGVW.exe with elevated privileges
5. Click Settings
* Select Log bind failures to disk
* Select Enable custom log path
* Enter the path defined in #3.
    **NOTE: Do not include the trailing \**
    **NOTE: The directory must be created first**
* Click OK
![image][1]
7. Run your application (refresh the browser on the service endpoint in my case)

Hopefully you will now have some information to work with.

![image][2]

Double clicking on the binding failure gives you all the information about what .Net did to try to find the assembly. The entry in my scenario looks like this:

```text
*** Assembly Binder Log Entry  (28/05/2010 @ 11:35:47 AM) ***
The operation failed.
Bind result: hr = 0x80070002. The system cannot find the file specified.

Assembly manager loaded from:  C:\Windows\Microsoft.NET\Framework64\v4.0.30319\clr.dll
Running under executable  c:\windows\system32\inetsrv\w3wp.exe
--- A detailed error log follows.
=== Pre-bind state information ===
LOG: User = NT AUTHORITY\NETWORK SERVICE
LOG: DisplayName = Microsoft.Synchronization, Version=2.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
 (Fully-specified)
LOG: Appbase = file:///C:/Program Files (x86)/MyService.Service/
LOG: Initial PrivatePath = C:\Program Files (x86)\MyService.Service\bin
LOG: Dynamic Base = C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files\root\c1449ba8
LOG: Cache Base = C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files\root\c1449ba8
LOG: AppName = abf9a5ae
Calling assembly : MyService.Synchronization.ServiceContracts, Version=1.0.0.0, Culture=neutral, PublicKeyToken=08670063761b63a0.
===
LOG: This bind starts in default load context.
LOG: Using application configuration file: C:\Program Files (x86)\MyService.Service\web.config
LOG: Using host configuration file: C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet.config
LOG: Using machine configuration file from C:\Windows\Microsoft.NET\Framework64\v4.0.30319\config\machine.config.
LOG: Post-policy reference: Microsoft.Synchronization, Version=2.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
LOG: GAC Lookup was unsuccessful.
LOG: Attempting download of new URL file:///C:/Windows/Microsoft.NET/Framework64/v4.0.30319/Temporary ASP.NET Files/root/c1449ba8/abf9a5ae/Microsoft.Synchronization.DLL.
LOG: Attempting download of new URL file:///C:/Windows/Microsoft.NET/Framework64/v4.0.30319/Temporary ASP.NET Files/root/c1449ba8/abf9a5ae/Microsoft.Synchronization/Microsoft.Synchronization.DLL.
LOG: Attempting download of new URL file:///C:/Program Files (x86)/MyService.Service/bin/Microsoft.Synchronization.DLL.
LOG: Attempting download of new URL file:///C:/Program Files (x86)/MyService.Service/bin/Microsoft.Synchronization/Microsoft.Synchronization.DLL.
LOG: Attempting download of new URL file:///C:/Windows/Microsoft.NET/Framework64/v4.0.30319/Temporary ASP.NET Files/root/c1449ba8/abf9a5ae/Microsoft.Synchronization.EXE.
LOG: Attempting download of new URL file:///C:/Windows/Microsoft.NET/Framework64/v4.0.30319/Temporary ASP.NET Files/root/c1449ba8/abf9a5ae/Microsoft.Synchronization/Microsoft.Synchronization.EXE.
LOG: Attempting download of new URL file:///C:/Program Files (x86)/MyService.Service/bin/Microsoft.Synchronization.EXE.
LOG: Attempting download of new URL file:///C:/Program Files (x86)/MyService.Service/bin/Microsoft.Synchronization/Microsoft.Synchronization.EXE.
LOG: All probing URLs attempted and failed.
```

In my case I have a binding failure on Sync Framework assemblies. Makes sense since I haven’t installed it yet :).

[0]: http://bartdesmet.net/blogs/bart/archive/2006/10/23/Assembly-probing_2C00_-Fusion-and-fuslogvw-in-5-minutes.aspx
[1]: /files/image_9.png
[2]: /files/image_10.png
