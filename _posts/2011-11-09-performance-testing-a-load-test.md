---
title: Performance Testing a Load Test
categories : .Net
tags : Performance
date: 2011-11-09 22:59:51 +10:00
---

Running a load test in Visual Studio is a great way to put your code under stress. The problem is that once you have identified that there is a potential performance issue, how do you then narrow it down to particular methods in your code base. Using a Performance Session is great for this. 

The problem is that Visual Studio does not provide out of the box support for running a performance session against a load test. There is a reasonably easy workaround though. Any other profiling tool will also work using the same technique. The trick is to get the performance session to manually profile mstest.exe as it runs the load test. 

The way to set this up is as follows:

1. Set the application to profile as _C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\MSTest.exe_
1. Set the command line arguments to _/testcontainer:**YourLoadTest.loadtest** /noisolation /noresults_(replacing the filename in red with your loadtest file name)
1. Set the working directory to the $(TargetDir) of the project that contains the load test
1. Ensure you that you have compiled the project

![image][0]

The /noisolation switch is critical otherwise the profiler is going to profile mstest.exe as it manages the execution of the load test using different process. This means you won’t be profiling the load test, just the harness.

The /noresults switch just makes it easy to work with because otherwise you need to configure a unique results file name for each run.

When you start the profiler, it will then kick of mstest which in turn will run your load test.![image][1]

At the end of it, you then have your data.

![image][2]

[0]: /files/image_131.png
[1]: /files/image_132.png
[2]: /files/image_133.png
