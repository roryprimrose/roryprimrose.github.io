---
title: When is EnvDTE.Project not an EnvDTE.Project?
categories : .Net, My Software
tags : Extensibility
date: 2008-05-19 13:37:01 +10:00
---

When it is a EnvDTE.ProjectItem. I must admit that this one took me by surprise. 

I am writing a Visual Studio addin for launching Reflector (see the CodePlex project [here][0]). You can launch Reflector from the tools menu and the Solution Explorer window and code document context menus. This issue came about when I found a bug in my addin while doing some testing. The scenario encountered was that the binary references for projects in Solution Explorer (the Project node and References node) were not resolved. I had tested this feature in another solution successfully and noticed that the difference between the solutions was that the one with the bug had the projects in solution folders.

After doing some debugging, it turns out that when a project is contained in a solution folder, the UIHierarchyItem.Object value exposed through Solution Explorer is no longer represented by an EnvDTE.Project. It is now an EnvDTE.ProjectItem. Ironically, you must reference ProjectItem.SubProject to get access to the project. 

The fact that the project is a project items sub project because its parent is a folder in Solution Explorer is totally confusing.

[0]: http://www.codeplex.com/NeovolveX
