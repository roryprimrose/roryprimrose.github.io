---
title: Thread identity propagation
categories: .Net
date: 2008-08-12 11:30:53 +10:00
---

I have been writing up some instrumentation documentation where I have been explaining the use of TraceFilter implementations. I gave an example of creating a TraceFilter that filters trace events from the TraceListener based on the identity of the executing thread. The issue here is that a TraceListener implementation may be writing its data on a different thread to the one that invoked the trace event. 
I had always assumed that when you create a new thread, the identity of the creating thread is used as the identity of the new thread. There is a lot of danger in assumptions though. While it's just an example, I needed to prove that my TraceFilter code wouldn't be invalid if a TraceListener did use threads for its work.

After firing up Reflector, I poked around Thread.CurrentPrincipal code. As expected, the principal is stored in the context data of the thread (LogicalCallContext via CallContext). What was interesting about the code is that if the call context doesn't contain a principal, it then consults the AppDomain. In the internal AppDomain.GetThreadPrincipal() method, the code runs a switch around a [PrincipalPolicy][0] value. I hadn't come across PrincipalPolicy before so this was an interesting discovery.

<!--more-->

The policy allows for an AppDomain to determine the default identity to use for threads. This is set by calling [AppDomain.SetPrincipalPolicy()][1], but its value is only valid until an identity is explicitly assigned to a thread. From that point on, that thread and any threads that it creates use the explicitly defined identity. The way to revert this back to using the policy on the AppDomain is to set the identity on the current thread to null.

So my assumption was correct. Identities on threads do get propagated to their created threads.

Here is an example:

```csharp
using System;
using System.Security.Principal;
using System.Threading;
     
namespace ConsoleApplication1
{
    internal class Program
    {
        private static void Main(String[] args)
        {
            Thread newThread = new Thread(ThreadTest);
            newThread.Start("Default AppDomain");
            newThread.Join();
     
            Console.WriteLine();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.NoPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain NoPrincipal");
            newThread.Join();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain UnauthenticatedPrincipal");
            newThread.Join();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain WindowsPrincipal");
            newThread.Join();
     
            Console.WriteLine();
     
            Thread.CurrentPrincipal = new GenericPrincipal(new GenericIdentity("NewIdentity"), null);
     
            Console.WriteLine("Assigned new identity to thread");
            Console.WriteLine();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.NoPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain NoPrincipal");
            newThread.Join();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain UnauthenticatedPrincipal");
            newThread.Join();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain WindowsPrincipal");
            newThread.Join();
     
            Console.WriteLine();
     
            Thread.CurrentPrincipal = null;
     
            Console.WriteLine("Removed identity from thread");
            Console.WriteLine();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.NoPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain NoPrincipal");
            newThread.Join();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain UnauthenticatedPrincipal");
            newThread.Join();
     
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            newThread = new Thread(ThreadTest);
            newThread.Start("AppDomain WindowsPrincipal");
            newThread.Join();
     
            Console.ReadKey();
        }
     
        private static void ThreadTest(Object value)
        {
            IPrincipal principal = Thread.CurrentPrincipal;
            String name = "null";
     
            if (principal != null
                && principal.Identity != null)
            {
                name = principal.Identity.Name;
     
                if (String.IsNullOrEmpty(name))
                {
                    name = "empty";
                }
            }
     
            Console.WriteLine(value + " - " + name);
        }
    }
}
    
```

The results in the following output (with the windows account removed):

```text
Default AppDomain - empty 

AppDomain NoPrincipal - null

AppDomain UnauthenticatedPrincipal - empty
          
AppDomain WindowsPrincipal - [WindowsAccountRemoved] 

Assigned new identity to thread 

AppDomain NoPrincipal - NewIdentity
          
AppDomain UnauthenticatedPrincipal - NewIdentity
          
AppDomain WindowsPrincipal - NewIdentity 

Removed identity from thread 

AppDomain NoPrincipal - null
          
AppDomain UnauthenticatedPrincipal - empty
          
AppDomain WindowsPrincipal - [WindowsAccountRemoved]
```

[0]: http://msdn.microsoft.com/en-us/library/system.security.principal.principalpolicy.aspx
[1]: http://msdn.microsoft.com/en-us/library/system.appdomain.setprincipalpolicy.aspx
