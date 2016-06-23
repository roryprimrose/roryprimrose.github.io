---
title: Custom Windows Workflow activity for dependency resolution–Wrap up
categories: .Net
tags: Dependency Injection, Unity, WF
date: 2010-10-01 15:39:04 +10:00
---

I have been writing a series of posts recently about implementing a custom WF activity that will provide dependency resolution support for WF4 workflows. ![][0]![image][1]

The InstanceResolver activity caters for lazy loading dependencies, multiple resolutions on the one activity, workflow persistence (including support for non-serializable dependencies) and lifetime management of the resolved dependencies. The activity uses Unity as the backend IoC container however this could be modified to support a different container with minimal changes.

<!--more-->

The following is a list of all the posts in this series.

* [Dependency injection options for Windows Workflow 4][2] - Describes the options for getting dependencies into a workflow
* [Custom Windows Workflow activity for dependency resolution–Part 1][3] - Outlines the design goals for the activity
* [Custom Windows Workflow activity for dependency resolution–Part 2][4] - Provides the activity extension support for resolving dependencies
* [Custom Windows Workflow activity for dependency resolution–Part 3][5] - Provides the implementation for the InstanceResolver activity
* [Creating updatable generic Windows Workflow activities][6] - A segue into an implementation for updatable generic typed arguments for generic activities
* [Custom Windows Workflow activity for dependency resolution–Part 4][7] - Provides designer support with IRegisterMetadata and custom morphing
* [Custom Windows Workflow activity for dependency resolution–Part 5][8] - Provides a custom updatable generic type argument support specific to the InstanceResolver activity
* [Custom Windows Workflow activity for dependency resolution–Part 6][9] - Provides the XAML designer for the custom activity and a designer service for managing attached properties

This workflow activity and several others can be found in my [Neovolve.Toolkit project][10] which can be found [here][11] on Codeplex.

[0]: /files/image_36.png
[1]: /files/image_37.png
[2]: /2010/09/15/dependency-injection-options-for-windows-workflow-4/
[3]: /2010/09/16/custom-windows-workflow-activity-for-dependency-resolutione28093part-1/
[4]: /2010/09/29/custom-windows-workflow-activity-for-dependency-resolutione28093part-2/
[5]: /2010/09/30/custom-windows-workflow-activity-for-dependency-resolutione28093part-3/
[6]: /2010/09/30/creating-updatable-generic-windows-workflow-activities/
[7]: /2010/09/30/custom-windows-workflow-activity-for-dependency-resolutione28093part-4/
[8]: /2010/09/30/custom-windows-workflow-activity-for-dependency-resolutione28093part-5/
[9]: /2010/10/01/custom-windows-workflow-activity-for-dependency-resolutione28093part-6/
[10]: /2010/10/01/neovolvetoolkit-10-rtw/
[11]: http://neovolve.codeplex.com/releases/view/19004
