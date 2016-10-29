---
title: Getting meaningful exceptions from WF
categories: .Net
tags: WF
date: 2010-08-18 13:33:00 +10:00
---

Overall I love the changes made to WF4. The latest workflow support is a huge leap forward from the 3.x versions. Unfortunately both versions suffer the same issues regarding feedback for the developer when exceptions are thrown. 

Consider the following simple workflow example.

<!--more-->

![image][0]

```csharp
namespace WorkflowConsoleApplication1
{
    using System;
    using System.Activities;
    class Program
    {
        static void Main(string[] args)
        { 
            try
            {
                WorkflowInvoker.Invoke(new Workflow1());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.ReadKey();
        }
    }
}
```

This example provides the following output.

```text
System.InvalidOperationException: Something went wrong
   at System.Activities.WorkflowApplication.Invoke(Activity activity, IDictionar
y`2 inputs, WorkflowInstanceExtensionManager extensions, TimeSpan timeout)
   at System.Activities.WorkflowInvoker.Invoke(Activity workflow, TimeSpan timeo
ut, WorkflowInstanceExtensionManager extensions)
   at System.Activities.WorkflowInvoker.Invoke(Activity workflow)
   at WorkflowConsoleApplication1.Program.Main(String[] args) in e:\users\profil
edr\documents\visual studio 2010\Projects\WorkflowConsoleApplication2\WorkflowCo
nsoleApplication2\Program.cs:line 12
```

Exceptions thrown from the workflow engine identify the exception type and message but do not contain an accurate stack trace. Exceptions are caught by the workflow engine and re-thrown when WorkflowInvoker.Invoke is used. This means that the stack trace identifies where the exception has been re-thrown rather than where the exception was originally raised. 

Debugging WF activities with this constraint is very difficult. Trying to debug your workflows gets exponentially harder with increasing complexity of the workflow system and it's underlying components. This is compounded when the exception (such as a NullReferenceException) doesn't provide enough information by itself to pinpoint where the exception was thrown.

The solution to this is in two parts. The first improvement is to provide more detail in the stack trace and the second is to identify the activity hierarchy involved. 

## Enhancing the stack trace

Using the throw statement on an existing exception instance wipes out any existing stack trace information. This is why you should never catch ex then throw ex, you should always just use the throw statement by itself. This isn’t possible in the case of the workflow engine as it has caught the exception on a different thread and has no choice but to throw the exception instance back on the original calling thread. 

The trick here is to get the exception to preserve the original stack trace when it is re-thrown. Exception has a method called InternalPreserveStackTrace that will do this for us. Reflection must be used here because the method is marked as internal. (Starting from .NET 4.5 you can use ExceptionDispatchInfo class instead of InternalPreserveStackTrace)

The first thing to change is that we need to use WorkflowApplication directly rather than WorkflowInvoker. WorkflowInvoker is great for simplicity but does not provide direct access to the thrown exception before it is re-thrown. Using WorkflowApplication provides the ability to hook the OnUnhandledException action where the exception is available. The reflected InternalPreserveStackTrace method can then be invoked on the exception instance before it is re-thrown.

```csharp
namespace WorkflowConsoleApplication1
{
    using System;
    using System.Activities;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    internal class ActivityInvoker
    {
        private static readonly MethodInfo _preserveStackTraceMethod = GetExceptionPreserveMethod();
        public static IDictionary<String, Object> Invoke(Activity activity, IDictionary<String, Object> inputParameters = null)
        {
            if (inputParameters == null)
            {
                inputParameters = new Dictionary<String, Object>();
            }
            WorkflowApplication application = new WorkflowApplication(activity, inputParameters);
            Exception thrownException = null;
            IDictionary<String, Object> outputParameters = new Dictionary<String, Object>();
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            application.OnUnhandledException = (WorkflowApplicationUnhandledExceptionEventArgs arg) =>
            {
                // Preserve the stack trace in this exception
                // This is a hack into the Exception.InternalPreserveStackTrace method that allows us to re-throw and preserve the call stack
                _preserveStackTraceMethod.Invoke(arg.UnhandledException, null);
                thrownException = arg.UnhandledException;
                return UnhandledExceptionAction.Terminate;
            };
            application.Completed = (WorkflowApplicationCompletedEventArgs obj) =>
            {
                waitHandle.Set();
                outputParameters = obj.Outputs;
            };
            application.Aborted = (WorkflowApplicationAbortedEventArgs obj) => waitHandle.Set();
            application.Idle = (WorkflowApplicationIdleEventArgs obj) => waitHandle.Set();
            application.PersistableIdle = (WorkflowApplicationIdleEventArgs arg) =>
            {
                waitHandle.Set();
                return PersistableIdleAction.Persist;
            };
            application.Unloaded = (WorkflowApplicationEventArgs obj) => waitHandle.Set();
            application.Run();
            waitHandle.WaitOne();
            if (thrownException != null)
            {
                throw thrownException;
            }
            return outputParameters;
        }
        private static MethodInfo GetExceptionPreserveMethod()
        {
            return typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
```

Changing the example program code to use this ActivityInvoker class rather than WorkflowInvoker now provides some more detailed stack trace information.

```text
System.InvalidOperationException: Something went wrong
   at System.Activities.Statements.Throw.Execute(CodeActivityContext context)
   at System.Activities.CodeActivity.InternalExecute(ActivityInstance instance,
ActivityExecutor executor, BookmarkManager bookmarkManager)
   at System.Activities.ActivityInstance.Execute(ActivityExecutor executor, Book
markManager bookmarkManager)
   at System.Activities.Runtime.ActivityExecutor.ExecuteActivityWorkItem.Execute
Body(ActivityExecutor executor, BookmarkManager bookmarkManager, Location result
Location)
   at WorkflowConsoleApplication1.ActivityInvoker.Invoke(Activity activity, IDic
tionary`2 inputParameters) in e:\users\profiledr\documents\visual studio 2010\Pr
ojects\WorkflowConsoleApplication2\WorkflowConsoleApplication2\ActivityInvoker.c
s:line 82
   at WorkflowConsoleApplication1.Program.Main(String[] args) in e:\users\profil
edr\documents\visual studio 2010\Projects\WorkflowConsoleApplication2\WorkflowCo
nsoleApplication2\Program.cs:line 11
```

The stack trace with preservation now includes the stack frames between ActivityInvoker.Invoke and the class that originally threw the exception. This is particularly helpful when the exception was thrown in an underlying component. Unfortunately the architecture of WF does not often provide much of a stack trace in itself. Preserving the stack trace in this simple scenario has added helpful information but not enough to make it really easy to debug.

## Identifying the activity hierarchy

Identifying the hierarchy of activities being executed will add further debugging assistance. The Activity class in the 3.x version of WF contained a public Parent property. You could recursively traverse up the chain of parent activities to create a hierarchy of activities to provide this information. This property is still there in version 4.0 but is now internal. Using reflection is again the only option for obtaining this information.
    
A tweak to the exception logic in the ActivityInvoker class will hook into the Activity.Parent property to calculate the activity hierarchy.
    
```csharp
namespace WorkflowConsoleApplication1
{
    using System;
    using System.Activities;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    internal class ActivityInvoker
    {
        private static readonly PropertyInfo _parentProperty = GetActivityParentProperty();
        private static readonly MethodInfo _preserveStackTraceMethod = GetExceptionPreserveMethod();
        public static Exception CreateActivityFailureException(Exception exception, Activity source)
        {
            // Create the hierarchy of activity names
            Activity activity = source;
            StringBuilder builder = new StringBuilder(exception.Message);
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("Workflow exception throw from the following activity stack:");
            while (activity != null)
            {
                builder.AppendLine(activity + " - " + activity.GetType().FullName);
                activity = _parentProperty.GetValue(activity, null) as Activity;
            }
            return new ActivityFailureException(builder.ToString(), exception);
        }
        public static IDictionary<String, Object> Invoke(Activity activity, IDictionary<String, Object> inputParameters = null)
        {
            if (inputParameters == null)
            {
                inputParameters = new Dictionary<String, Object>();
            }
            WorkflowApplication application = new WorkflowApplication(activity, inputParameters);
            Exception thrownException = null;
            IDictionary<String, Object> outputParameters = new Dictionary<String, Object>();
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            application.OnUnhandledException = (WorkflowApplicationUnhandledExceptionEventArgs arg) =>
            {
                // Preserve the stack trace in this exception
                // This is a hack into the Exception.InternalPreserveStackTrace method that allows us to re-throw and preserve the call stack
                _preserveStackTraceMethod.Invoke(arg.UnhandledException, null);
                thrownException = CreateActivityFailureException(arg.UnhandledException, arg.ExceptionSource);
                return UnhandledExceptionAction.Terminate;
            };
            application.Completed = (WorkflowApplicationCompletedEventArgs obj) =>
            {
                waitHandle.Set();
                outputParameters = obj.Outputs;
            };
            application.Aborted = (WorkflowApplicationAbortedEventArgs obj) => waitHandle.Set();
            application.Idle = (WorkflowApplicationIdleEventArgs obj) => waitHandle.Set();
            application.PersistableIdle = (WorkflowApplicationIdleEventArgs arg) =>
            {
                waitHandle.Set();
                return PersistableIdleAction.Persist;
            };
            application.Unloaded = (WorkflowApplicationEventArgs obj) => waitHandle.Set();
            application.Run();
            waitHandle.WaitOne();
            if (thrownException != null)
            {
                throw thrownException;
            }
            return outputParameters;
        }
        private static PropertyInfo GetActivityParentProperty()
        {
            return typeof(Activity).GetProperty("Parent", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic);
        }
        private static MethodInfo GetExceptionPreserveMethod()
        {
            return typeof(Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
```

The change here calculates the activity hierarchy and appends it to the exception message. A custom ActivityFailureException is used to help identify that this an exception handled from the workflow engine. The original exception is still available via the InnerException property. 

The output of the program now provides the extended stack trace and the activity hierarchy.

```text
WorkflowConsoleApplication1.ActivityFailureException: Something went wrong
Workflow exception thrown from the following activity stack:
1.5: Throw - System.Activities.Statements.Throw
1.4: Fourth Sequence - System.Activities.Statements.Sequence
1.3: Third Sequence - System.Activities.Statements.Sequence
1.2: Second Sequence - System.Activities.Statements.Sequence
1.1: First Sequence - System.Activities.Statements.Sequence
1: Workflow1 - WorkflowConsoleApplication1.Workflow1
 ---> System.InvalidOperationException: Something went wrong
   at System.Activities.Statements.Throw.Execute(CodeActivityContext context)
   at System.Activities.CodeActivity.InternalExecute(ActivityInstance instance,
ActivityExecutor executor, BookmarkManager bookmarkManager)
   at System.Activities.ActivityInstance.Execute(ActivityExecutor executor, Book
markManager bookmarkManager)
   at System.Activities.Runtime.ActivityExecutor.ExecuteActivityWorkItem.Execute
Body(ActivityExecutor executor, BookmarkManager bookmarkManager, Location result
Location)
   --- End of inner exception stack trace ---
   at WorkflowConsoleApplication1.ActivityInvoker.Invoke(Activity activity, IDic
tionary`2 inputParameters) in e:\users\profiledr\documents\visual studio 2010\Pr
ojects\WorkflowConsoleApplication2\WorkflowConsoleApplication2\ActivityInvoker.c
s:line 80
   at WorkflowConsoleApplication1.Program.Main(String[] args) in e:\users\profil
edr\documents\visual studio 2010\Projects\WorkflowConsoleApplication2\WorkflowCo
nsoleApplication2\Program.cs:line 11
```
    
The combination of these two pieces of information will now allow you to quickly identify where the exception is being thrown in or through your workflow assembly.

[0]: /files/image_25.png
