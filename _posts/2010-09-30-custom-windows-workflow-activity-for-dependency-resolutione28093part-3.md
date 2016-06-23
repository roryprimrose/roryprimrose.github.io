---
title: Custom Windows Workflow activity for dependency resolution–Part 3
categories: .Net
date: 2010-09-30 12:34:00 +10:00
---

My [previous post][0] provided the base framework for resolving dependencies, handling persistence and tearing down resolved dependencies in Windows Workflow. This post will provide the custom activity for exposing resolved dependencies to a workflow.

The original implementation of this activity supported resolving a single dependency. It has slowly evolved into one that can support up to 16 dependencies. The reason for this specific number is that the activity leverages the ScheduleAction method on the NativeActivityContext class. This method has overloads that support up to 16 generic arguments. This avoids the developer needing to use nested activities to achieve the same result if only one dependency was supported.

The ScheduleAction method provides the ability for a child activity to be scheduled for execution with one or more delegate arguments. This is the way that ForEach&lt;T&gt; and ParallelForEach&lt;T&gt; activities work. In these cases the argument defines the item being provided in the iterator of the loop behind the activity. This is seen below being defined as the variable “item”.![image][1]

<!--more-->

The custom activity defined here has a concept of the number of arguments that it supports at runtime. This is defined at design time using a GenericArgumentCount enum definition. In part this enum is used to support the design-time experience. The activity also uses this value to ensure that only the intended number of generic arguments are provided to the child activity.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow
{ 
    public enum GenericArgumentCount
    {
        One = 1, 
        Two, 
        Three, 
        Four, 
        Five, 
        Six, 
        Seven, 
        Eight, 
        Nine, 
        Ten, 
        Eleven, 
        Twelve, 
        Thirteen, 
        Fourteen, 
        Fifteen, 
        Sixteen
    }
}
{% endhighlight %}

