---
title: TFS alerts for changes to NuGet packages
tags: TFS
date: 2012-11-29 11:21:51 +10:00
---

I’ve been a little late to the NuGet bandwagon. Overall I am really happy with the service that NuGet provides. It is not all smooth sailing though and the following are the pain points I have hit:

* Packages aren’t all strong named so our solution can’t be either
* Package dependencies being updated might break other packages that depend on them
* The amount of binding redirects added to config files is not always desirable and don’t always work
* TFS get latest on a solution does not bring down NuGet package changes

<!--more-->

I did have an idea about how to at least raise the awareness of new and updated NuGet packages in a solution to a team that uses TFS. Using the new Team Alerts feature in TFS 2012 will do the job. A custom TFS alert can notify the developers of a check in to the packages directory. The setup looks something like this:

Ensure that you have a Developers TFS team in the project.  
![image][0]

Add a new Checkin alert that watches a specific path.  
![image][1]

Configure the path as contains packages.  
![image][2]

All done.

[0]: /files/image_144.png
[1]: /files/image_145.png
[2]: /files/image_146.png
