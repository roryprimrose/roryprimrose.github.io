---
title: Sorting Windows Workflow rules
categories : .Net, Applications
tags : Great Tools, WF
date: 2007-06-20 21:17:00 +10:00
---

<p>
I&#39;ve been having some issues with WF rules files over the last few weeks. I am using code generation to build some services that use workflow&#39;s in the business layer. The problems start when I run a code generation after editing the workflow rules and want to compare the difference. The result tends to be something like this for what is actually the same content: 
</p>
<p>
<a href="/photos/blog_images/picture2530.aspx" target="_blank"></a>
</p>
<p>
<a href="//blogfiles/WindowsLiveWriter/SortingWindowsWorkflowrules_B664/CompareBefore_2.jpg"><img style="border: 0px" src="//blogfiles/WindowsLiveWriter/SortingWindowsWorkflowrules_B664/CompareBefore_thumb.jpg" border="0" alt="CompareBefore" width="606" height="484" /></a> 
</p>
<p>
This is even a tame example. I have had full color difference indicators before. 
</p>
<p>
The reason for this is that the WF designer does not reliably write the xml for the rules in the same way. From just a little bit of testing, it places new rules at the bottom of the ruleset, and places the last modified ruleset at the bottom of the file. I wrote a quick addin that searches for editable rule files in the solution and sorts the contents. It sorts the rulesets alphabetically by name, and then the rules according to descending priority. It will maintain the existing defined order of rules for those with the same priority. 
</p>
<p>
I&#39;m almost ashamed to post this addin because it is such a hack in its implementation. I had a problem where XPath queries were not returning results so the code has a couple of very unfortunate workarounds. That being said, the compare tool results now look like this: 
</p>
<p>
<a href="//blogfiles/WindowsLiveWriter/SortingWindowsWorkflowrules_B664/CompareAfter_2.jpg"><img style="border: 0px" src="//blogfiles/WindowsLiveWriter/SortingWindowsWorkflowrules_B664/CompareAfter_thumb.jpg" border="0" alt="CompareAfter" width="606" height="484" /></a> 
</p>
<p>
Download: <a href="/blogfiles/2008%2f9%2fWorkflowRulesSorter.zip">WorkflowRulesSorter.zip (10.60 kb)</a>
</p>

