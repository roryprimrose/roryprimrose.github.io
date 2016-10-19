---
title: Visual Studio 2005 Performance
categories: .Net, IT Related
tags: Useful Links
date: 2007-11-02 09:15:19 +10:00
---

A sure sign that I haven't blogged recently is that Windows Live Writer has dropped off my recent programs in the start menu.

Anyway, I have seen this first set of content before (perhaps even blogged it), but it is worth including again as I have found another set of information for VS performance. Here are the goods:

[http://dotnettipoftheday.org/tips/speedup_visual_studio.aspx][0]

<!--more-->

## Speed up Visual Studio 2005

* Make sure Visual Studio 2005 SP1 is installed.
* Turn off animation.
Go to **Tools | Options | Environment** and uncheck **Animate environment tools**.
* Disable Navigation Bar.
If you are using ReSharper, you don't need VS2005 to update the list of methods and fields at the top of the file (CTRL-F12 does this nicely). Go to **Tools | Options | Text Editor | C#** and uncheck **Navigation bar**.
* Turn off Track Changes.
Go to **Tools | Options | Text Editor** and uncheck **Track changes**. This will reduce overhead and speeds up IDE response.
* Turn off Track Active item.
This will turn off jumping in the explorer whenever you select different files in different projects. Go to **Tools | Options | Projects and Solutions** and uncheck **Track Active Item in Solution Explorer**. This will ensure that if you are moving across files in different projects, left pane will still be steady instead of jumping around.
* Turn off AutoToolboxPopulate.
There is an option in VS 2005 that will cause VS to automatically populate the toolbox with any controls you compile as part of your solution. This is a useful feature when developing controls since it updates them when you build, but it can cause VS to end up taking a long time in some circumstances. To disable this option, select the **Tools | Options | Windows Forms Designer** and then set **AutoToolboxPopulate** to False.

[http://dotnettipoftheday.org/tips/optimize_launch_of_vs2005.aspx][1]

## Optimize the launch of the Visual Studio 2005

* Disable "Start Page".
1. Go to **Tools | Options**.
1. In **Environment | Startup** section, change **At startup** setting to **Show empty environment**.
* Disable splash screen.
1. Open the properties of Visual Studio 2005 shortcut.
1. Add the parameter <code>/nosplash</code> to the target.
* Close all unnecessary panels/tabs to prevent them from appearing when the IDE loads.

[0]: http://dotnettipoftheday.org/tips/speedup_visual_studio.aspx
[1]: http://dotnettipoftheday.org/tips/optimize_launch_of_vs2005.aspx
