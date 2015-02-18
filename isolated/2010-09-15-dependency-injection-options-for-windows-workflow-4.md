---
title: Dependency injection options for Windows Workflow 4
categories : .Net
tags : Dependency Injection, WF
date: 2010-09-15 16:42:05 +10:00
---

I’m a fan of DI for all the benefits that it brings. Unfortunately dependency injection is not really supported with Windows Workflow. 

DI is a pattern in which dependencies are calculated outside an entity and provided to the entity for it to use. The DI container is responsible for creating and managing these dependencies and injecting them onto the entity.

WF does not fully support this model. Any dependencies calculated outside a workflow must be provided to the workflow execution engine as a dictionary of input parameters. A DI container can be used resolve the dependencies, however providing them to the workflow via the workflow engine is a manual process. This means that constructor, property and method injection are not supported as you cannot use a container to resolve or build up a workflow instance.

There are two ways that you can get dependencies to be available on a workflow. These are via arguments or via a custom extension/activity implementation. 

There are a few considerations that dependency support in workflow should cater for. These are the amount of plumbing code required, validation of dependencies and support for persistence.

**Dependencies via workflow arguments (the almost DI pattern)**

Using workflow arguments to provide dependencies is similar to property injection. Using handler classes is helpful for abstracting the workflows from callers of the assembly. DI is used to create the handler with required dependencies. The handler then provides these dependencies to the workflow by manually pushing them through to the workflow execution as input parameters when a handler method is invoked. This produces decent amounts of manual code that pollutes the handler classes.

Workflows need to manually validate the arguments it has been provided. The workflow can’t make any assumptions about what has been provided to it so manual guard checks need to be added to the workflow. This is then further implementation in the workflow that requires testing.

Workflow arguments may not be serializable. The workflow isn’t responsible for creating the arguments and can’t guarantee that the arguments are serializable. Persisting a workflow to durable storage (SQL Server for example) would only be supported if all the dependencies were serializable. An exception would be thrown if a non-serializable argument was provided and the workflow was persisted and unloaded.

_Pros_

* Not coupled to a DI container

_Cons_

* Workflow invocation code is messy
* Explicit dependency validation required
* Persistence support not guaranteed

**Dependencies via custom extension/activity (the Service Locator pattern)**

Using a custom extension or activity is more aligned with the service locator (anti-) pattern than dependency injection. A DI container should still be used in this method to resolve the dependencies. This is because the DI containers have the logic to create complex dependencies and typically have some aspects of instance lifetime management.

The first benefit of this method is that there is no plumbing code to provide these dependencies to the workflow engine. The dependencies will be requested by an activity inside the workflow execution and be resolved at that point (hence a service locator pattern).

Dependencies resolved inside the workflow execution will not require validation of the dependencies. A DI container would normally throw an exception if a dependency can’t be resolved. If this is not the case then the custom logic involved can provide that validation.

Serialization of dependencies is still an issue with this method. The custom implementation can cater for this however and will be covered in the next post.

Any custom implementation in WF that uses this method should leverage a custom extension even if a custom activity is also used to provide the dependency to the parent workflow. This will allow the same dependency resolution logic to be available from any activity or extension running in the workflow execution. Using an extension is also a good way to abstract the DI container from any custom activities that request a dependency.

_Pros_

* Workflow invocation is clean
* Implicit dependency validation

_Cons_

* Slightly more coupled to DI container
* Persistence support not guaranteed, although can be fully supported

**Conclusion**

Most of my workflow experience has used workflow arguments to provide dependencies. I tend to be a purist so I prefer a true DI implementation over a Service Locator implementation. I am however finding the advantages of using a custom extension/activity to resolve dependencies in WF4 far outweigh the disadvantages.

Persistence is the main disadvantage with the custom extension/activity method outlined above. The next post will provide an implementation that fully caters for persistence.


