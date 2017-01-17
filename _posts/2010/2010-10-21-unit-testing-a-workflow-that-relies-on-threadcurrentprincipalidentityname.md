---
title: Unit testing a workflow that relies on Thread.CurrentPrincipal.Identity.Name
categories: .Net
tags: Unit Testing
date: 2010-10-21 14:03:05 +10:00
---

This is a bit of a curly one. I have a workflow that is mostly abstracted from security concerns in that authentication and authorization logic has already been processed. The workflow does however need to get the name of the current user via Thread.CurrentPrincipal.Identity.Name. Unfortunately this returns an empty string when executing the workflow directly in a unit test.

The reason for the workflow not having access to the current principal is that workflows are executed on a new thread. The principal associated with the workflow thread is determined according to the PrincipalPolicy assigned to the AppDomain. By default the AppDomain will return an unauthenticated GenericPrincipal. See my [Thread identity propagation][0] post from a few years ago for the background information.

<!--more-->

Fortunately there are two easy workarounds for this problem. Both workarounds make changes to the configuration of the AppDomain and how it manages the current principal. This will work because the workflow is executed within the same AppDomain as the unit test, just on a different thread.

These workarounds leverage the logic that Reflector shows in AppDomain.GetThreadPrincipal. This method gets called by Thread.CurrentPrincipal when the current principal is null. Fortunately this isn't a problem for executing a workflow as the workflow engine does not assign a principal for the new workflow thread.

```csharp
internal IPrincipal GetThreadPrincipal()
{
    IPrincipal principal = null;
    IPrincipal principal2;
    lock (this)
    {
        if (this._DefaultPrincipal == null)
        {
            switch (this._PrincipalPolicy)
            {
                case PrincipalPolicy.UnauthenticatedPrincipal:
                    principal = new GenericPrincipal(new GenericIdentity(&quot;&quot;, &quot;&quot;), new string[] { &quot;&quot; });
                    goto Label_0073;
    
                case PrincipalPolicy.NoPrincipal:
                    principal = null;
                    goto Label_0073;
    
                case PrincipalPolicy.WindowsPrincipal:
                    principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                    goto Label_0073;
            }
            principal = null;
        }
        else
        {
            principal = this._DefaultPrincipal;
        }
    Label_0073:
        principal2 = principal;
    }
    return principal2;
}
```

## 1. Change the AppDomain PrincipalPolicy

```csharp
// Configure the app domain to put the current windows credential into the thread when Thread.CurrentPrincipal is invoked
AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
```

Changing the PrincipalPolicy on the AppDomain will result in the current WindowsIdentity being returned if the thread did not already have a principal assigned.

There are a few caveats for this workaround to be aware of:

* Thread.CurrentPrincipal will end up with the current users WindowsPrincipal.
* No flexibility for other principal types
* No flexibility for custom principal values
    
## 2. Assign a default principal against the AppDomain

Changing the AppDomain PrincipalPolicy will work however the outcome locks you into testing against the current WindowsIdentity which is not ideal. The AppDomain class also has the ability to define a default principal. This overrides the PrincipalPolicy as seen in the above GetThreadPrincipal method. Assigning the principal in this way provides a lot more control over the principal that is used in the workflow thread.

```csharp
AppDomain.CurrentDomain.SetThreadPrincipal(newPrincipal);
```

There is a caveat for this workaround as well. The default principal for the app domain can only be set once. You will need to have all your tests running against the same principal to avoid any issues.

## Clean up and test method usage

It is important to clean up any changes made when the test either completes or fails. This is where a handle context/scope style class comes into play.

```csharp
namespace Neovolve.Jabiru.Server.TestSupport
{
    using System;
    using System.Security.Policy;
    using System.Security.Principal;
    using System.Threading;
    
    public class TestUserContext : IDisposable
    {
        private readonly IPrincipal _originalPrincipal;
    
        public TestUserContext(IPrincipal newPrincipal)
        {
            _originalPrincipal = Thread.CurrentPrincipal;
    
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.NoPrincipal);
    
            Thread.CurrentPrincipal = null;
    
            IPrincipal defaultAppDomainPrincipal = Thread.CurrentPrincipal;
    
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
    
            if (defaultAppDomainPrincipal != null)
            {
                // Check if the app domain default principal has been set to another principal
                const String AppDomainHasAlreadyBeenAssignedADifferentPrincipal = &quot;App domain has already been assigned a different principal&quot;;
    
                if (newPrincipal.GetType().Equals(defaultAppDomainPrincipal.GetType()) == false)
                {
                    throw new PolicyException(AppDomainHasAlreadyBeenAssignedADifferentPrincipal);
                }
    
                if (newPrincipal.Identity.GetType().Equals(defaultAppDomainPrincipal.Identity.GetType()) == false)
                {
                    throw new PolicyException(AppDomainHasAlreadyBeenAssignedADifferentPrincipal);
                }
    
                if (newPrincipal.Identity.Name != defaultAppDomainPrincipal.Identity.Name)
                {
                    throw new PolicyException(AppDomainHasAlreadyBeenAssignedADifferentPrincipal);
                }
            }
            else
            {
                AppDomain.CurrentDomain.SetThreadPrincipal(newPrincipal);
            }
    
            Thread.CurrentPrincipal = newPrincipal;
        }
    
        public void Dispose()
        {
            Thread.CurrentPrincipal = _originalPrincipal;
        }
    }
}
```

This class will configure the AppDomain to use a provided principal. It will also attempt to detect if the app domain is already configured with a different default principal. The Dispose method will then restore the original principal back onto the thread.

```csharp
using (TestUsers.DefaultUser.CreateContext())
{
    IDictionary<String, Object> outputParameters = ActivityInvoker.Invoke(target, inputParameters);
    
    itemSetId = (Guid)outputParameters[&quot;ItemSetId&quot;];
}
```

The unit test method can then use this context in a using statement with the help of some nice syntactic sugar provided by extension methods. The using statement here will ensure that the current thread is restored by TestUserContext.Dispose regardless of whether an exception is thrown.

**Updated:** Found issue with AppDomain default principal only being allowed to be set once. Updated TestUserContext to attempt to cater for this.

[0]: /2008/08/12/thread-identity-propagation/