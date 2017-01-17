---
title: WF rules and bitwise OR
categories: .Net
tags: WF
date: 2007-03-15 10:09:52 +10:00
---

I have been doing some WF development this morning and came across something interesting. I had a PolicyActivity in which I was trying to assign a bitwise enum value on an object instance. The rules editor gave be an error saying that I couldn't use a bitwise OR operation on the types of operands I was using. It seems to me that CodeDom doesn't correctly parse bitwise OR. 

From what I have googled this morning, is looks like the WF rules engine is supposed to support it. I wasn't however able to find any code examples or screenshots of where people have successfully implemented bitwise OR. So at this stage it looks like it is just documented, not actually used.

My workaround was to modify the enum to include a definition of the bitwise value that I require so that the | operator isn't in the rule definition. While this covers this particular scenario, what happens when the enum that I need to use is defined in an assembly that I don't control?


