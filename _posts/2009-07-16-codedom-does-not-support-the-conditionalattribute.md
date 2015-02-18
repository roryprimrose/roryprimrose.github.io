---
title: CodeDom does not support the ConditionalAttribute
categories : .Net
tags : WF
date: 2009-07-16 11:31:06 +10:00
---

I need to output some debug statements from a PolicyActivity as I’m having some issues with running a workflow from a unit test. Adding some Debug.WriteLine statements to the rule set seems like a good idea given that I am not getting the WF designer step through experience from MSTest. I thought about whether CodeDom checks for the Conditional attribute before it invokes a method. 

CodeDom evaluates the xml rules and executes the instructions in C#. At compile time, native C# is striped of any method call that is decorated with a ConditionalAttribute that does not match the additional compilation symbols of the build configuration. For example, the Debug.WriteLine statement is decorated with Conditional(“Debug”) attribute so any calls to this method in a typical Release build configuration will get stripped out as the Debug compilation symbol is not defined. What about CodeDom though?

As expected, CodeDom does not check the ConditionalAttribute marker on methods it invokes. As such, debugging methods are evaluated and invoked at runtime regardless of the build configuration.


