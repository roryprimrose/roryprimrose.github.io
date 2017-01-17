---
title: Inheriting A WebForm From A Custom Class
categories: .Net
date: 2005-04-15 03:01:00 +10:00
---

For the last couple of days at work, I have been developing a new ASP.Net site. Across all the pages in the site, there are some common methods and properties that need to be used. In the case of this new application, I needed to check if the authenticated user has permissions in Active Directory to see the details of the user they are requesting.  
  
 The easiest way to achieve this common functionality is to create a base class that inherits from System.Web.UI.Page. Each WebForm code behind page can then inherit from the custom class instead of the default System.Web.UI.Page class. I hit some interesting problems with developing this solution in the VS IDE.   
  
<!--more-->

 The first problem is that the design-time view of the pages usually didn't render because it couldn't identify the custom class. This was caused because I initially had the custom class in the ASP.Net project. This is an issue because the design-time render needs a compiled class to render controls and pages as it doesn't interpret uncompiled code. As the class was in the web project , the assembly may not yet be compiled, or if compiled, it usually can't be resolved.   
  
 To fix this problem, I separated the custom class out into a class library which was referenced by the web project. This meant that when the designer attempted to render the design-time display, it had a compiled assembly that it could resolve and use.  
  
 The second problem was that the IDE was continuously locking files in the .Net cache. This caused compiling problems with the solution. I can only guess that this second problem was caused by the custom class being in the web project as they haven't happened since I separated the class out into another project.


