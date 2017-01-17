---
title: Getting Rhino mock to return a new mock type for the same interface
categories: .Net
tags: Dependency Injection, RhinoMock, Unit Testing
date: 2008-12-03 13:31:00 +10:00
---

I am working with a bit of code (Manager) that involves caching values based on the type injected as a dependency (Resolver). The same Manager type can be used with different Resolvers and the keys used to store items in the Manager cache that are returned from the different Resolvers should be different. 

To achieve this, I generate a cache key that identifies the manager (constant string), the assembly qualified name of the resolver and then the name of the item, TraceSource instances in this case. This means that two resolvers injected into two different managers that are asked to return a TraceSource instance of the same name, will be stored in the managers internal cache as two entries. 

<!--more-->

When I was unit testing this behaviour, I found that Rhino mock is reusing mocked types. This means that the following code failed: 

```csharp
MockRepository mock = new MockRepository();
ITraceSourceResolver firstResolver = mock.CreateMock<ITraceSourceResolver>();
ITraceSourceResolver secondResolver = mock.CreateMock<ITraceSourceResolver>();
    
Assert.AreNotEqual(
    firstResolver.GetType().AssemblyQualifiedName,
    secondResolver.GetType().AssemblyQualifiedName,
    "Resolvers have the same name");
```

The easiest way around this was to get Rhino mock to see the mock definitions as different. The way to do this is request a multi-mock using some interface that means nothing to the test and therefore would have no impact on the test. I chose ICloneable to achieve this. 

The following now succeeds and I have two unique mocked types of the same interface to use in my unit tests. 

```csharp
MockRepository mock = new MockRepository();
ITraceSourceResolver firstResolver = mock.CreateMock<ITraceSourceResolver>();
ITraceSourceResolver secondResolver = mock.CreateMultiMock<ITraceSourceResolver>(typeof(ICloneable));
    
Assert.AreNotEqual(
    firstResolver.GetType().AssemblyQualifiedName,
    secondResolver.GetType().AssemblyQualifiedName,
    "Resolvers have the same name");
```