The custom activity for providing dependency resolution is a generic InstanceResolver class. It defines the 16 generic arguments for the type definitions of the 16 possible dependencies to resolve. It inherits from NativeActivity in order to get access to the NativeActivityContext for scheduling child activity execution and for hooking into activity lifecycle events.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Windows;
    using System.Windows.Markup;
    using Neovolve.Toolkit.Workflow.Extensions;
    
    [ToolboxBitmap(typeof(InstanceResolver), "brick.png")]
    [ContentProperty("Body")]
    public class InstanceResolver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : NativeActivity, IActivityTemplateFactory
    {
        private readonly Variable<List<Guid>> _handlers = new Variable<List<Guid>>();
    
        public InstanceResolver()
        {
            ArgumentCount = GenericArgumentCount.One;
            DisplayName = InstanceResolver.GenerateDisplayName(GetType(), ArgumentCount);
            Body = new ActivityAction
                <InstanceHandler<T1>, InstanceHandler<T2>, InstanceHandler<T3>, InstanceHandler<T4>, InstanceHandler<T5>, InstanceHandler<T6>, 
                    InstanceHandler<T7>, InstanceHandler<T8>, InstanceHandler<T9>, InstanceHandler<T10>, InstanceHandler<T11>, InstanceHandler<T12>, 
                    InstanceHandler<T13>, InstanceHandler<T14>, InstanceHandler<T15>, InstanceHandler<T16>>
                    {
                        Argument1 = new DelegateInArgument<InstanceHandler<T1>>("handler1"), 
                        Argument2 = new DelegateInArgument<InstanceHandler<T2>>("handler2"), 
                        Argument3 = new DelegateInArgument<InstanceHandler<T3>>("handler3"), 
                        Argument4 = new DelegateInArgument<InstanceHandler<T4>>("handler4"), 
                        Argument5 = new DelegateInArgument<InstanceHandler<T5>>("handler5"), 
                        Argument6 = new DelegateInArgument<InstanceHandler<T6>>("handler6"), 
                        Argument7 = new DelegateInArgument<InstanceHandler<T7>>("handler7"), 
                        Argument8 = new DelegateInArgument<InstanceHandler<T8>>("handler8"), 
                        Argument9 = new DelegateInArgument<InstanceHandler<T9>>("handler9"), 
                        Argument10 = new DelegateInArgument<InstanceHandler<T10>>("handler10"), 
                        Argument11 = new DelegateInArgument<InstanceHandler<T11>>("handler11"), 
                        Argument12 = new DelegateInArgument<InstanceHandler<T12>>("handler12"), 
                        Argument13 = new DelegateInArgument<InstanceHandler<T13>>("handler13"), 
                        Argument14 = new DelegateInArgument<InstanceHandler<T14>>("handler14"), 
                        Argument15 = new DelegateInArgument<InstanceHandler<T15>>("handler15"), 
                        Argument16 = new DelegateInArgument<InstanceHandler<T16>>("handler16")
                    };
        }
    
        [DebuggerNonUserCode]
        public Activity Create(DependencyObject target)
        {
            return new InstanceResolver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>();
        }
    
        protected override void Abort(NativeActivityAbortContext context)
        {
            DestroyHandlers(context);
        }
    
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.RequireExtension<InstanceManagerExtension>();
            metadata.AddDelegate(Body);
            metadata.AddDefaultExtensionProvider(() => new InstanceManagerExtension());
            metadata.AddImplementationVariable(_handlers);
    
            base.CacheMetadata(metadata);
        }
    
        protected override void Cancel(NativeActivityContext context)
        {
            context.CancelChildren();
    
            DestroyHandlers(context);
        }
    
        protected override void Execute(NativeActivityContext context)
        {
            if (CanExecute() == false)
            {
                return;
            }
    
            InstanceManagerExtension extension = context.GetExtension<InstanceManagerExtension>();
    
            ExecuteBody(context, extension);
        }
    
        private Boolean CanExecute()
        {
            if (Body == null)
            {
                return false;
            }
    
            if (Body.Handler == null)
            {
                return false;
            }
    
            return true;
        }
    
        private InstanceHandler<TH> CreateHandler<TH>(
            ActivityContext context, 
            InstanceManagerExtension extension, 
            InArgument<String> resolutionName, 
            GenericArgumentCount handlerCount, 
            GenericArgumentCount argumentCount)
        {
            if (handlerCount > argumentCount)
            {
                return null;
            }
    
            String resolveName = resolutionName.Get(context);
            InstanceHandler<TH> handler = extension.CreateInstanceHandler<TH>(resolveName);
            List<Guid> handlerIdList = Handlers.Get(context);
    
            handlerIdList.Add(handler.InstanceHandlerId);
    
            return handler;
        }
    
        private void DestroyHandlers(ActivityContext context)
        {
            InstanceManagerExtension extension = context.GetExtension<InstanceManagerExtension>();
            List<Guid> handlers = Handlers.Get(context);
    
            handlers.ToList().ForEach(extension.DestroyHandler);
        }
    
        private void ExecuteBody(NativeActivityContext context, InstanceManagerExtension extension)
        {
            GenericArgumentCount argumentCount = ArgumentCount;
            List<Guid> handlerIdList = new List<Guid>((Int32)argumentCount);
    
            Handlers.Set(context, handlerIdList);
    
            InstanceHandler<T1> handler1 = CreateHandler<T1>(context, extension, ResolutionName1, GenericArgumentCount.One, argumentCount);
            InstanceHandler<T2> handler2 = CreateHandler<T2>(context, extension, ResolutionName2, GenericArgumentCount.Two, argumentCount);
            InstanceHandler<T3> handler3 = CreateHandler<T3>(context, extension, ResolutionName3, GenericArgumentCount.Three, argumentCount);
            InstanceHandler<T4> handler4 = CreateHandler<T4>(context, extension, ResolutionName4, GenericArgumentCount.Four, argumentCount);
            InstanceHandler<T5> handler5 = CreateHandler<T5>(context, extension, ResolutionName5, GenericArgumentCount.Five, argumentCount);
            InstanceHandler<T6> handler6 = CreateHandler<T6>(context, extension, ResolutionName6, GenericArgumentCount.Six, argumentCount);
            InstanceHandler<T7> handler7 = CreateHandler<T7>(context, extension, ResolutionName7, GenericArgumentCount.Seven, argumentCount);
            InstanceHandler<T8> handler8 = CreateHandler<T8>(context, extension, ResolutionName8, GenericArgumentCount.Eight, argumentCount);
            InstanceHandler<T9> handler9 = CreateHandler<T9>(context, extension, ResolutionName9, GenericArgumentCount.Nine, argumentCount);
            InstanceHandler<T10> handler10 = CreateHandler<T10>(context, extension, ResolutionName10, GenericArgumentCount.Ten, argumentCount);
            InstanceHandler<T11> handler11 = CreateHandler<T11>(context, extension, ResolutionName11, GenericArgumentCount.Eleven, argumentCount);
            InstanceHandler<T12> handler12 = CreateHandler<T12>(context, extension, ResolutionName12, GenericArgumentCount.Twelve, argumentCount);
            InstanceHandler<T13> handler13 = CreateHandler<T13>(context, extension, ResolutionName13, GenericArgumentCount.Thirteen, argumentCount);
            InstanceHandler<T14> handler14 = CreateHandler<T14>(context, extension, ResolutionName14, GenericArgumentCount.Fourteen, argumentCount);
            InstanceHandler<T15> handler15 = CreateHandler<T15>(context, extension, ResolutionName15, GenericArgumentCount.Fifteen, argumentCount);
            InstanceHandler<T16> handler16 = CreateHandler<T16>(context, extension, ResolutionName16, GenericArgumentCount.Sixteen, argumentCount);
    
            context.ScheduleAction(
                Body, 
                handler1, 
                handler2, 
                handler3, 
                handler4, 
                handler5, 
                handler6, 
                handler7, 
                handler8, 
                handler9, 
                handler10, 
                handler11, 
                handler12, 
                handler13, 
                handler14, 
                handler15, 
                handler16, 
                OnCompleted, 
                null);
        }
    
        private void OnCompleted(ActivityContext context, ActivityInstance completedInstance)
        {
            DestroyHandlers(context);
        }
    
        [Browsable(false)]
        public GenericArgumentCount ArgumentCount
        {
            get;
            set;
        }
    
        [Browsable(false)]
        public
            ActivityAction
                <InstanceHandler<T1>, InstanceHandler<T2>, InstanceHandler<T3>, InstanceHandler<T4>, InstanceHandler<T5>, InstanceHandler<T6>, 
                    InstanceHandler<T7>, InstanceHandler<T8>, InstanceHandler<T9>, InstanceHandler<T10>, InstanceHandler<T11>, InstanceHandler<T12>, 
                    InstanceHandler<T13>, InstanceHandler<T14>, InstanceHandler<T15>, InstanceHandler<T16>> Body
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName1
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName10
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName11
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName12
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName13
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName14
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName15
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName16
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName2
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName3
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName4
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName5
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName6
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName7
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName8
        {
            get;
            set;
        }
    
        public InArgument<String> ResolutionName9
        {
            get;
            set;
        }
    
        private Variable<List<Guid>> Handlers
        {
            get
            {
                return _handlers;
            }
        }
    }
}
{% endhighlight %}

