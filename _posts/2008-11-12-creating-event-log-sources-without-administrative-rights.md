---
title: Creating event log sources without administrative rights
categories : .Net, Applications
date: 2008-11-12 20:13:00 +10:00
---

I have been coming up against a scenario where I need to create new event log sources in an existing event log at runtime. 

**Using System.Diagnostics.EventLog**

Administrative rights are required to create event log sources using the _System.Diagnostics.EventLog_ class. Without administrative rights, CAS kicks in and a SecurityException is thrown.

> _System.Security.SecurityException: The source was not found, but some or all event logs could not be searched. Inaccessible logs: Security._

Attempting to create a new source or checking to see if the source already exists will enumerate all the event logs on the machine. This requires administrative rights because some event logs (such as the Security event log) are protected with specific security permissions. Without administrative rights, those event logs fail to be read.

**Avoiding System.Diagnostics.EventLog**

As administrative rights shouldn't be used to run applications and services, this is a bit of a problem. Almost every suggested solution on the net says to run the application with administrative rights. This is a really bad idea. If the application is a client application, the user's profile doesn't necessarily have administrative rights to begin with. If it is a service application, you don't want it to run with administrative rights as this would be a significant security risk (especially if it is an IIS based application).

A better workaround is to modify the registry directly rather than using the _System.Diagnostics.EventLog_ class. Ideally, the installation of the application/service should create a custom event log for use by the application. Whichever event log is used, appropriate ACL's should be applied to the registry key for that event log so that the application has read/write permissions to that event log (see screen shot below). This allows new event log sources to be created directly without having to use the _System.Diagnostics.EventLog_ class.

The general logic to achieve this is:

* Connect to _LocalMachine_ registry on the local/remote machine
* Open key _SYSTEM\CurrentControlSet\Services\EventLog\[YourEventLogNameHere]_
* Throw exception if the event log does not exist
* Open key _[YourEventLogSourceNameHere]_
* If the key is null (the source does not yet exist) 
  * Create new sub key using _[YourEventLogSourceNameHere]_
  * Create new string value _EventMessageFile_ in the new subkey with the value _C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\EventLogMessages.dll_

To set up the permissions to achieve this, the installation package needs to assign permissions to the registry key for the event log for the account used to run the application. The location of the event log in the registry is indicated in the first two points above. 

The permissions should look something like this:![image][0]

**Update:**

After further testing, the only permissions required for the event log are _Set Value_ and _Create Subkey_. The above settings for _Query Value_ and _Enumerate Subkeys_ are not required. This is because when you run the above logic, querying for the existence of a key (by simply calling OpenSubKey) is not querying a value and isn't enumerating the keys. Once the source is created, no further registry permissions are required to write to the event log.

[0]: //files/WindowsLiveWriter/Creatingeventlogsourceswithoutadministra_DCD9/image_3.png
