---
title: Splitting up arguments with a regular expression
categories : .Net
date: 2009-02-18 13:13:21 +10:00
---

There have been several times when I have needed to process a string that contains a set of arguments. It always seems to be a lot of work splitting up arguments by using white space as a delimiter because you need to take into account white space that is surrounded by brackets. 

Just for kicks, I tried to come up with a regular expression that would do just that. Here is the result.

> [^\s&quot;]*&quot;[^&quot;]*&quot;[^\s&quot;]*|[^\s&quot;]+


