---
title: ActivityExecutionContext failed to serialize
categories : .Net
tags : WF
date: 2007-08-13 14:07:04 +10:00
---

_> An exception of type 'System.Runtime.Serialization.SerializationException' occurred in Neovolve.Framework.Workflow.DLL but was not handled in user code__> Additional information: Type 'System.Workflow.ComponentModel.ActivityExecutionContext' in Assembly 'System.Workflow.ComponentModel, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35' is not marked as serializable._

This was a bit of a curly one. 

The problem was caused because one of my custom activities couldn't be serialized. The custom activity was used in a WhileActivity. Serialization is happening behind the scenes because the WhileActivity uses serialization to create clones of the activities in its loop.

[Jon Flanders][0] posted some great entries about AEC and serialization ([here][1] and [here][2]).

The moral of the story is to ensure that your custom activities can be serialized (or workarounds developed) so that WF persistence and WhileActivities can use them.

[0]: http://www.masteringbiztalk.com/blogs/jon/
[1]: http://www.masteringbiztalk.com/blogs/jon/PermaLink,guid,5f4d8c41-73bf-4d7f-93b4-8934130a783b.aspx
[2]: http://www.masteringbiztalk.com/blogs/jon/PermaLink,guid,4e0f556a-2546-4aac-bda9-698b4d5c04ee.aspx
