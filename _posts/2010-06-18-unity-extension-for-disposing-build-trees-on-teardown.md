---
title: Unity Extension For Disposing Build Trees On TearDown
categories : .Net
tags : ASP.Net, Unity, WCF
date: 2010-06-18 13:40:50 +10:00
---

A Unity container is used to create objects for use by an application. There are several reasons why it is also responsible for cleaning up the instances that it creates.

* A Unity container owns the instances according to my [Law Of Instance Ownership][0] as the container would typically be near the top of the call stack and higher stack frames don’t hold a reference to container created instances
* Container management in a WCF Service (see [here][1] and [here][2]) or an ASP.Net application (see [here][3]) result in the container being held outside of the scope of the application code
* The container is already responsible for the lifetime management of instances with respect to instance creation

Code outside the container should not be concerned with the lifetime management of instances created by the container for these reasons. 

<!--more-->

The [IUnityContainer][4] interface in Unity provides a [TearDown][5] method which can be used to clean up container instances. TearDown invokes the [IBuilderStrategy.PreTearDown][6] and [IBuilderStrategy.PostTearDown][7] methods on each builder strategy added to the container. Unfortunately the TearDown method has no affect by default as none of the “out of the box” strategies dispose created instances. 

This can be addressed by creating a builder strategy that is hooked up via a Unity extension. This extension will allow for IDisposable instances to be disposed by the container when TearDown is invoked. 

My initial version of this implementation was very simple.

{% highlight csharp %}
using System;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;
    
namespace Neovolve.Toolkit.Unity
{
    public class DisposableStrategyExtension : UnityContainerExtension
    {
        protected override void Initialize()
        {
            Context.Strategies.Add(new DisposableBuilderStrategy(), UnityBuildStage.PostInitialization);
        }
    }
    
