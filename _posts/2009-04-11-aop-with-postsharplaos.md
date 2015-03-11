---
title: AOP with PostSharp.Laos
categories : .Net
tags : AOP, PostSharp, Tracing
date: 2009-04-11 15:17:00 +10:00
---

PostSharp.Laos is a Lightweight Aspect-Orientated System for PostSharp that makes it really easy to include AOP in your code. In this demo, PostSharp will be used to aspect the RunTest() method in the following application. The aspect will output console messages before and after the aspected method is executed.

<!--more-->

{% highlight csharp %}
using System;
     
namespace ConsoleApplication1
{ 
    internal class Program
    {
        private static void Main(string[] args)
        {
            RunTest();
     
            Console.ReadKey();
        }
     
        private static void RunTest()
        {
            Console.WriteLine("Running the test method");
        }
    }
}    
{% endhighlight %}

**Project references required**

The project requires PostSharp.Public.dll and PostSharp.Laos.dll references in order to create an aspect attribute.![image][0]

As you can see, I also added in a strong name key to see if it caused any issues with PostSharp. It didn’t.

**Code required**

I know that PostSharp.Laos is easy to use in comparison to PostSharp.Core, but I was honestly shocked at just how easy it really is. To put this demo together, it look somewhere in the order of two minutes.

In order to aspect the RunTest method in the code defined above, an OnMethodInvocationAspect derived attribute was created for the aspect and declared against the RunTest method.

{% highlight csharp %}
using System;
using PostSharp.Laos;
     
namespace ConsoleApplication1
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            RunTest();
     
            Console.ReadKey();
        }
     
        [MethodTraceAspect]
        private static void RunTest()
        {
            Console.WriteLine("Running the test method");
        }
    }
     
    [Serializable]
    internal class MethodTraceAspectAttribute : OnMethodInvocationAspect
    {
        public override void OnInvocation(MethodInvocationEventArgs context)
        {
            try
            {
                Console.WriteLine("Calling {0}", context.Delegate.Method);
     
                context.Proceed();
            }
            finally
            {
                Console.WriteLine("Called {0}", context.Delegate.Method);
            }
        }
    }
}    
{% endhighlight %}

PostSharp will find the MethodTraceAspect attribute on the RunTest method and change the IL so that the OnInvocation method of the attribute is invoked instead of the RunTest method. The context.Proceed() method in the attribute then invokes the RunTest method.

**IL result**

PostSharp kicks in after the original code has been compiled and changes the IL to include the aspect. Reflector shows the following outcome as the combination of the compiler and PostSharp doing their work.

{% highlight csharp %}
using System.Reflection;
using System.Runtime.CompilerServices;
using PostSharp.Laos;
     
namespace ConsoleApplication1
{
    internal class Program
    {
        [CompilerGenerated]
        static Program()
        {
            if (!~PostSharp~Laos~Implementation.initialized)
            {
                LaosNotInitializedException.Throw();
            }
            ~PostSharp~Laos~Implementation.~targetMethod~1 = methodof(Program.RunTest);
            ~PostSharp~Laos~Implementation.MethodTraceAspectAttribute~1.RuntimeInitialize(~PostSharp~Laos~Implementation.~targetMethod~1);
        }
     
        private static void ~RunTest()
        {
            Console.WriteLine("Running the test method");
        }
     
        private static void Main(string[] args)
        {
            RunTest();
            Console.ReadKey();
        }
     
        [DebuggerNonUserCode, CompilerGenerated]
        private static void RunTest()
        {
            Delegate delegateInstance = new ~PostSharp~Laos~Implementation.~delegate~0(Program.~RunTest);
            MethodInvocationEventArgs eventArgs = new MethodInvocationEventArgs(delegateInstance, null);
            ~PostSharp~Laos~Implementation.MethodTraceAspectAttribute~1.OnInvocation(eventArgs);
        }
    }
     
