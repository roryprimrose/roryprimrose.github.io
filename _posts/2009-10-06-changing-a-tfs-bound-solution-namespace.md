---
title: Changing a TFS bound solution namespace
categories: .Net
tags: ReSharper, TFS
date: 2009-10-06 16:17:00 +10:00
---

Changing the namespace of a solution that has already progressed significantly down the development path can be a pain. Visual Studio doesn't native support namespace refactoring and tools like R# can get bogged down with a huge set of changes. There is usually a dirty result if either of these tools are used as the project and solution directories are not renamed. These need to be done manually in Source Code Explorer which then throws out the relative paths stored in the solution file.

Changing a namespace can be a messy job. In these circumstances it's often best to hand craft an external solution to this problem. I've finally written that solution as a console app after doing this job the painful manual way too many times.

The attached file contains the console application code. There is no defensive code against exceptions so it's best to run this in the IDE. There are three constants you need to update before running this. These are the root path, find text and replace text. The code records everything that happens out to the console and a log file that is unique for each run.

[Program.cs (11.54 kb)][0]

[0]: /files/2009/10/Program.cs
