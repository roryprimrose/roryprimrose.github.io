---
title: Custom Windows Workflow activity for dependency resolution–Part 3
categories : .Net
date: 2010-09-30 12:34:00 +10:00
---

My [previous post][0] provided the base framework for resolving dependencies, handling persistence and tearing down resolved dependencies in Windows Workflow. This post will provide the custom activity for exposing resolved dependencies to a workflow.

The original implementation of this activity supported resolving a single dependency. It has slowly evolved into one that can support up to 16 dependencies. The reason for this specific number is that the activity leverages the ScheduleAction method on the NativeActivityContext class. This method has overloads that support up to 16 generic arguments. This avoids the developer needing to use nested activities to achieve the same result if only one dependency was supported.

The ScheduleAction method provides the ability for a child activity to be scheduled for execution with one or more delegate arguments. This is the way that ForEach<T&gt; and ParallelForEach<T&gt; activities work. In these cases the argument defines the item being provided in the iterator of the loop behind the activity. This is seen below being defined as the variable “item”.![image][1]

The custom activity defined here has a concept of the number of arguments that it supports at runtime. This is defined at design time using a GenericArgumentCount enum definition. In part this enum is used to support the design-time experience. The activity also uses this value to ensure that only the intended number of generic arguments are provided to the child activity.

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
    }{% endhighlight %}

