---
title: Using testrunconfig sometimes fails to deploy dependencies
categories : .Net
date: 2009-10-23 16:45:00 +10:00
---

I&rsquo;ve never been a fan of deploying test dependencies using testrunconfig. I prefer dependencies to be located as closely as possible to where they are used, ideally on an individual unit test. Using testrunconfig means that the dependencies are deploy at the solution level.

Having dependencies attached to each unit test for my current project has become unmaintainable however as almost all unit tests require the same dependencies. This makes testrunconfig appropriate in this case.

The issue I found with deploying dependencies with testrunconfig is that the dependency is not deployed into the TestResults directory when a new dependency reference is added to the config. This appears to be a bug in Visual Studio where the testrunconfig contents is cached. The workaround is to reload the solution. Each time this happens I have actually closed the IDE and opened the solution again. When the test run is executed the dependencies are not deployed as expected.


