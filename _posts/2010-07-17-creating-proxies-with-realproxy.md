---
title: Creating proxies with RealProxy
categories : .Net, .Net
tags : WCF, WCF
date: 2010-07-17 19:53:24 +10:00
---

I first came across the RealProxy class three years ago when trying to figure out better ways of handling WCF client proxies (correct connection disposal, reusing faulted channels etc). I was intrigued about how ChannelFactory&lt;T&gt; could return an instance of a service contract that had the implementation of a WCF client for an interface definition it knows nothing about.   

With a decent amount of Reflector surfing I followed the rabbit hole down to the usage of RealProxy. In my opinion this has to be one of the most interesting classes in the CLR. I finally founds some time to post some information about this amazing class.   

Imagine a scenario where you want to have an action that you want to take when a method on a type is invoked when you do not have any prior knowledge of the type definition. The RealProxy class is able to run some magic that will return you a proxy instance of that type and provide notification when method invocations occur on that proxy. The main downside with RealProxy is that the type to proxy must either be an interface or a class that inherits from MarshalByRefObject.  

The following is an example of a RealProxy implementation that outputs method invocations to the console.  

{% highlight csharp linenos %}
public class TestProxy<T> : RealProxy
{
    public TestProxy()
        :base(typeof(T))
    {
    }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
    [DebuggerStepThrough]
    public override IMessage Invoke(IMessage msg)
    {
        Debug.Assert(msg != null, "No message has been provided");

        ReturnMessage responseMessage;
        Object response = null;
        Exception caughtException = null;

        try
        {
            String methodName = (String)msg.Properties["__MethodName"];
            Type[] parameterTypes = (Type[])msg.Properties["__MethodSignature"];
            MethodBase method = typeof(T).GetMethod(methodName, parameterTypes);

            Object[] parameters = (Object[])msg.Properties["__Args"];

            // ______________________________________________________________________________
            //
            // Start the logic for handling the method invocation on this proxy
            // ______________________________________________________________________________

            Console.WriteLine("Invoking " + method.Name + " with the following parameters:");

            for (int index = 0; index < parameters.Length; index++)
            {
                Console.WriteLine(parameterTypes[index].Name + " - " + parameters[index]);
            }

            // ______________________________________________________________________________
            //
            // End the logic for handling the method invocation on this proxy
            // ______________________________________________________________________________
        }
        catch (Exception ex)
        {
            // Store the caught exception
            caughtException = ex;
        }

        IMethodCallMessage message = msg as IMethodCallMessage;

        // Check if there is an exception
        if (caughtException == null)
        {
            // Return the response from the service
            responseMessage = new ReturnMessage(response, null, 0, null, message);
        }
        else
        {
            // Return the exception thrown by the service
            responseMessage = new ReturnMessage(caughtException, message);
        }

        // Return the response message
        return responseMessage;
    }
}
{% endhighlight %}

The class informs RealProxy about the type to proxy when it calls the base constructor. Invocations of the proxy methods cause the Invoke method to be called. The Invoke parameters provide information about the method name, parameter types and parameter values for the method invocation on the proxy instance. The Invoke method handles any exceptions so that they can be processed correctly by the proxy and be correctly thrown in the calling code. A return value (or null for void methods) are processed in the return message if no exception was found.

The key for working with a RealProxy instance is how the proxy is created. The proxy instance is not the class that inherits from RealProxy. That class is simply runs the processing of methods called on the proxy instance. The proxy instance is actually created from the RealProxy class using the GetTransparentProxy() method. 

The following is a console application that uses an interface definition to test the above proxy implementation.

{% highlight csharp linenos %}
class Program
{
    static void Main(string[] args)
    {
        TestProxy<ITester> proxy = new TestProxy<ITester>();
        ITester tester = proxy.GetTransparentProxy() as ITester;

        tester.RunTest("This is the message", true, Guid.NewGuid());

        Console.ReadKey();
    }
}

public interface ITester
{
    void RunTest(String message, Boolean flag, Guid marker);
}
{% endhighlight %}

This console application uses the TestProxy&lt;T&gt; class to create a proxy for the ITester interface. Invocations of the proxy instance then cause TestProxy&lt;T&gt;.Invoke to process the invocation. TestProxy in this case will output information about the method invocation to the console. The output for this example is something like the following.
    
{% highlight text linenos %}
Invoking RunTest with the following parameters:
String - This is the message
Boolean - True
Guid - aade8728-9ca3-4919-9080-14090b5b016b
{% endhighlight %}

That’s all there is to it for rolling your own proxy implementation.
