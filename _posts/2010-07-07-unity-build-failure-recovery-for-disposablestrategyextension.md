---
title: Unity build failure recovery for DisposableStrategyExtension
categories : .Net, Applications
tags : Unity
date: 2010-07-07 22:41:31 +10:00
---

I [posted recently][0] about my Unity extension that disposes build trees when a container tears down an instance it previously created. The extension makes an assumption that a Unity build operation will either succeed completely or fail completely. Normally you expect this to be the case. I have however now come up with an edge case. 

I am writing another Unity extension that adds support for injecting proxy instances as dependencies. Part of this design allows for custom proxy handlers to be injected. Defining a custom proxy handler is optional and can either be specifically defined in configuration at the injection definition or be automatically resolved by the Unity container from contextual information. If a custom proxy isn’t defined or can’t be automatically resolved then the injection will fall back to a default proxy handler type. 

This new extension creates a child build context within the build context of the proxy dependency being created by the Unity container. The child build context will attempt to create the proxy handler just in case the container is configured for it. Unity will throw an exception if the proxy handler isn’t defined. In this case however, the build operation needs to continue rather than failing like you would normally expect. 

This becomes a problem for the DisposableStrategyExtension. It tracks build trees as they are created using the BuildTreeTracker class mentioned in the [previous post][0]. It tracks when a new build context is started and when it is finished. Each iteration of this process tracks the current tree node being built by the context. Each child build context encountered results in a new child node that is simply added to the current node. The child node then becomes tracked as the current node being built. Unfortunately there is no fault tolerance for when an item fails to be built when the overall build operation can continue. 

Consider the following build tree for example.

* Root 
  * ChildA 
      * DependencyA
      * DependencyB
  * ChildB

The PreBuildUp method in BuildTreeTracker gets invoked before each tree node is created and PostBuildUp gets invoked after each node (and it’s children) are completely built. The BuildTreeTracker class tracks the start and end of each item being built via these two methods. What happens if the creation of ChildA fails without any recovery action? Assuming the build process handles the build failure exception and continues the build, the BuildTreeTracker identifies the current node as ChildA when it goes to create ChildB (PreBuildUp) however it should actually point to Root. This is because the tracker re-points the current node in the build tree back to the parent of the current node on PostBuildUp. Unfortunately PostBuildUp does not get invoked when a build context fails.

Enter the IRequiresRecovery interface. This interface allows for some recovery operation to be invoked when a build context has failed to create an instance.{% highlight csharp linenos %}
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Practices.ObjectBuilder2;
    
namespace Neovolve.Toolkit.Unity
{
    internal class BuildTreeRecovery : IRequiresRecovery
    {
        public BuildTreeRecovery(IBuilderContext context, BuildTreeItemNode failedNode, Action<BuildTreeItemNode> failureAction)
        {
            Contract.Requires<ArgumentNullException>(context != null, "The context parameter is null");
            Contract.Requires<ArgumentNullException>(failedNode != null, "The failedNode parameter is null");
    
            Context = context;
            FailedNode = failedNode;
            FailureAction = failureAction;
        }
    
        public void Recover()
        {
            try
            {
                if (FailureAction != null)
                {
                    FailureAction(FailedNode);
                }
    
                BuildTreeItemNode parentNode = FailedNode.Parent;
    
                BuildTreeDisposer.DisposeTree(Context, FailedNode);
    
                if (parentNode != null)
                {
                    parentNode.Children.Remove(FailedNode);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Recovery failed: " + ex);
            }
        }
    
        protected IBuilderContext Context
        {
            get;
            private set;
        }
    
        protected BuildTreeItemNode FailedNode
        {
            get;
            private set;
        }
    
        protected Action<BuildTreeItemNode> FailureAction
        {
            get;
            private set;
        }
    }
}
{% endhighlight %}

The BuildTreeRecovery class allows for a custom action to be invoked. It then cleans up any partial build trees that were successfully created under the failed node and then removes it from its parent. If you assume in the previous example that DependencyA and DependencyB were successfully created before ChildA failed, then BuildTreeRecovery will ensure that ChildA, DependencyA and DependencyB are all disposed and that the failed node (ChildA) is then removed from the build tree.

The final piece missing here is that the BuildTreeTracker class will still have a reference to ChildA being the current node instead of Root when PreBuildUp is invoked for ChildB. This is where the custom action of the recovery class comes in. The BuildTreeTracker.PreBuildUp method has been updated to create the BuildTreeRecovery class and use it in a recovery stack. The action passed to its constructor is a lambda expression that repairs the assignment of the current node in the build tree to be the parent of the failed node.{% highlight csharp linenos %}
public override void PreBuildUp(IBuilderContext context)
{
    base.PreBuildUp(context);
    
    if (context == null)
    {
        return;
    }
    
    Boolean nodeCreatedByContainer = context.Existing == null;
    BuildTreeItemNode newTreeNode = new BuildTreeItemNode(context.BuildKey, nodeCreatedByContainer, CurrentBuildNode);
    
    if (CurrentBuildNode != null)
    {
        // This is a child node
        CurrentBuildNode.Children.Add(newTreeNode);
    }
    
    CurrentBuildNode = newTreeNode;
    
    BuildTreeRecovery recovery = new BuildTreeRecovery(context, newTreeNode, failedNode => CurrentBuildNode = failedNode.Parent);
    
    context.RecoveryStack.Add(recovery);
}
{% endhighlight %}

Completing the example proposed above, the resultant build tree when ChildA fails to build would be:

* Root 
    * ChildB
    
While using this recovery class satisfies my optional build requirement for proxy injection, it provides an arguably more important feature. Build trees might fail at anytime and applications may recover from or simply ignore the failures and continue. The partial build trees created in these attempts may have created some references that require disposal but this can never occur because the original BuildTreeTracker code would simply lose track of nodes successfully created in a failed build tree. This solution will ensure that these instances are disposed immediately if the build operation fails.

[0]: /post/2010/06/18/Unity-Extension-For-Disposing-Build-Trees-On-TearDown.aspx