    [Serializable]
    internal class MethodTraceAspectAttribute : OnMethodInvocationAspect
    {
        public override void OnInvocation(MethodInvocationEventArgs context)
        {
            try
            {
                Console.WriteLine("Calling {0}", context.Delegate.Method);
                context.Proceed();
            }
            finally
            {
                Console.WriteLine("Called {0}", context.Delegate.Method);
            }
        }
    }
}
     
[CompilerGenerated]
internal sealed class ~PostSharp~Laos~Implementation
{
    internal static MethodBase ~targetMethod~1;
    internal static bool initialized;
    internal static readonly MethodTraceAspectAttribute MethodTraceAspectAttribute~1;
     
    static ~PostSharp~Laos~Implementation()
    {
        try
        {
            object[] objArray = (object[]) Internals.DeserializeFromResource(typeof(~PostSharp~Laos~Implementation).Assembly, "~PostSharp~Laos~CustomAttributes~");
            MethodTraceAspectAttribute~1 = (MethodTraceAspectAttribute) objArray[0];
            initialized = true;
        }
        catch (Exception exception)
        {
            Debugger.Log(30, "PostSharp", exception.Message);
            throw;
        }
    }
     
    [Serializable]
    public delegate void ~delegate~0();
}
{% endhighlight %}

The important part to note is that the contents of RunTest() have been moved into ~RunTest() and have been replaced with a call to MethodTraceAspectAttribute.OnInvocation. A delegate to ~RunTest() is passed in the arguments to OnInvocation so that the aspect can then invoke the original method.

**Compiled assembly references**

After PostSharp has finished weaving the aspect into the IL, the assembly still has a reference to PostSharp.Laos.dll and PostSharp.Public.dll.  
![][1]

**Debugging support**

Debugging the assembly after it has been aspected is seamless. When the Main method invokes RunTest(), the debugger steps into the MethodTraceAspectAttribute.OnInvocation method. Stepping into context.Proceed() takes you into the RunTest() method in the source code (which is actually ~RunTest in the compiled assembly). Stepping out of RunTest takes you back into the MethodTraceAspectAttribute.OnInvocation method.

**Runtime result**

![Result][2]

**License implications**

My understanding of the PostSharp license (read [here][3] and [here][4]) is that this implementation does not require a commercial license as PostSharp.Core.dll does not need to be bundled with the application for distribution.

**Advantages**

PostSharp.Laos is very easy to use. So easy in fact that I am surprised this product isn’t much more famous than it already is. Something this powerful that is so easy to use is a major advantage especially because it can be implemented for free.

**Disadvantages**

There are several disadvantages using PostSharp.Laos. The importance of each of these disadvantages will depend on your unique scenario.

* The final assembly requires references to PostSharp.Laos.dll and PostSharp.Public.dll. 
 * The requirement for the two PostSharp assemblies to be shipped may be an issue because of increased packaging and dependencies. I prefer projects to be as clean and lean as possible so the less references the better.
* The reference to the aspected method identifies it as ~RunTest rather than RunTest 
 * The changed name of the aspected method is only going to be an issue when the aspect implementation does something with that name. Tracing the method name (as in the case of this demo) may create confusion. The aspect could strip the leading ~, however no assumption about such a behaviour from PostSharp should be made.
* The IL isn’t very clean with all the code used to redirect the RunTest method invocation through the aspect. 
 * If you spend a lot of time in Reflector, the IL generated for the RunTest implementation will be a little confusing especially if compared against the original source code.
    
**Conclusion**

Wow, this is an unbelievable product. If you want to get some easy advantages out of AOP and don’t mind the few disadvantages stated above, then PostSharp.Laos is your new best friend.

[0]: /files/WindowsLiveWriter/AOPwithPostSharp.Laos/579CA136/image.png
[1]: /files/WindowsLiveWriter/AOPwithPostSharp.Laos/2C580A2F/image.png
[2]: /files/WindowsLiveWriter/AOPwithPostSharp.Laos/0C3CFD72/image.png
[3]: http://www.codingglove.com/products/postsharp-10/licensing
[4]: http://www.postsharp.org/about/faq
