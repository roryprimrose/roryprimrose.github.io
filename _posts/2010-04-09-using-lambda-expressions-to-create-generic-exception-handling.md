---
title: Using Lambda expressions to create generic exception handling
tags: Unity
date: 2010-04-09 13:50:06 +10:00
---

I’m writing a system that exposes several services. Each service is created using Unity and has many dependency slices including business layers, data access layers, caching, logging and exception management.

As I was writing the exception management slice, the code started to get incredibly duplicated. Each method in the class began to look like this:

<!--more-->

```csharp
public String DoSomething(String withSomeValue, Boolean andAnotherValue)
{
    try
    {
        return Dependency.DoSomething(withSomeValue, andAnotherValue);
    }
    catch (BusinessFailureException)
    {
        // Ignore this exception
        throw;
    }
    catch (SystemFailureException)
    {
        // Ignore this exception
        throw;
    }
    catch (Exception ex)
    {
        LogEvent record = new LogEvent(LogEventCategory.Exception, "We failed to do something", EventLogEntryType.Error);
    
        record.Parameters.Add("SomeValue", withSomeValue);
        record.Parameters.Add("AnotherValue", andAnotherValue);
        record.Parameters.Add("Exception", ex);
    
        LogWriter.WriteEvent(record);
    
        // Throw this as a system failure exception to indicate that the exception has been handled by the system
        throw new SystemFailureException(ex);
    }
}
```

When there are several methods on the interface/class, this ends up being a maintenance nightmare and really breaks the DRY principle.

It then occurred to me that I could use lambda expressions and pass the functions around as Func&lt;T&gt; and Action parameters. This resulted in code that looked like the following.

```csharp
public String DoSomething(String withSomeValue, Boolean andAnotherValue)
{
    return InvokeWithLogging(() => Dependency.DoSomething(withSomeValue, andAnotherValue), "Failed to do something");
}
    
private T InvokeWithLogging<T>(Func<T> method, String failureMessage)
{
    try
    {
        return method.Invoke();
    }
    catch (BusinessFailureException)
    {
        // Ignore this exception
        throw;
    }
    catch (SystemFailureException)
    {
        // Ignore this exception
        throw;
    }
    catch (Exception ex)
    {
        LogEvent record = new LogEvent(LogEventCategory.Exception, failureMessage, EventLogEntryType.Error);
    
        List<FieldInfo> fields = method.Target.GetType().GetFields().Skip(1).ToList();
    
        fields.ForEach(f => record.Parameters.Add(f.Name, f.GetValue(method.Target)));
    
        record.Parameters.Add("Exception", ex);
    
        LogWriter.WriteEvent(record);
    
        // Throw this as a system failure exception to indicate that the exception has been handled by the system
        throw new SystemFailureException(ex);
    }
}
```

The next bit of duplication was that I have void methods and methods that return a value. I first created a duplicate InvokeWIthLogging method that accepted Action method and duplicated the exception management and logging. I wasn’t happy with this duplication so I figured that the ultimate outcome would be to get the void method to be passed to the InvokeWithLogging(Func&lt;T&gt;) method.

So the big question is how do you pass an Action as a Func&lt;T&gt;. This is really easy. Here is the final code.

```csharp
public void DoSomethingElse(String withThisValue)
{
    InvokeWithLogging(() => Dependency.DoSomethingElse(withThisValue), "Failed to do something");
}
    
public String DoSomething(String withSomeValue, Boolean andAnotherValue)
{
    return InvokeWithLogging(() => Dependency.DoSomething(withSomeValue, andAnotherValue), "Failed to do something");
}
    
private void InvokeWithLogging(Action method, String failureMessage)
{
    InvokeWithLogging(() => MethodReturnWrapper(method), failureMessage);
}
    
private static Object MethodReturnWrapper(Action method)
{
    method.Invoke();
    
    return null;
}
    
private T InvokeWithLogging<T>(Func<T> method, String failureMessage)
{
    try
    {
        return method.Invoke();
    }
    catch (BusinessFailureException)
    {
        // Ignore this exception
        throw;
    }
    catch (SystemFailureException)
    {
        // Ignore this exception
        throw;
    }
    catch (Exception ex)
    {
        LogEvent record = new LogEvent(LogEventCategory.Exception, failureMessage, EventLogEntryType.Error);
    
        List<FieldInfo> fields = method.Target.GetType().GetFields().Skip(1).ToList();
    
        fields.ForEach(f => record.Parameters.Add(f.Name, f.GetValue(method.Target)));
    
        record.Parameters.Add("Exception", ex);
    
        LogWriter.WriteEvent(record);
    
        // Throw this as a system failure exception to indicate that the exception has been handled by the system
        throw new SystemFailureException(ex);
    }
}
```

The trick here is to pass the Action method to a function that simply returns null. This function can then be used to pass to the InvokeWithLogging(Func&lt;T&gt;) method. Job done.

Given that this is very generic code, this can then be pushed out into a helper class that all service exception management and logging slices can use.


