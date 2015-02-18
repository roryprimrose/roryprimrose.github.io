---
title: Tracking lazy loaded build trees in Unity
categories : .Net
tags : Dependency Injection, Unity
date: 2010-07-27 13:31:38 +10:00
---

I recently [posted a Unity extension][0] for disposing build trees created by Resolve and BuildUp actions. Unity has a feature that supports lazy loading injected dependencies that the original disposal extension didn’t cater for.

Consider the following example.{% highlight csharp linenos %}
using System;
using Microsoft.Practices.Unity;
using Neovolve.Toolkit.Unity;
    
namespace Neovolve.LazyInjectionTesting
{
    class Program
    {
        static void Main(String[] args)
        {
            IUnityContainer container = new UnityContainer();
    
            container.AddExtension(new DisposableStrategyExtension());
            container.RegisterType(typeof(ITester), typeof(Tester));
    
            Root root = container.Resolve<Root>();
    
            container.Teardown(root);
    
            Console.WriteLine(root.TestInstance.IsDisposed);
            Console.ReadKey();
        }
    }
    
    public class Root
    {
        public Root(ITester tester)
        {
            TestInstance = tester;
        }
    
        public ITester TestInstance
        {
            get;
            private set;
        }
    }
    
    public interface ITester
    {
        void DoSomething();
    
        Boolean IsDisposed
        {
            get;
        }
    }
    
    public class Tester : ITester, IDisposable
    {
        public void DoSomething()
        {
        }
    
        public void Dispose()
        {
            IsDisposed = true;
        }
    
        public bool IsDisposed
        {
            get;
            private set;
        }
    }
}
{% endhighlight %}

This example resolves an instance of Root that has a dependency of type ITester. The build tree tracked by the disposal extension for this example is:

* Root 
    * Tester
    
Tester exposes a property that indicates whether the instance has been disposed. The disposal extension will ensure that all items in the build tree are disposed when the container is told to tear down the root instance. The Tester.IsDisposed property returns True when this console code is run.

Lazy loading dependencies is achieved by changing the injection type from T to Func<T>. This can be done in the above example by changing the Root constructor from public Root(ITester tester) to public Root(Func<ITester> tester). The example above now looks like the following. This delegate then needs to be invoked to get Unity to resolve the dependency instance.{% highlight csharp linenos %}
using System;
using Microsoft.Practices.Unity;
using Neovolve.Toolkit.Unity;
    
namespace Neovolve.LazyInjectionTesting
{
    class Program
    {
        static void Main(String[] args)
        {
            IUnityContainer container = new UnityContainer();
    
            container.AddExtension(new DisposableStrategyExtension());
            container.RegisterType(typeof(ITester), typeof(Tester));
    
            Root root = container.Resolve<Root>();
            ITester testInstance = root.TestInstance();
    
            container.Teardown(root);
    
            Console.WriteLine(testInstance.IsDisposed);
            Console.ReadKey();
        }
    }
    
    public class Root
    {
        public Root(Func<ITester> tester)
        {
            TestInstance = tester;
        }
    
        public Func<ITester> TestInstance
        {
            get;
            private set;
        }
    }
    
    public interface ITester
    {
        void DoSomething();
    
        Boolean IsDisposed
        {
            get;
        }
    }
    
    public class Tester : ITester, IDisposable
    {
        public void DoSomething()
        {
        }
    
        public void Dispose()
        {
            IsDisposed = true;
        }
    
        public bool IsDisposed
        {
            get;
            private set;
        }
    }
}
{% endhighlight %}

Unity will create a delegate for Func<ITester> in order to call the constructor on Root. The delegate is a function that will resolve ITester from the container when the delegate is invoked. This means that the resolution of Root occurs at a different time to the resolution of ITester. The DisposableStrategyExtension is now tracking two build trees rather than one. The build tree that contains Root does not contain ITester and therefore the disposal strategy can’t dispose the dependency when container.TearDown(root) is invoked. The console application now returns False because the DisposalStrategyExtension does not understand that the two build trees are related.

I have updated the DisposableStrategyExtension to correctly track build trees of these lazy loaded dependencies. 

The PostBuildUp method is the second half of the logic that creates a build tree for each Unity build operation in this extension. The update to track lazy loaded build trees is to detect that the dependency created (context.Existing) is a delegate. If this is the case then&#160; a wrapper delegate is created and returned instead.{% highlight csharp linenos %}
public override void PostBuildUp(IBuilderContext context)
{
    if (context != null)
    {
        // Check if the item created is Func<T> for lazy loaded dependency injection
        if (context.Existing is Delegate)
        {
            context.Existing = CreateTrackedDeferredResolution((Delegate)context.Existing);
        }
    
        AssignInstanceToCurrentTreeNode(context.BuildKey, context.Existing);
    
        BuildTreeItemNode parentNode = CurrentBuildNode.Parent;
    
        if (parentNode == null)
        {
            // This is the end of the creation of the root node
            using (new LockWriter(_lock))
            {
                BuildTrees.Add(CurrentBuildNode);
            }
        }
    
        // Move the current node back up to the parent
        // If this is the top level node, this will set the current node back to null
        CurrentBuildNode = parentNode;
    }
    
    base.PostBuildUp(context);
}
{% endhighlight %}

