---
title: Reflection Pop Quiz - Does interface inheritance exist?
categories: .Net
date: 2008-07-03 14:43:10 +10:00
---

I've written some code that reflections MethodInfo objects from a type using its name and signature. This has been working well and all unit tests have passed, until now. 

Consider the following code:

<!--more-->

```csharp
internal interface IBaseInterface
{
    void GetSomething();
}
    
internal interface IDerivedInterface : IBaseInterface
{
    void DoSomethingElse();
}
```

Does interface inheritance exist?

Much to my amazement, the answer is no, at least according to reflection. This is completely not what I had assumed.

Does it matter? Normally, probably not. The reason is that most people are dealing with concrete types. As concrete types provide implementations of the methods that are defined by their interfaces, the type correctly returns the expected MethodInfo objects from Type.GetMethods(). 

Interfaces behave very differently. If you call Type.GetMethods() on IDerivedInterface then you will be missing the methods defined in IBaseInterface. I expected that IDerivedInterface had a base type of IBaseInterface and inherited its methods. I made this assumption because that is the affect that IDerivedInterface has on types that implement it. There is an inheritance behaviour as the concrete type must implement methods for both interfaces.

What is interesting is that IDerivedInterface doesn't have a base type, and Type.GetMethods() doesn't return IBaseInterface methods. IDerivedInterface does however indicate that it implements the IBaseInterface interface. 

Now things get a little curly here. An interface that implements an interface doesn't need to provide any implementation (it can't, its an interface) or do anything to satisfy implementing the interface. This makes me think that interfaces neither implement other interfaces, or inherit from them.

Here is the complete source:

```csharp
using System;
using System.Reflection;
     
namespace ConsoleApplication1
{
    internal class Program
    {
        private static void Main(String[] args)
        {
            // _______________________________________________________
            //
            // Base Interface test
            // _______________________________________________________
            Type baseInterfaceTest = typeof(IBaseInterface);
     
            // This returns 1 method
            MethodInfo[] baseInterfaceMethods = baseInterfaceTest.GetMethods();
     
            // This returns false
            Boolean baseInterfaceHasBaseClass = baseInterfaceTest.BaseType != null;
     
            // This returns null
            Type[] baseInterfaceInterfaces = baseInterfaceTest.GetInterfaces();
     
            // _______________________________________________________
            //
            // Derived Interface test
            // _______________________________________________________
     
            Type derivedInterfaceTest = typeof(IDerivedInterface);
     
            // This will only return 1 method
            MethodInfo[] derivedInterfaceMethods = derivedInterfaceTest.GetMethods();
     
            // This returns false
            Boolean derivedInterfaceHasBaseClass = derivedInterfaceTest.BaseType != null;
     
            // This returns 1 interface type
            Type[] derivedInterfaceInterfaces = derivedInterfaceTest.GetInterfaces();
     
            // _______________________________________________________
            //
            // Derived Type test
            // _______________________________________________________
     
            Type derivedTypeTest = typeof(DerivedType);
     
            // This will return 2 methods (in addition to the 4 from System.Object)
            MethodInfo[] derivedTypeMethods = derivedTypeTest.GetMethods();
     
            // This will return true (System.Object)
            Boolean derivedTypeHasBaseClass = derivedTypeTest.BaseType != null;
     
            // This returns 2 interface type
            Type[] derivedTypeInterfaces = derivedTypeTest.GetInterfaces();
        }
    }
     
    internal interface IBaseInterface
    {
        void GetSomething();
    }
     
    internal interface IDerivedInterface : IBaseInterface
    {
        void DoSomethingElse();
    }
     
    internal class DerivedType : IDerivedInterface
    {
        public void DoSomethingElse()
        {
        }
     
        public void GetSomething()
        {
        }
    }
}    
```


