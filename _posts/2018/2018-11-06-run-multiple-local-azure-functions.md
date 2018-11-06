---
title: Run multiple local Azure Functions
categories: .Net
tags: 
date: 2018-11-06 15:10:00 +11:00
---

I recently broke a fairly large Windows Service application which ran a lot of jobs (using Quartz.net) down into individual Azure Functions. Most of the jobs (now functions) participate in a logic flow of data from one end of the system to the other. While there were many benefits for breaking up this sytem into more of a micro-services architecture, it did leave one big problem. How do you run multiple functions on the local machine?

<!--more-->

The first issue you will hit is that running just two functions on the local development machine will fail on the second function. This is because the local host configuration uses the same port which is locked by the first function.

I also had several requirements for running multiple functions. I want to:

- control the entire set of functions via one application. This means one application in the task bar rather than a large number of powershell scripts consoles
- run additional actions before the function like managing git branches
- see the console output of each function (with full colour support)
- start all the functions in a single action
- stop all the functions in a single action

I tried several options, including starting to write my own application to wrap around the functions CLI but this wasn't going to be a good enough outcome. The answer was ultimately the very clever [ConEmu][0] application.

The way to configure ConEmu to execute multiple local Azure Functions is to go into Settings -> Tasks and add a new predefined task. My predefined task in ConEmu will do the following for each function:

- output the current git branch
- output the git branch list
- pull the current branch
- invoke the functions CLI with a build and custom port

For example, the following script is a predefined task that will do the above steps for FunctionA, FunctionB and FunctionC.

```
cmd.exe /k git branch & git branch -r & git pull & func host start --build --port 7199 -new_console:d:"C:\Repos\FunctionA\FunctionA":t:FunctionA

cmd.exe /k git branch & git branch -r & git pull & func host start --build --port 7200 -new_console:d:"C:\Repos\FunctionB\FunctionB":t:FunctionB

cmd.exe /k git branch & git branch -r & git pull & func host start --build --port 7201 -new_console:d:"C:\Repos\FunctionC\FunctionC":t:FunctionC
```

ConEmu provides a really nice interface to select a particular named task from a dropdown list and also provides nice taskbar preview for each task/tab.

[0]: https://conemu.github.io/