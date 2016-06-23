---
title: Dealing with ExecutionEngineException, code contracts and WF
categories: .Net
tags: WF
date: 2011-10-10 13:34:20 +10:00
---

I recently battled the scary ExecutionEngineException. It’s scary because the exception itself does not provide any information about what has gone wrong and why. Figuring out why the exception is being thrown can get a little tricky.

In my scenario, Visual Studio was crashing when trying to display a WF designer. After attaching another Visual Studio instance to debug the crash, I found that the exception being thrown in the designer was ExecutionEngineException. The place it was being thrown was in an evaluation of a Code Contract (Requires&lt;ArgumentNullException&gt;). I quickly realised that I had added some code contracts into my WF design assembly but not enabled the code contract rewriter for compiling the assembly. Rather than throwing an ArgumentNullException for a given condition, the contract was not correctly written into the assembly and the framework threw ExecutionEngineException instead.

The ExecutionEngineException appears to be a fairly low level exception. Unfortunately its use in Code Contracts is confusing because of the lack of information about what is going wrong and how the developer should fix it.


