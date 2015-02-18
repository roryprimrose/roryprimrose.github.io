---
title: Custom Workflow activity for business failure evaluation–Wrap up
categories : .Net, My Software
tags : WF
date: 2010-10-13 14:19:00 +10:00
---

<p>My latest series on custom WF activities has provided a solution for managing business failures. </p>  <p>The BusinessFailureEvaluator&lt;T&gt; activity evaluates a single business failure and may result in an exception being thrown.</p>  <p><a href="//blogfiles/image_46.png"><img src="//blogfiles/image_46.png" /></a></p>  <p>The BusinessFailureScope&lt;T&gt; activity manages multiple business failures and may result in an exception being thrown for a set of failures.</p>  <p><a href="//blogfiles/image_45.png"><img src="//blogfiles/image_45.png" /></a></p>  <p>The following is a list of all the posts in this series.</p>  <ul>   <li><a href="/post/2010/10/11/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-1.aspx">Custom Workflow activity for business failure evaluation–Part 1</a> – Defines the high-level design requirements for the custom activities </li>    <li><a href="/post/2010/10/12/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-2.aspx">Custom Workflow activity for business failure evaluation–Part 2</a> – Provides the base classes used to work with business failures </li>    <li><a href="/post/2010/10/12/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-3.aspx">Custom Workflow activity for business failure evaluation–Part 3</a> – Describes the implementation of the BusinessFailureExtension </li>    <li><a href="/post/2010/10/13/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-4.aspx">Custom Workflow activity for business failure evaluation–Part 4</a> – Describes the implementation of the BusinessFailureEvaluator&lt;T&gt; activity </li>    <li><a href="/post/2010/10/13/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-5.aspx">Custom Workflow activity for business failure evaluation–Part 5</a> – Describes the implementation of the BusinessFailureScope&lt;T&gt; activity </li>    <li><a href="/post/2010/10/13/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-6.aspx">Custom Workflow activity for business failure evaluation–Part 6</a> – Outlines the designer support for the custom activities </li> </ul>  <p>This workflow activity and several others can be found in my <a href="http://neovolve.codeplex.com/releases/view/53499" target="_blank">Neovolve.Toolkit project</a> (in 1.1 Beta with latest source code <a href="http://neovolve.codeplex.com/SourceControl/changeset/view/67800#1422138" target="_blank">here</a>) on <a href="http://www.codeplex.com/">CodePlex</a>.</p> 