---
title: DisconnectedContext when debugging with MSTest
categories : .Net
tags : WF
date: 2009-09-18 15:42:00 +10:00
---

I have been hitting a DisconnectedContext issue when I’ve been running MSTest in the Visual Studio IDE with the debugger attached. At first I thought it was an issue with debugging WF, but it is now also happening with LINQ to SQL.![image][0]

The debugging on the thread of the executing test appears to be stopped when this is hit. Other threads/tests in the test run will still hit breakpoints however.

Searching the net hasn’t yielded any pointers. Anyone out there have some information about this?

[0]: //files/image_7.png
