---
title: Custom Workflow activity for business failure evaluation–Wrap up
categories: .Net, My Software
tags: WF
date: 2010-10-13 14:19:00 +10:00
---

My latest series on custom WF activities has provided a solution for managing business failures.   

The BusinessFailureEvaluator&lt;T&gt; activity evaluates a single business failure and may result in an exception being thrown.  

![][0]

The BusinessFailureScope&lt;T&gt; activity manages multiple business failures and may result in an exception being thrown for a set of failures.  
![][1]

<!--more-->

The following is a list of all the posts in this series.

* [Custom Workflow activity for business failure evaluation–Part 1][2] – Defines the high-level design requirements for the custom activities
* [Custom Workflow activity for business failure evaluation–Part 2][3] – Provides the base classes used to work with business failures
* [Custom Workflow activity for business failure evaluation–Part 3][4] – Describes the implementation of the BusinessFailureExtension
* [Custom Workflow activity for business failure evaluation–Part 4][5] – Describes the implementation of the BusinessFailureEvaluator&lt;T&gt; activity
* [Custom Workflow activity for business failure evaluation–Part 5][6] – Describes the implementation of the BusinessFailureScope&lt;T&gt; activity
* [Custom Workflow activity for business failure evaluation–Part 6][7] – Outlines the designer support for the custom activities 

This workflow activity and several others can be found in my [Neovolve.Toolkit project][8] (in 1.1 Beta with latest source code [here][9]) on [CodePlex][10]. 

[0]: /files/image_46.png
[1]: /files/image_45.png
[2]: /2010/10/11/custom-workflow-activity-for-business-failure-evaluatione28093part-1/
[3]: /2010/10/12/custom-workflow-activity-for-business-failure-evaluatione28093part-2/
[4]: /2010/10/12/custom-workflow-activity-for-business-failure-evaluatione28093part-3/
[5]: /2010/10/12/custom-workflow-activity-for-business-failure-evaluatione28093part-4/
[6]: /2010/10/13/custom-workflow-activity-for-business-failure-evaluatione28093part-5/
[7]: /2010/10/13/custom-workflow-activity-for-business-failure-evaluatione28093part-6/
[8]: http://neovolve.codeplex.com/releases/view/53499
[9]: http://neovolve.codeplex.com/SourceControl/changeset/view/67800#1422138
[10]: http://www.codeplex.com/