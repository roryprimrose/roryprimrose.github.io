---
title: Be careful of code clean-up's when you have WF projects
categories: .Net, IT Related
tags: WF
date: 2007-01-29 09:21:49 +10:00
---

I am really enjoying using WCF and WF in my development. While playing with the new toys can be frustrating at times, it is a whole lot of fun. There are a few gotchas though. The one I want to highlight here is the "Find All References" support in the IDE. Here is a scenario that can bite you.

Let's say that I am using PolicyActivities to do pre and post condition validation. I may also be using them to change data in the workflow based on rules. For example, I might need to do some business to data object mapping. To neatly achieve this conversion while avoiding a CodeActivity, I can have a rule in the PolicyActivity that calls a static method on a converter class. Now that my solution is created, I want to clean up the code to make sure I don't have redundant methods. 

In order to check if a method is being used, I would normally have used the find all references feature to see if the method is being used anywhere, but here's the kicker. This feature (from what I can guess) only looks at code that will be compiled down to IL. This will not work when the method exists in a rules file because the rules file is simply compiled as an embedded resource. 

When you have WF projects included in your solution, run a text search for the method, searching all files in the solution (not just *.cs files). Oh, and always re-run your tests after you can any code.


