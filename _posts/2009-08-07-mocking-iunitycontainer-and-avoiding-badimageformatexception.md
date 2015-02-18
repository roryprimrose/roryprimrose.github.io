---
title: Mocking IUnityContainer and avoiding BadImageFormatException
categories : .Net
tags : ASP.Net, Unity
date: 2009-08-07 13:30:00 +10:00
---

There is an issue with mocking IUnityContainer from RhinoMocks. A BadImageFormatException is thrown when you attempt to create a mock or a stub of IUnityContainer. For example: 

{% highlight csharp linenos %}
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
    
namespace TestProject1
{
    public interface ITestInterface
    {
        void DoSomething();
    }
     
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void MockingIUnityContainerFailsTest()
        {
            // This throws a BadImageFormatException
            IUnityContainer container = MockRepository.GenerateStub<IUnityContainer>();
            ITestInterface test = MockRepository.GenerateStub<ITestInterface>();
    
            container.Stub(x => x.Resolve<ITestInterface>()).Return(test);
    
            ITestInterface resolvedTest = container.Resolve<ITestInterface>();
    
            resolvedTest.DoSomething();
        }
    }
}
{% endhighlight %}

This test results in the following failure: 

> Test method TestProject1.UnitTest1.MockingIUnityContainerFailsTest threw exception: System.BadImageFormatException: An attempt was made to load a program with an incorrect format. (Exception from HRESULT: 0x8007000B).
> System.Reflection.Emit.TypeBuilder._TermCreateClass(Int32 handle, Module module)
> System.Reflection.Emit.TypeBuilder.CreateTypeNoLock()
> System.Reflection.Emit.TypeBuilder.CreateType()
> Castle.DynamicProxy.Generators.Emitters.AbstractTypeEmitter.BuildType()
> Castle.DynamicProxy.Generators.Emitters.AbstractTypeEmitter.BuildType()
> Castle.DynamicProxy.Generators.InterfaceProxyWithTargetGenerator.GenerateCode(Type proxyTargetType, Type[] interfaces, ProxyGenerationOptions options)
> Castle.DynamicProxy.DefaultProxyBuilder.CreateInterfaceProxyTypeWithoutTarget(Type theInterface, Type[] interfaces, ProxyGenerationOptions options)
> Castle.DynamicProxy.ProxyGenerator.CreateInterfaceProxyTypeWithoutTarget(Type theInterface, Type[] interfaces, ProxyGenerationOptions options)
> Castle.DynamicProxy.ProxyGenerator.CreateInterfaceProxyWithoutTarget(Type theInterface, Type[] interfaces, ProxyGenerationOptions options, IInterceptor[] interceptors)
> Castle.DynamicProxy.ProxyGenerator.CreateInterfaceProxyWithoutTarget(Type theInterface, Type[] interfaces, IInterceptor[] interceptors)
> Rhino.Mocks.MockRepository.MockInterface(CreateMockState mockStateFactory, Type type, Type[] extras)
> Rhino.Mocks.MockRepository.CreateMockObject(Type type, CreateMockState factory, Type[] extras, Object[] argumentsForConstructor)
> Rhino.Mocks.MockRepository.Stub(Type type, Object[] argumentsForConstructor)
> Rhino.Mocks.MockRepository.GenerateStub(Type type, Object[] argumentsForConstructor)
> Rhino.Mocks.MockRepository.GenerateStub[T](Object[] argumentsForConstructor)
> TestProject1.UnitTest1.MockingIUnityContainerFailsTest() in C:\Users\rprimrose\Documents\Visual Studio 2008\Projects\TestProject1\TestProject1\UnitTest1.cs: line 19
 
You can see in the stack trace that this is not actually an issue with RhinoMocks but an issue with Castle. As Moq also uses Castle internally it suffers from the same problem. Many people have [raised this as an issue and asked for a solution][0]. There does not seem to be an adequate answer out there other than a solution from [Roy Osherove][1] which is a bit heavy handed for what I want.

The answer is actually really simple. Reflector indicates the inheritance hierarchy of IUnityContainer as the following.![][2]

You can use either of these types to get the expected outcome and avoiding the BadImageFormatException. This exception seems to be an issue with mocking/stubbing the interface rather than its implemented types.

For example, both of the following tests pass.

{% highlight csharp linenos %}
[TestMethod]
public void MockingUnityContainerBaseSucceedsTest()
{
    IUnityContainer container = MockRepository.GenerateStub<UnityContainerBase>();
    ITestInterface test = MockRepository.GenerateStub<ITestInterface>();
    
    container.Stub(x => x.Resolve<ITestInterface>()).Return(test);
    
    ITestInterface resolvedTest = container.Resolve<ITestInterface>();
    
    resolvedTest.DoSomething();
}
    
[TestMethod]
public void MockingUnityContainerSucceedsTest()
{
    IUnityContainer container = MockRepository.GenerateStub<UnityContainer>();
    ITestInterface test = MockRepository.GenerateStub<ITestInterface>();
    
    container.Stub(x => x.Resolve<ITestInterface>()).Return(test);
    
    ITestInterface resolvedTest = container.Resolve<ITestInterface>();
    
    resolvedTest.DoSomething();
}
{% endhighlight %}

Easy fix.

[0]: http://www.google.com/search?q=mock+IUnityContainer+BadImageFormatException&amp;hl=en&amp;rls=com.microsoft:en-au&amp;start=0&amp;sa=N
[1]: http://weblogs.asp.net/rosherove/archive/2008/04/14/creating-a-automockingcontainer-with-microsoft-unity-pretty-darn-simple.aspx
[2]: //blogfiles/image_2.png
