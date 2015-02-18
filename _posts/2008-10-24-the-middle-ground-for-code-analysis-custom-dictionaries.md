---
title: The middle ground for code analysis custom dictionaries
categories : .Net
tags : TFS
date: 2008-10-24 14:45:00 +10:00
---

I [posted almost a year ago][0] about using custom dictionaries for spell checking in code analysis. At the time, it seemed like a good idea to modify the custom dictionary in _C:\Program Files\Microsoft Visual Studio 9.0\Team Tools\Static Analysis Tools\FxCop_ so that any code on the machine will be evaluated against that dictionary. I'm now starting to swing away from this idea.

Having a custom dictionary in one spot on the machine is a good because any solution you use on that machine will adhere to that dictionary. The problem in a team environment is that the custom dictionary is not in source control. Any new dev machine will not have those changes and is likely to fail code analysis. Build servers will also suffer a maintenance nightmare with their custom dictionary as it will need to support all possible solutions being built, not just the ones on a particular dev machine.

If you swing to the other extreme, suppressing spelling failures in code is messy, but is in source control for other developers and build servers to benefit from.

I am working with a new model that is the middle ground of these two extremes.

I noticed a while ago that Visual Studio now contains more build actions for files in a project. One of them is _CodeAnalysisDictionary_. This becomes very helpful.![image][1]

My new method of satisfying spell checking in code analysis is to have my custom dictionary checked into source control. This usually means that the xml file will be located in the same place as the solution file.

The first thing to do is add the custom dictionary as a solution item.![image][2]

This makes it easy to open, check out, edit and check in this file without having to go through _Source Code Explorer_ to do a manual check out, edit, check in cycle. It also means that it is part of the solution in the _Pending Changes_ dialog when check in files are filtered by the current solution.

Next, we need to associate this single custom dictionary with all of the projects in the solution. We do not want to copy this file between the projects as this would still be a maintenance nightmare. Visual Studio is kind enough to have a file link facility. In each project, open up the _Add Existing Item_ dialog, find the custom dictionary and add it as a link.![image][3]

This will then add this file as a link to the project. You will notice that the overlay of the icon in _Solution Explorer_ also indicates that this file is a shortcut to another location.![image][4]

Open up the properties on this file, and select _CodeAnalysisDictionary_ as the _Build Action_ for this xml file.![image][1]

As you compile the application and run code analysis, this custom dictionary will be referenced.

The advantages of this solution are:

* Under source control so it is accessible to all developers
* Under source control so it is accessible to team build servers
* It contains only custom dictionary entries that are related to the projects that reference it
* Common to all projects in the solution
* Common to all projects in a TFS project/branch depending on TFS project structure
* Easily updated and checked in
* Visible to developers who use the solution

The disadvantage of this solution is:

* The custom dictionary may need to be duplicated for each solution/TFS project


[0]: /post/2007/12/06/vs2008-code-analysis-dictionary.aspx
[1]: //blogfiles/WindowsLiveWriter/Themiddlegroundforcodeanalysiscustomdict_B193/image_3.png
[2]: //blogfiles/WindowsLiveWriter/Themiddlegroundforcodeanalysiscustomdict_B193/image_6.png
[3]: //blogfiles/WindowsLiveWriter/Themiddlegroundforcodeanalysiscustomdict_B193/image_9.png
[4]: //blogfiles/WindowsLiveWriter/Themiddlegroundforcodeanalysiscustomdict_B193/image_12.png