The wrapper delegate will now be injected as the dependency rather than the Unity delegate. The wrapper delegate is created using the CreateTrackedDeferredResolution method. This method has some defensive coding to ensure that we are actually dealing with a Func<T> delegate. The wrapper delegate it creates is a function call out to a Resolve method on a custom DeferredResolutionTracker<T> class.{% highlight csharp linenos %}
public Delegate CreateTrackedDeferredResolution(Delegate originalDeferredFunction)
{
    Type delegateType = originalDeferredFunction.GetType();
    
    if (delegateType.IsGenericType == false)
    {
        return originalDeferredFunction;
    }
    
    Type genericDelegateType = delegateType.GetGenericTypeDefinition();
    
    if (genericDelegateType.Equals(typeof(Func<>)) == false)
    {
        return originalDeferredFunction;
    }
    
    // This looks like a lazy loaded delegate for dependency injection
    // We need to redirect this through another method to manage the original build tree 
    // when the delegate invocation creates more instances
    Type[] genericArguments = delegateType.GetGenericArguments();
    
    if (genericArguments.Length > 1)
    {
        return originalDeferredFunction;
    }
    
    Type genericArgument = genericArguments[0];
    Type deferredTrackerType = typeof(DeferredResolutionTracker<>);
    Type[] typeArguments = new[]
                                {
                                    genericArgument
                                };
    Type genericDeferredTrackerType = deferredTrackerType.MakeGenericType(typeArguments);
    MethodInfo resolveMethod = genericDeferredTrackerType.GetMethod("Resolve");
    Object[] trackerArguments = new Object[]
                                    {
                                        originalDeferredFunction, this, CurrentBuildNode
                                    };
    Object resolvedTracker = Activator.CreateInstance(genericDeferredTrackerType, trackerArguments);
    
    return Delegate.CreateDelegate(delegateType, resolvedTracker, resolveMethod);
}
{% endhighlight %}

The DeferredResolutionTracker class takes in the original delegate, the parent node in the original build tree (the instance that the delegate is injected into) and a reference to the collection of build trees. Its Resolve method then invokes the original delegate to lazy load the dependency instance from Unity. It then searches the collection of build trees to find the build tree of that resolution action. That build tree is then removed from the collection and added as a child of the original parent node from the original build tree. {% highlight csharp linenos %}
using System;
using System.Diagnostics.Contracts;
using System.Linq;
    
namespace Neovolve.Toolkit.Unity
{
    internal class DeferredResolutionTracker<T>
    {
        public DeferredResolutionTracker(
            Func<T> resolutionFunction, IBuildTreeTracker originalTracker, BuildTreeItemNode parentNode)
        {
            Contract.Requires<ArgumentNullException>(resolutionFunction != null, "No resolution function provided.");
            Contract.Requires<ArgumentNullException>(originalTracker != null, "No original tracker provided.");
            Contract.Requires<ArgumentNullException>(parentNode != null, "No parent node provided.");
    
            ResolutionFunction = resolutionFunction;
            OriginalTracker = originalTracker;
            ParentNode = parentNode;
        }
    
        public T Resolve()
        {
            T resolvedInstance = ResolutionFunction();
    
            BuildTreeItemNode deferredBuildTree =
                    OriginalTracker.BuildTrees.Where(x => IsDeferredBuildTreeReference(x, resolvedInstance)).
                        FirstOrDefault();
    
            if (deferredBuildTree == null)
            {
                return resolvedInstance;
            }
    
            // We have found the deferred build tree for the instance created by the deferred resolution function
            OriginalTracker.RemoveBuildTree(deferredBuildTree);
    
            // Add the build tree as a child to the parent node
            ParentNode.Children.Add(deferredBuildTree);
    
            return resolvedInstance;
        }
    
        private static Boolean IsDeferredBuildTreeReference(BuildTreeItemNode buildTreeNode, T resolvedInstance)
        {
            if (buildTreeNode == null)
            {
                return false;
            }
    
            if (buildTreeNode.ItemReference.IsAlive == false)
            {
                return false;
            }
    
            if (ReferenceEquals(buildTreeNode.ItemReference.Target, resolvedInstance))
            {
                return true;
            }
    
            return false;
        }
    
        private IBuildTreeTracker OriginalTracker
        {
            get;
            set;
        }
    
        private BuildTreeItemNode ParentNode
        {
            get;
            set;
        }
    
        private Func<T> ResolutionFunction
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The extension will now have a build tree for the root instance from the example above that has a reference to the lazy loaded dependency. This means that the extension will now correctly tear down the root instance when container.TearDown(root) is invoked. The above example with this fix now writes True to the console for the lazy loaded example.

The updated code for the DisposalStrategyExtension can be found [here][1] in my Toolkit project on CodePlex.

[0]: /post/2010/06/18/Unity-Extension-For-Disposing-Build-Trees-On-TearDown.aspx
[1]: http://neovolve.codeplex.com/SourceControl/changeset/view/63473#1224390