The bulk of the code in this class is the definition of the resolution name properties. There is a resolution name property for each of the possible 16 instance resolutions. It defines the ArgumentCount property that determines how many instance resolutions are going to be processed by the activity. The class also defines a Body property holds a reference to the child activity that will be executed with the InstanceHandler references.

The class also maintains a List&lt;Guid&gt; variable that keep track of the all the InstanceHandlerId values created for the activity. This list of values is used to destroy resolved instances using the InstanceManagerExtension when the activity completes, is aborted or is cancelled. Persistence is managed by the InstanceManagerExtension as outlined in the previous post.

The InstanceResolver also implements the IActivityTemplateFactory interface. This interface defines the Create method that allows the activity to provide an activity definition when it is dragged from the toolbox to the designer. This is used to preconfigure an InstanceResolver instance with default values.

One of the issues with the support for generic activities that define more than one generic argument is that the developer gets a prompt for the generic argument types when the activity is dragged onto the design surface.![image][2]

This is a usability issue for the InstanceResolver class as the developer using it will not often need all 16 arguments for their workflow. Unfortunately this implementation forces them to identify a type definition for all 16 arguments as this is what defines the InstanceResolve activity being used.

On a side note, there is a way around this for activity types that define one generic argument. Decorating the activity with the DefaultTypeArgumentAttribute allows you to specify the default type. ForEach&lt;T&gt; and ParallelForEach&lt;T&gt; use this to define the type of int. This is why the developer does not see the above prompt when dropping these activities onto the design surface. Unfortunately this attribute was only designed to support a single generic argument.

