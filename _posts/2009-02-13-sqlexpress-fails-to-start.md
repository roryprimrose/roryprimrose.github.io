---
title: SQLEXPRESS fails to start
categories : IT Related
date: 2009-02-13 09:33:00 +10:00
---

I have just encountered a problem where the SQLEXPRESS instance installed on my machine was not starting. It looks like a recent windows update has failed, but also knocked out SQL Server. The event log contains the following entry:

_> Error 3(error not found) occurred while opening file 'C:\Program Files\Microsoft SQL Server\MSSQL.1\MSSQL\DATA\master.mdf' to obtain configuration information at startup. An invalid startup option might have caused the error. Verify your startup options, and correct or remove them if necessary._

After [searching around][0], there seems to be lots of forum posts going back several years about this issue. The problem is that the only known solution seems to be to change the credentials of the SQLEXPRESS service account to Local System. This will then allow the service to start. Doing this through the services console presents a problem however because you can't set the service credentials back to Network Service as you need to know the password.

A better answer was found in [this forum post][1]. Using the SQL Server Configuration Manager, you can change between the system accounts without needing to know the password.![image][2]

What you need to do is change the account to Local System, then click Apply and Start. You can then click Stop and change the account back to Network Service and then click Apply and Start again.

[0]: http://www.google.com/search?q=Error+3(error+not+found)+occurred+while+opening+file+'C:\Program+Files\Microsoft+SQL+Server\MSSQL.1\MSSQL\DATA\master.mdf'+to+obtain+configuration+information+at+startup.+An+invalid+startup+option+might+have+caused+the+error.+Verify+your+startup+options,+and+correct+or+remove+them+if+necessary.&amp;rls=com.microsoft:en-au&amp;ie=UTF-8&amp;oe=UTF-8&amp;startIndex=&amp;startPage=1
[1]: http://social.msdn.microsoft.com/forums/en-US/sqldataaccess/thread/cd4cbc1d-3e0e-4a54-9e8f-f9df5b669992/
[2]: //blogfiles/WindowsLiveWriter/SQLEXPRESSfailstostart_83E6/image_7.png
