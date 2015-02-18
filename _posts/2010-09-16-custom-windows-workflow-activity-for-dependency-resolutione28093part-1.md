---
title: Custom Windows Workflow activity for dependency resolution–Part 1
categories : .Net
tags : Dependency Injection, WF
date: 2010-09-16 17:41:00 +10:00
---

The [previous post][0] talked about the issues with supporting DI in WF4. The nature of Windows Workflow means that there is no true support for DI. Using a custom extension and activity will allow for pulling in dependencies to a workflow while catering for the concerns outlined in the previous post. This series will refer to the dependency injection concept as dependency resolution because this technique is more aligned with the Service Locator pattern than DI.

This first part will go through the design goals of the custom activity.

The custom activity will have the following characteristics:

* Operate as a scope type activity (like ForEach<T&gt; or ParallelFor<T&gt;)
* It can hold a single child activity
* Resolved instance is available to all child activities
* Resolved instance is strongly typed
* Support named resolutions
* Support persistence
* Dependency should only be resolved when they are referenced
* Dependency should be torn down when the activity completes
* Dependency should be torn down when the activity is persisted
* Dependency must be resolved against when referenced after the activity is resumed from a bookmark
* Provide adequate designer support for authoring the child activity

The biggest sticking point identified for dependency resolution in a workflow activity is how to deal with persistence. Using a DI container as the mechanism for creating the dependency instance means that there can be no guarantee that the instance is serializable. An exception will be thrown by the workflow engine if a workflow contains a reference to an instance that is not serializable when the workflow is persisted.

There are several scenarios that this implementation needs to cater for regarding persistence.

1. A workflow execution where an instance is resolved and used without any persistence ![image][1]

  
  
This scenario is reasonably simple. The instance will be resolved when it is referenced. It will be torn down by the DI container (via the extension) when the activity completes.
1. A workflow execution where instances are resolved and used before and after a bookmark ![image][2]

  
  
This scenario requires that the first resolved instance is torn down when the bookmark causes the workflow to persist. The second instance resolution needs to have enough information to create an instance after the workflow has been resumed from the bookmark. The activity then needs to ensure that the second instance is torn down when the activity completes.
1. A workflow execution where an instance resolution scope surrounds a bookmark ![image][3]

  
  
As far as the implementation goes, this scenario is actually the same as the second. When the bookmark is resumed, there needs to be some information stored about the instance resolution so that the instance can be recreated again when it is referenced. This instance is then torn down when the activity completes.

**The problems of persistence**

The central issue with persistence of a dependency in WF is that the dependency instance may not be serializable. The support for persistence in this custom activity is a design issue more than an implementation issue. You may notice that the InvokeMethod activities in the above screenshots make a reference to an Instance property. This indicates how persistence is supported under the covers.

The answer to this problem is to persist the details of the resolution rather than the resolved instance itself. This means that we need to serialize the resolution type and the resolution name for each defined resolution in the workflow. Using a handler class will allow serialization of the resolution definition. It is this class that is provided to the child activity. This handler type specifically identifies that the actual resolved instance is not serializable. Using this handler type also provides the ability to resolve the instance only when it is requested.

The next post will go through the implementation of the handler and the extension classes.

[0]: /post/2010/09/15/Dependency-injection-options-for-Windows-Workflow-4.aspx
[1]: //blogfiles/image_26.png
[2]: //blogfiles/image_27.png
[3]: //blogfiles/image_28.png