The workaround for this usability issue is to use some indirection. Another activity that implements IActivityTemplateFactory can be used to produce this result. This is where the non-generic InstanceResolver class comes into play.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Drawing;
    using System.Windows;
    
    [ToolboxBitmap(typeof(InstanceResolver), "brick.png")]
    public sealed class InstanceResolver : NativeActivity, IActivityTemplateFactory
    {
        public static String GenerateDisplayName(Type activityType, GenericArgumentCount argumentCount)
        {
            Contract.Requires<ArgumentNullException>(activityType != null);
    
            Type[] genericArguments = activityType.GetGenericArguments();
            String displayName = "InstanceResolver<" + genericArguments[0].Name;
            Int32 maxArguments = (Int32)argumentCount;
    
            if (maxArguments == 1)
            {
                return displayName + ">";
            }
    
            for (Int32 index = 1; index < maxArguments; index++)
            {
                displayName += ", " + genericArguments[index].Name;
            }
    
            return displayName + ">";
        }
    
        [DebuggerNonUserCode]
        public Activity Create(DependencyObject target)
        {
            return
                new InstanceResolver
                    <Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>();
        }
    
        protected override void Execute(NativeActivityContext context)
        {
            throw new NotSupportedException("This activity is intended to be used as a design time activity only. Use InstanceResolver instead.");
        }
    }
}
{% endhighlight %}

This activity is used to create a generic InstanceResolver instance by defining the type of Object for all 16 of the generic arguments. The user will not be prompted to define these types when dropping this activity on the designer. The result as far as the designer is concerned is that the workflow will contain the generic InstanceResolver rather than the non-generic InstanceResolver. The toolbox contains both of these activities so the developer still has a choice about how to get this activity onto the designer.![image][3]

Why was this IActivityTemplateFactory.Create method not implemented in this way in the generic InstanceResolver? 

The reason is that IActivityTemplateFactory and its Create method are defined against an instance of the activity rather than as a static method. The Create method on the generic InstanceResolver can only be invoked once all 16 generic arguments have been defined such that the activity instance can be created. Using a non-generic InstanceResolver activity gets around this issue as no generic types need defining before it is dropped onto a designer. The non-generic InstanceResolver is essentially a proxy into the generic InstanceResolver for the design-time experience.

This post has built upon the underlying framework for resolving dependencies in a workflow and has provided the implementation for a custom activity. The next post will look at the designer support for this activity.

[0]: /2010/09/29/custom-windows-workflow-activity-for-dependency-resolutione28093part-2/
[1]: /files/image_29.png
[2]: /files/image_30.png
[3]: /files/image_31.png
