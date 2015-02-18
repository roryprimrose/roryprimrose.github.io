---
title: Mocking a factory created instance to test its consumer
categories : .Net
tags : Unit Testing
date: 2009-04-26 00:26:00 +10:00
---

I have faced a bit of a curly one tonight with a class that I want to unit test. Part of its implementation is to use an instance that is created from a factory class. To adequately test the class, I need to get the factory to return a mock. The factory class is a closed design so this is difficult to achieve. The factory does however use a configuration value to help it determine the concrete type to create. 

The difficulty with this scenario is that Type.GetType (using the configuration value) isn’t able to load the mock type directly. The solution to this problem is to use a wrapper around the mock. In doing this, the type loaded by Type.GetType is a statically defined type that happens to hold a reference to the mocked object to which it simply forwards on the required calls.

Here is a slimmed down example.

{% highlight csharp linenos %}using System; using System.Configuration; using Microsoft.VisualStudio.TestTools.UnitTesting; using Rhino.Mocks; namespace TestProject1 { public class ClassToTest { public void SomethingToTest() { IDependency dependency = DependencyFactory.Create(); if (string.IsNullOrEmpty(dependency.GetValue())) { throw new InvalidOperationException(); } } } public interface IDependency { String GetValue(); } public static class DependencyFactory { public const string DependencyTypeConfigurationKey = "DependencyType"; public static IDependency Create() { Type dependencyType = Type.GetType(ConfigurationManager.AppSettings[DependencyTypeConfigurationKey]); return (IDependency)Activator.CreateInstance(dependencyType); } } [TestClass] public class UnitTest1 { /// <summary&gt; /// Runs test for something to test throws exception when dependency returns an empty value. /// </summary&gt; [TestMethod] [ExpectedException(typeof(InvalidOperationException))] public void SomethingToTestThrowsExceptionWhenDependencyReturnsAnEmptyValueTest() { ConfigurationManager.AppSettings[DependencyFactory.DependencyTypeConfigurationKey] = typeof(DependencyMockWrapper).AssemblyQualifiedName; MockRepository mock = new MockRepository(); IDependency dependency = mock.CreateMock<IDependency&gt;(); using (mock.Record()) { dependency.GetValue(); LastCall.Return(String.Empty); } ClassToTest target = new ClassToTest(); using (mock.Playback()) { DependencyMockWrapper.MockInstance = dependency; target.SomethingToTest(); } } } public class DependencyMockWrapper : IDependency { public string GetValue() { return MockInstance.GetValue(); } public static IDependency MockInstance { get; set; } } } {% endhighlight %}

The ClassToTest is, as the name suggests, the class being tested. It makes a call to DependencyFactory to create an instance of something for it to use. Ideally we want this to be the mock, however we need to settle for a static wrapper around a mock. The DependencyFactory consults configuration to determine the type to create and it instantiates the type and returns it.

The unit test uses the DependencyMockWrapper class so that it can be loaded at runtime via Type.GetType, but also allows the unit test to inject the mock into the wrapper before it is used. The unit test sets up configuration to point to the wrapper, creates the mock with its expectations, and injects the mock into the wrapper. The key here is that the MockInstance property on the wrapper must be a static as the factory returns a new instance which the unit test can’t control and it still needs a reference to the mock.

As the class under test invokes the dependency created from the factory, the dependency (being the wrapper) simply forwards the call on to the mock. This satisfies the expectations set up when recording the mock.