The custom activity for providing dependency resolution is a generic InstanceResolver class. It defines the 16 generic arguments for the type definitions of the 16 possible dependencies to resolve. It inherits from NativeActivity in order to get access to the NativeActivityContext for scheduling child activity execution and for hooking into activity lifecycle events.

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
    
        [ToolboxBitmap(typeof(InstanceResolver), &quot;brick.png&quot;)]
        [ContentProperty(&quot;Body&quot;)]
        public class InstanceResolver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16&gt; : NativeActivity, IActivityTemplateFactory
        {
            private readonly Variable<List<Guid&gt;&gt; _handlers = new Variable<List<Guid&gt;&gt;();
    
            public InstanceResolver()
            {
                ArgumentCount = GenericArgumentCount.One;
                DisplayName = InstanceResolver.GenerateDisplayName(GetType(), ArgumentCount);
                Body = new ActivityAction
                    <InstanceHandler<T1&gt;, InstanceHandler<T2&gt;, InstanceHandler<T3&gt;, InstanceHandler<T4&gt;, InstanceHandler<T5&gt;, InstanceHandler<T6&gt;, 
                        InstanceHandler<T7&gt;, InstanceHandler<T8&gt;, InstanceHandler<T9&gt;, InstanceHandler<T10&gt;, InstanceHandler<T11&gt;, InstanceHandler<T12&gt;, 
                        InstanceHandler<T13&gt;, InstanceHandler<T14&gt;, InstanceHandler<T15&gt;, InstanceHandler<T16&gt;&gt;
                       {
                           Argument1 = new DelegateInArgument<InstanceHandler<T1&gt;&gt;(&quot;handler1&quot;), 
                           Argument2 = new DelegateInArgument<InstanceHandler<T2&gt;&gt;(&quot;handler2&quot;), 
                           Argument3 = new DelegateInArgument<InstanceHandler<T3&gt;&gt;(&quot;handler3&quot;), 
                           Argument4 = new DelegateInArgument<InstanceHandler<T4&gt;&gt;(&quot;handler4&quot;), 
                           Argument5 = new DelegateInArgument<InstanceHandler<T5&gt;&gt;(&quot;handler5&quot;), 
                           Argument6 = new DelegateInArgument<InstanceHandler<T6&gt;&gt;(&quot;handler6&quot;), 
                           Argument7 = new DelegateInArgument<InstanceHandler<T7&gt;&gt;(&quot;handler7&quot;), 
                           Argument8 = new DelegateInArgument<InstanceHandler<T8&gt;&gt;(&quot;handler8&quot;), 
                           Argument9 = new DelegateInArgument<InstanceHandler<T9&gt;&gt;(&quot;handler9&quot;), 
                           Argument10 = new DelegateInArgument<InstanceHandler<T10&gt;&gt;(&quot;handler10&quot;), 
                           Argument11 = new DelegateInArgument<InstanceHandler<T11&gt;&gt;(&quot;handler11&quot;), 
                           Argument12 = new DelegateInArgument<InstanceHandler<T12&gt;&gt;(&quot;handler12&quot;), 
                           Argument13 = new DelegateInArgument<InstanceHandler<T13&gt;&gt;(&quot;handler13&quot;), 
                           Argument14 = new DelegateInArgument<InstanceHandler<T14&gt;&gt;(&quot;handler14&quot;), 
                           Argument15 = new DelegateInArgument<InstanceHandler<T15&gt;&gt;(&quot;handler15&quot;), 
                           Argument16 = new DelegateInArgument<InstanceHandler<T16&gt;&gt;(&quot;handler16&quot;)
                       };
            }
    
            [DebuggerNonUserCode]
            public Activity Create(DependencyObject target)
            {
                return new InstanceResolver<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16&gt;();
            }
    
            protected override void Abort(NativeActivityAbortContext context)
            {
                DestroyHandlers(context);
            }
    
            protected override void CacheMetadata(NativeActivityMetadata metadata)
            {
                metadata.RequireExtension<InstanceManagerExtension&gt;();
                metadata.AddDelegate(Body);
                metadata.AddDefaultExtensionProvider(() =&gt; new InstanceManagerExtension());
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
    
                InstanceManagerExtension extension = context.GetExtension<InstanceManagerExtension&gt;();
    
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
    
            private InstanceHandler<TH&gt; CreateHandler<TH&gt;(
                ActivityContext context, 
                InstanceManagerExtension extension, 
                InArgument<String&gt; resolutionName, 
                GenericArgumentCount handlerCount, 
                GenericArgumentCount argumentCount)
            {
                if (handlerCount &gt; argumentCount)
                {
                    return null;
                }
    
                String resolveName = resolutionName.Get(context);
                InstanceHandler<TH&gt; handler = extension.CreateInstanceHandler<TH&gt;(resolveName);
                List<Guid&gt; handlerIdList = Handlers.Get(context);
    
                handlerIdList.Add(handler.InstanceHandlerId);
    
                return handler;
            }
    
            private void DestroyHandlers(ActivityContext context)
            {
                InstanceManagerExtension extension = context.GetExtension<InstanceManagerExtension&gt;();
                List<Guid&gt; handlers = Handlers.Get(context);
    
                handlers.ToList().ForEach(extension.DestroyHandler);
            }
    
            private void ExecuteBody(NativeActivityContext context, InstanceManagerExtension extension)
            {
                GenericArgumentCount argumentCount = ArgumentCount;
                List<Guid&gt; handlerIdList = new List<Guid&gt;((Int32)argumentCount);
    
                Handlers.Set(context, handlerIdList);
    
                InstanceHandler<T1&gt; handler1 = CreateHandler<T1&gt;(context, extension, ResolutionName1, GenericArgumentCount.One, argumentCount);
                InstanceHandler<T2&gt; handler2 = CreateHandler<T2&gt;(context, extension, ResolutionName2, GenericArgumentCount.Two, argumentCount);
                InstanceHandler<T3&gt; handler3 = CreateHandler<T3&gt;(context, extension, ResolutionName3, GenericArgumentCount.Three, argumentCount);
                InstanceHandler<T4&gt; handler4 = CreateHandler<T4&gt;(context, extension, ResolutionName4, GenericArgumentCount.Four, argumentCount);
                InstanceHandler<T5&gt; handler5 = CreateHandler<T5&gt;(context, extension, ResolutionName5, GenericArgumentCount.Five, argumentCount);
                InstanceHandler<T6&gt; handler6 = CreateHandler<T6&gt;(context, extension, ResolutionName6, GenericArgumentCount.Six, argumentCount);
                InstanceHandler<T7&gt; handler7 = CreateHandler<T7&gt;(context, extension, ResolutionName7, GenericArgumentCount.Seven, argumentCount);
                InstanceHandler<T8&gt; handler8 = CreateHandler<T8&gt;(context, extension, ResolutionName8, GenericArgumentCount.Eight, argumentCount);
                InstanceHandler<T9&gt; handler9 = CreateHandler<T9&gt;(context, extension, ResolutionName9, GenericArgumentCount.Nine, argumentCount);
                InstanceHandler<T10&gt; handler10 = CreateHandler<T10&gt;(context, extension, ResolutionName10, GenericArgumentCount.Ten, argumentCount);
                InstanceHandler<T11&gt; handler11 = CreateHandler<T11&gt;(context, extension, ResolutionName11, GenericArgumentCount.Eleven, argumentCount);
                InstanceHandler<T12&gt; handler12 = CreateHandler<T12&gt;(context, extension, ResolutionName12, GenericArgumentCount.Twelve, argumentCount);
                InstanceHandler<T13&gt; handler13 = CreateHandler<T13&gt;(context, extension, ResolutionName13, GenericArgumentCount.Thirteen, argumentCount);
                InstanceHandler<T14&gt; handler14 = CreateHandler<T14&gt;(context, extension, ResolutionName14, GenericArgumentCount.Fourteen, argumentCount);
                InstanceHandler<T15&gt; handler15 = CreateHandler<T15&gt;(context, extension, ResolutionName15, GenericArgumentCount.Fifteen, argumentCount);
                InstanceHandler<T16&gt; handler16 = CreateHandler<T16&gt;(context, extension, ResolutionName16, GenericArgumentCount.Sixteen, argumentCount);
    
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
                    <InstanceHandler<T1&gt;, InstanceHandler<T2&gt;, InstanceHandler<T3&gt;, InstanceHandler<T4&gt;, InstanceHandler<T5&gt;, InstanceHandler<T6&gt;, 
                        InstanceHandler<T7&gt;, InstanceHandler<T8&gt;, InstanceHandler<T9&gt;, InstanceHandler<T10&gt;, InstanceHandler<T11&gt;, InstanceHandler<T12&gt;, 
                        InstanceHandler<T13&gt;, InstanceHandler<T14&gt;, InstanceHandler<T15&gt;, InstanceHandler<T16&gt;&gt; Body
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName1
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName10
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName11
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName12
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName13
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName14
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName15
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName16
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName2
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName3
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName4
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName5
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName6
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName7
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName8
            {
                get;
                set;
            }
    
            public InArgument<String&gt; ResolutionName9
            {
                get;
                set;
            }
    
            private Variable<List<Guid&gt;&gt; Handlers
            {
                get
                {
                    return _handlers;
                }
            }
        }
    }{% endhighlight %}

The bulk of the code in this class is the definition of the resolution name properties. There is a resolution name property for each of the possible 16 instance resolutions. It defines the ArgumentCount property that determines how many instance resolutions are going to be processed by the activity. The class also defines a Body property holds a reference to the child activity that will be executed with the InstanceHandler references.

The class also maintains a List<Guid&gt; variable that keep track of the all the InstanceHandlerId values created for the activity. This list of values is used to destroy resolved instances using the InstanceManagerExtension when the activity completes, is aborted or is cancelled. Persistence is managed by the InstanceManagerExtension as outlined in the previous post.

The InstanceResolver also implements the IActivityTemplateFactory interface. This interface defines the Create method that allows the activity to provide an activity definition when it is dragged from the toolbox to the designer. This is used to preconfigure an InstanceResolver instance with default values.

One of the issues with the support for generic activities that define more than one generic argument is that the developer gets a prompt for the generic argument types when the activity is dragged onto the design surface.![image][2]

This is a usability issue for the InstanceResolver class as the developer using it will not often need all 16 arguments for their workflow. Unfortunately this implementation forces them to identify a type definition for all 16 arguments as this is what defines the InstanceResolve activity being used.

On a side note, there is a way around this for activity types that define one generic argument. Decorating the activity with the DefaultTypeArgumentAttribute allows you to specify the default type. ForEach<T&gt; and ParallelForEach<T&gt; use this to define the type of int. This is why the developer does not see the above prompt when dropping these activities onto the design surface. Unfortunately this attribute was only designed to support a single generic argument.

The workaround for this usability issue is to use some indirection. Another activity that implements IActivityTemplateFactory can be used to produce this result. This is where the non-generic InstanceResolver class comes into play.

    namespace Neovolve.Toolkit.Workflow.Activities
    {
        using System;
        using System.Activities;
        using System.Activities.Presentation;
        using System.Diagnostics;
        using System.Diagnostics.Contracts;
        using System.Drawing;
        using System.Windows;
    
        [ToolboxBitmap(typeof(InstanceResolver), &quot;brick.png&quot;)]
        public sealed class InstanceResolver : NativeActivity, IActivityTemplateFactory
        {
            public static String GenerateDisplayName(Type activityType, GenericArgumentCount argumentCount)
            {
                Contract.Requires<ArgumentNullException&gt;(activityType != null);
    
                Type[] genericArguments = activityType.GetGenericArguments();
                String displayName = &quot;InstanceResolver<&quot; + genericArguments[0].Name;
                Int32 maxArguments = (Int32)argumentCount;
    
                if (maxArguments == 1)
                {
                    return displayName + &quot;&gt;&quot;;
                }
    
                for (Int32 index = 1; index < maxArguments; index++)
                {
                    displayName += &quot;, &quot; + genericArguments[index].Name;
                }
    
                return displayName + &quot;&gt;&quot;;
            }
    
            [DebuggerNonUserCode]
            public Activity Create(DependencyObject target)
            {
                return
                    new InstanceResolver
                        <Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object&gt;();
            }
    
            protected override void Execute(NativeActivityContext context)
            {
                throw new NotSupportedException(&quot;This activity is intended to be used as a design time activity only. Use InstanceResolver instead.&quot;);
            }
        }
    }{% endhighlight %}

This activity is used to create a generic InstanceResolver instance by defining the type of Object for all 16 of the generic arguments. The user will not be prompted to define these types when dropping this activity on the designer. The result as far as the designer is concerned is that the workflow will contain the generic InstanceResolver rather than the non-generic InstanceResolver. The toolbox contains both of these activities so the developer still has a choice about how to get this activity onto the designer.![image][3]

Why was this IActivityTemplateFactory.Create method not implemented in this way in the generic InstanceResolver? 

The reason is that IActivityTemplateFactory and its Create method are defined against an instance of the activity rather than as a static method. The Create method on the generic InstanceResolver can only be invoked once all 16 generic arguments have been defined such that the activity instance can be created. Using a non-generic InstanceResolver activity gets around this issue as no generic types need defining before it is dropped onto a designer. The non-generic InstanceResolver is essentially a proxy into the generic InstanceResolver for the design-time experience.

This post has built upon the underlying framework for resolving dependencies in a workflow and has provided the implementation for a custom activity. The next post will look at the designer support for this activity.

[0]: /post/2010/09/29/Custom-Windows-Workflow-activity-for-dependency-resolutione28093Part-2.aspx
[1]: //blogfiles/image_29.png
[2]: //blogfiles/image_30.png
[3]: //blogfiles/image_31.png