    internal class DisposableBuilderStrategy : BuilderStrategy
    {
        public override void PostTearDown(IBuilderContext context)
        {
            base.PostTearDown(context);
    
            IDisposable disposable = context.Existing as IDisposable;
    
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
{% endhighlight %}

Unfortunately this implementation failed miserably. Only the instance passed into the TearDown method was disposed rather than the instance and all its dependencies. It turns out that UnityContainer constructs an IBuilderContext with the instance provided to TearDown regardless of whether the instance was resolved or built up by the container. A [variation on this method][8] is to use property reflection on the instance in order to identify its dependencies.

Both of this implementation and the variation suffer from the same problems.

* The instance passed into TearDown may not have been created by the container (consider a ASP.Net page that has been BuildUp by the container – we can’t dispose this)
* The instance or any of its dependencies may exist in a lifetime manager for reuse in subsequent Resolve/BuildUp operations
* Dependencies injected via constructor that are not visible via properties (or even reflected fields) will not be disposed
* Instances that have been de-referenced can no longer be accessed
* Properties that had an instance injected may have since had a new instance assigned to it
* Instances may have a hierarchy of dependencies so recursion is required to dispose more than the first level dependencies
    
Unfortunately Unity containers do not track the instances they create. There is no stored knowledge in the container of the instances or the relationships between them. The only time that a container holds on to an instance is in a lifetime manager. In this case, the lifetime manager returns the instance to use on Resolve or BuildUp rather than creating a new instance. Typical usages of the lifetime manager are to reuse instances as singletons, unique to threads or unique to Resolve/BuildUp operations.

A builder strategy that will address all these concerns will need to track build trees as they are created. It can then use that knowledge to dispose build trees when an instance is provided to TearDown. An example of a build tree might be:

* ServiceLayer 
 * SecuritySlice 
     * Logger
 * AuditSlice 
     * Auditer
 * BusinessLayer 
     * Logger
     * ServiceDependency 
         * Logger
         * DataCacheSlice 
             * Logger
         * DataLayer 
             * Logger
             * DatabaseAccess 
                  * Logger
    
Tree nodes must use a WeakReference for the tracked instance. This ensures that the build tree does not prevent the instance from being garbage collected while the build tree is still rooted in memory. Consider an instance for a tree node which is nulled out or replaced with another instance in application code before garbage collection occurs. The garbage collector would not be able to clean up the instance for the tree node if the build tree still held a direct reference to it even though application code no longer does. This is important because created instances can go out of scope in application code at any time regardless of if/when TearDown is invoked.

The benefit of tracking and storing a build tree means that if dependencies are de-referenced (meaning a sub tree of instances is orphaned from its parent instance), the TearDown of the build tree would still track the references to the orphaned instances and they can still be disposed. The same benefit also applies to instances injected into a constructor that may not be made available via a property or field on the parent instance.

**The Code**

Build trees are made up build tree nodes which may have 0-many children and a reference back to their parent.

{% highlight csharp %}
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using Microsoft.Practices.ObjectBuilder2;
using Neovolve.Toolkit.Unity.Properties;
    
namespace Neovolve.Toolkit.Unity
{
    internal class BuildTreeItemNode
    {
        public BuildTreeItemNode(
            NamedTypeBuildKey buildKey, Boolean nodeCreatedByContainer, BuildTreeItemNode parentNode)
        {
            Contract.Requires<ArgumentNullException>(buildKey != null);
    
            BuildKey = buildKey;
            NodeCreatedByContainer = nodeCreatedByContainer;
            Parent = parentNode;
            Children = new Collection<BuildTreeItemNode>();
        }
    
        public void AssignInstance(Object instance)
        {
            if (ItemReference != null)
            {
                throw new ArgumentException(Resources.BuildTreeNode_InstanceAlreadyAssigned_ExceptionMessage, "instance");
            }
    
            ItemReference = new WeakReference(instance);
        }
    
        public NamedTypeBuildKey BuildKey
        {
            get;
            private set;
        }
    
        public Collection<BuildTreeItemNode> Children
        {
            get;
            private set;
        }
    
        public WeakReference ItemReference
        {
            get;
            private set;
        }
    
        public Boolean NodeCreatedByContainer
        {
            get;
            private set;
        }
    
        public BuildTreeItemNode Parent
        {
            get;
            private set;
        }
    }
}
{% endhighlight %}

The DisposableStrategyExtension is responsible for attaching the build tree tracker strategy and for disposing all the build trees when the owning container is disposed.

{% highlight csharp %}
using System;
using System.Diagnostics.Contracts;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;
    
namespace Neovolve.Toolkit.Unity
{
    public class DisposableStrategyExtension : UnityContainerExtension, IDisposable
    {
        public DisposableStrategyExtension()
            : this(new BuildTreeTracker())
        {
        }
    
        internal DisposableStrategyExtension(IBuildTreeTracker buildTreeTracker)
        {
            Contract.Requires<ArgumentNullException>(buildTreeTracker != null, "The buildTreeTracker provided is null.");
    
            TreeTracker = buildTreeTracker;
        }
    
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                // Free managed resources
                TreeTracker.DisposeAllTrees();
            }
    
            // Free native resources if there are any.
        }
    
        protected override void Initialize()
        {
            Context.Strategies.Add(TreeTracker, UnityBuildStage.PreCreation);
        }
    
        private IBuildTreeTracker TreeTracker
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The BuildTreeTracker is responsible for creating a build tree in a Resolve or BuildUp operation. This must detect the difference between a Resolve and a BuildUp as this will determine whether the root instance is disposed. 

The tracker will find a build tree for an instance provided to TearDown and run a top down recursive dispose operation against the build tree. It will ignore nodes in the tree that were not created by the container or have been garbage collected and skip over nodes that exist in a lifetime manager. Once a build tree has been disposed, the tracker will also look for build trees that have root instances that have been garbage collected and dispose those build trees as well. Any build tree that the tracker has disposed are then removed to minimize memory usage.

The tracker uses a ThreadStatic to track the current node being built in a build tree as the container may be creating multiple build trees over several threads at the same time. Similarly the tracker needs to protect the list of build trees with suitable locking. The locking in this case uses my [LockReader][9] and [LockWriter][10] classes.

{% highlight csharp %}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Practices.ObjectBuilder2;
using Neovolve.Toolkit.Threading;
    
namespace Neovolve.Toolkit.Unity
{
    internal class BuildTreeTracker : BuilderStrategy, IBuildTreeTracker
    {
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    
        private readonly List<BuildTreeItemNode> _buildTrees = new List<BuildTreeItemNode>();
    
        [ThreadStatic]
        private static BuildTreeItemNode _currentBuildNode;
    
        public void AssignInstanceToCurrentTreeNode(NamedTypeBuildKey buildKey, Object instance)
        {
            if (CurrentBuildNode.BuildKey != buildKey)
            {
                const String ErrorMessageFormat =
                    "Build tree constructed out of order. Build key '{0}' was expected but build key '{1}' was provided.";
                String message = String.Format(CultureInfo.CurrentCulture, ErrorMessageFormat, CurrentBuildNode.BuildKey, buildKey);
    
                throw new InvalidOperationException(message);
            }
    
            CurrentBuildNode.AssignInstance(instance);
        }
    
        public void DisposeAllTrees()
        {
            using (new LockReader(_lock, true))
            {
                for (Int32 index = BuildTrees.Count - 1; index >= 0; index--)
                {
                    BuildTreeItemNode buildTree = BuildTrees[index];
    
                    DisposeTree(null, buildTree);
                }
            }
        }
    
        public BuildTreeItemNode GetBuildTreeForInstance(Object instance)
        {
            using (new LockReader(_lock))
            {
                return BuildTrees.Where(x => x.ItemReference.IsAlive && ReferenceEquals(x.ItemReference.Target, instance)).SingleOrDefault();
            }
        }
    
        public override void PostBuildUp(IBuilderContext context)
        {
            if (context != null)
            {
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
    
        public override void PostTearDown(IBuilderContext context)
        {
            base.PostTearDown(context);
    
            // Get the build tree for this item
            if (context != null)
            {
                BuildTreeItemNode buildTree = GetBuildTreeForInstance(context.Existing);
    
                if (buildTree != null)
                {
                    DisposeTree(context, buildTree);
                }
    
                DisposeDeadTrees(context);
            }
        }
    
        public override void PreBuildUp(IBuilderContext context)
        {
            base.PreBuildUp(context);
    
            if (context != null)
            {
                Boolean nodeCreatedByContainer = context.Existing == null;
    
                BuildTreeItemNode newTreeNode = new BuildTreeItemNode(
                    context.BuildKey, nodeCreatedByContainer, CurrentBuildNode);
    
                if (CurrentBuildNode != null)
                {
                    // This is a child node
                    CurrentBuildNode.Children.Add(newTreeNode);
                }
    
                CurrentBuildNode = newTreeNode;
            }
        }
    
        private void DisposeDeadTrees(IBuilderContext context)
        {
            // Need to enumerate in the reverse order because the trees that are torn down are removed from the set
            using (new LockReader(_lock, true))
            {
                for (Int32 index = BuildTrees.Count - 1; index >= 0; index--)
                {
                    BuildTreeItemNode buildTree = BuildTrees[index];
    
                    if (buildTree.ItemReference.IsAlive == false)
                    {
                        DisposeTree(context, buildTree);
                    }
                }
            }
        }
    
        private void DisposeTree(IBuilderContext context, BuildTreeItemNode buildTree)
        {
            BuildTreeDisposer.DisposeTree(context, buildTree);
    
            using (new LockWriter(_lock))
            {
                BuildTrees.Remove(buildTree);
            }
        }
    
        private static BuildTreeItemNode CurrentBuildNode
        {
            get
            {
                return _currentBuildNode;
            }
    
            set
            {
                _currentBuildNode = value;
            }
        }
    
        public virtual IList<BuildTreeItemNode> BuildTrees
        {
            get
            {
                return _buildTrees;
            }
        }
    }
}
{% endhighlight %}

The BuildTreeTracker calls out to a helper class that is used to dispose build trees.

{% highlight csharp %}
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Practices.ObjectBuilder2;
    
namespace Neovolve.Toolkit.Unity
{
    internal static class BuildTreeDisposer
    {
        public static void DisposeTree(IBuilderContext context, BuildTreeItemNode buildTree)
        {
            TeardownTreeNode(context, buildTree);
        }
    
        private static bool CanTeardownInstance(IBuilderContext context, WeakReference instanceReference)
        {
            return CanTeardownInstance(context, instanceReference.Target);
        }
    
        private static bool CanTeardownInstance(IBuilderContext context, Object instance)
        {
            if (InstanceExistsInLifetimeManager(context, instance))
            {
                // This instance is still stored in the Unity container for future reference
                return false;
            }
    
            return true;
        }
    
        private static void DisposeInstance(WeakReference instanceReference)
        {
            if (instanceReference.IsAlive)
            {
                DisposeInstance(instanceReference.Target);
            }
        }
    
        private static void DisposeInstance(Object instance)
        {
            IDisposable disposable = instance as IDisposable;
    
            if (disposable != null)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    Debug.WriteLine("Object was already disposed");
                }
            }
        }
    
        private static Boolean InstanceExistsInLifetimeManager(IBuilderContext context, Object instance)
        {
            if (context == null)
            {
                return false;
            }
    
            return context.Lifetime.OfType<ILifetimePolicy>().Any(lifetimeManager => ReferenceEquals(lifetimeManager.GetValue(), instance));
        }
    
        private static void TeardownTreeNode(IBuilderContext context, BuildTreeItemNode treeNode)
        {
            // If the parent node can't be torn down then neither can any of the children
            if (CanTeardownInstance(context, treeNode.ItemReference) == false)
            {
                return;
            }
    
            // Only nodes created by the unit container will be disposed
            if (treeNode.NodeCreatedByContainer)
            {
                DisposeInstance(treeNode.ItemReference);
            }
    
            // Recursively call through the child nodes
            for (Int32 index = 0; index < treeNode.Children.Count; index++)
            {
                BuildTreeItemNode child = treeNode.Children[index];
    
                TeardownTreeNode(context, child);
            }
    
            return;
        }
    }
}
{% endhighlight %}

Lastly there is the code to hook up the extension.

{% highlight csharp %}
public void ExampleExtensionUsage()
{
    IDisposableType actual;
    
    using (UnityContainer container = new UnityContainer())
    {
        using (DisposableStrategyExtension disposableStrategyExtension = new DisposableStrategyExtension())
        {
            container.AddExtension(disposableStrategyExtension);
            container.RegisterType(typeof(IDisposableType), typeof(DisposableType), new TransientLifetimeManager());
    
            actual = container.Resolve<IDisposableType>();
    
            container.Teardown(actual);
        }
    }
}
{% endhighlight %}

This can also be done via configuration.

{% highlight xml %}
<?xml version="1.0" ?>
<configuration>
    <configSections>
        <section name="unity"
                    type="Microsoft.Practices.Unity.Configuration.UnityConfigurationSection, Microsoft.Practices.Unity.Configuration" />
    </configSections>
    <unity>
        <containers>
            <container>
                <register type="System.Security.Cryptography.HashAlgorithm, mscorlib"
                            mapTo="System.Security.Cryptography.SHA256CryptoServiceProvider, System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
        <extensions>
            <add type="Neovolve.Toolkit.Unity.DisposableStrategyExtension, Neovolve.Toolkit.Unity" />
        </extensions>
        </container>
        </containers>
    </unity>
</configuration>
{% endhighlight %}

All the code (including help documentation) can be found in my [Toolkit project][11] on CodePlex.

[0]: /2010/06/15/law-of-instance-ownership/
[1]: /2010/05/15/unity-dependency-injection-for-wcf-services-e28093-part-1/
[2]: /2010/05/17/unity-dependency-injections-for-wcf-services-e28093-part-2/
[3]: /2010/05/19/unity-dependency-injection-for-aspnet/
[4]: http://msdn.microsoft.com/en-us/library/microsoft.practices.unity.iunitycontainer(v=PandP.20).aspx
[5]: http://msdn.microsoft.com/en-us/library/microsoft.practices.unity.iunitycontainer.teardown(v=PandP.20).aspx
[6]: http://msdn.microsoft.com/en-us/library/microsoft.practices.objectbuilder2.ibuilderstrategy.preteardown(v=PandP.20).aspx
[7]: http://msdn.microsoft.com/en-us/library/microsoft.practices.objectbuilder2.ibuilderstrategy.postteardown(v=PandP.20).aspx
[8]: http://stackoverflow.com/questions/1443515/unity-to-dispose-of-object
[9]: http://neovolve.codeplex.com/SourceControl/changeset/view/59871#361071
[10]: http://neovolve.codeplex.com/SourceControl/changeset/view/59871#361075
[11]: http://neovolve.codeplex.com/SourceControl/changeset/view/59887#1221625
