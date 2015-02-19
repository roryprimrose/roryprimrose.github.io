---
title: MStest unit test adapter fails on CorrelationManager logical operation that isn't stopped
categories : .Net
tags : Tracing, Unit Testing, WF
date: 2008-12-11 13:02:00 +10:00
---

I have been writing lots of unit tests for my [Toolkit][0] project on CodePlex. The most recent work is adding activity tracing support. As I was writing these unit tests, I came across a bug in the unit testing framework in Visual Studio. If a logical operation is started in the CorrelationManager with a non-serializable object, but not stopped before the unit test exits then the unit test adapter throws an exception. 

This is easily reproduced with the following code: 

{% highlight csharp linenos %}
using System.Diagnostics; 
using Microsoft.VisualStudio.TestTools.UnitTesting; 
      
namespace TestProject2 
{ 
    [TestClass] 
    public class UnitTest1 
    { 
        [TestMethod] 
        public void TestMethod1() 
        { 
            Trace.CorrelationManager.StartLogicalOperation(new TestObject()); 
        } 
    } 
      
    internal class TestObject 
    { 
    } 
}     
{% endhighlight %}

This fails with the message: 

> _Unit Test Adapter threw exception: Type 'TestProject2.TestObject' in assembly 'TestProject2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' is not marked as serializable._ 

It seems that there is an assumption somewhere in the unit test adapter that assumes that the object added to the logical operation stack is the one that the adapter put there (an object that must be serializable). Given that any user code can start its own operation with a custom object, this is a bit of a problem. 

The workaround is to ensure that your test stops the logical operation that it started. Internally, this pops the object off the stack. This means that the object on the top of the stack is most likely to be the one that the adapter put there when the unit test adapter goes to serialize/deserialize the object. 

For example, the following code passes: 

{% highlight csharp linenos %}
using System.Diagnostics; 
using Microsoft.VisualStudio.TestTools.UnitTesting; 
      
namespace TestProject2 
{ 
    [TestClass] 
    public class UnitTest1 
    { 
        [TestMethod] 
        public void TestMethod1() 
        { 
            Trace.CorrelationManager.StartLogicalOperation(new TestObject()); 
            Trace.CorrelationManager.StopLogicalOperation(); 
        } 
    } 
     
    internal class TestObject 
    { 
    } 
}     
{% endhighlight %}

I have raised a bug in Connect [here][1]. 

[0]: http://www.codeplex.com/Neovolve
[1]: https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=387535
