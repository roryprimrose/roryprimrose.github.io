---
title: Custom DisposalScope activity
categories : .Net
tags : WF
date: 2011-03-10 09:51:30 +10:00
---

The [previous post][0] outlined the issues with working with unmanaged and IDisposable resources in WF. To recap, the issues with these resources in WF are:

* Persistence
* Restoration after persistence
* Error handling 
  * Messy WF experience to handle this correctly

A custom activity can handle these issues in a much more elegant way than the partially successful implementation in the previous post. 

The design goals of this activity are:

* take in a resource of a generic type (limited to IDisposable)
* enforce a [No Persist Zone][1] to avoid the persistence issue
* dispose the resource on successful execution
* dispose the resource on a faulted child activity
* as always, provide adequate designer support

The code implementation of the custom DisposalScope<T&gt; activity handles these goals.

    namespace Neovolve.Toolkit.Workflow.Activities
    {
        using System;
        using System.Activities;
        using System.Activities.Presentation;
        using System.Activities.Statements;
        using System.Activities.Validation;
        using System.Collections.ObjectModel;
        using System.ComponentModel;
        using System.Drawing;
        using System.Windows;
        using System.Windows.Markup;
        using Neovolve.Toolkit.Workflow.Properties;
    
        [ToolboxBitmap(typeof(ExecuteBookmark), &quot;bin_closed.png&quot;)]
        [DefaultTypeArgument(typeof(IDisposable))]
        [ContentProperty(&quot;Body&quot;)]
        public sealed class DisposalScope<T&gt; : NativeActivity<T&gt;, IActivityTemplateFactory where T : class, IDisposable
        {
            public DisposalScope()
            {
                NoPersistHandle = new Variable<NoPersistHandle&gt;();
            }
    
            public Activity Create(DependencyObject target)
            {
                ActivityAction<T&gt; body = new ActivityAction<T&gt;
                                         {
                                             Handler = new Sequence(), 
                                             Argument = new DelegateInArgument<T&gt;(&quot;instance&quot;)
                                         };
    
                return new DisposalScope<T&gt;
                       {
                           Body = body
                       };
            }
    
            protected override void CacheMetadata(NativeActivityMetadata metadata)
            {
                metadata.AddDelegate(Body);
                metadata.AddImplementationVariable(NoPersistHandle);
    
                RuntimeArgument instanceArgument = new RuntimeArgument(&quot;Instance&quot;, typeof(T), ArgumentDirection.In, true);
    
                metadata.Bind(Instance, instanceArgument);
    
                Collection<RuntimeArgument&gt; arguments = new Collection<RuntimeArgument&gt;
                                                        {
                                                            instanceArgument
                                                        };
    
                metadata.SetArgumentsCollection(arguments);
    
                if (Body == null || Body.Handler == null)
                {
                    ValidationError validationError = new ValidationError(Resources.Activity_NoChildActivitiesDefined, true, &quot;Body&quot;);
    
                    metadata.AddValidationError(validationError);
                }
            }
    
            protected override void Execute(NativeActivityContext context)
            {
                NoPersistHandle noPersistHandle = NoPersistHandle.Get(context);
    
                noPersistHandle.Enter(context);
    
                T instance = Instance.Get(context);
    
                context.ScheduleAction(Body, instance, OnCompletion, OnFaulted);
            }
    
            private void DestroyInstance(NativeActivityContext context)
            {
                T instance = Instance.Get(context);
    
                if (instance == null)
                {
                    return;
                }
    
                try
                {
                    instance.Dispose();
    
                    Instance.Set(context, null);
                }
                catch (ObjectDisposedException)
                {
                    // Ignore this exception
                }
            }
    
            private void OnCompletion(NativeActivityContext context, ActivityInstance completedinstance)
            {
                DestroyInstance(context);
    
                NoPersistHandle noPersistHandle = NoPersistHandle.Get(context);
    
                noPersistHandle.Exit(context);
            }
    
            private void OnFaulted(NativeActivityFaultContext faultcontext, Exception propagatedexception, ActivityInstance propagatedfrom)
            {
                DestroyInstance(faultcontext);
    
                NoPersistHandle noPersistHandle = NoPersistHandle.Get(faultcontext);
    
                noPersistHandle.Exit(faultcontext);
            }
    
            [Browsable(false)]
            public ActivityAction<T&gt; Body
            {
                get;
                set;
            }
    
            [DefaultValue((String)null)]
            [RequiredArgument]
            public InArgument<T&gt; Instance
            {
                get;
                set;
            }
    
            private Variable<NoPersistHandle&gt; NoPersistHandle
            {
                get;
                set;
            }
        }
    }{% endhighlight %}

The DisposalScope<T&gt; activity enforces a no persist zone. Attempts at persistence by a child activity will result in throwing an exception. The resource is released on either a successful or fault outcome. There is some validation in the activity that ensures that a Body activity has been defined. The activity also uses the IActivityTemplateFactory to create the activity with a Sequence activity for its Body property when it is created on the WF designer.

The designer of the activity handles most of the design time experience.

    <sap:ActivityDesigner x:Class=&quot;Neovolve.Toolkit.Workflow.Design.Presentation.DisposalScopeDesigner&quot;
                          xmlns=&quot;http://schemas.microsoft.com/winfx/2006/xaml/presentation&quot;
                          xmlns:x=&quot;http://schemas.microsoft.com/winfx/2006/xaml&quot;
                          xmlns:s=&quot;clr-namespace:System;assembly=mscorlib&quot;
                          xmlns:sap=&quot;clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation&quot;
                          xmlns:sapv=&quot;clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation&quot;
                          xmlns:conv=&quot;clr-namespace:System.Activities.Presentation.Converters;assembly=System.Activities.Presentation&quot;
                          xmlns:sadm=&quot;clr-namespace:System.Activities.Presentation.Model;assembly=System.Activities.Presentation&quot;
                          xmlns:ComponentModel=&quot;clr-namespace:System.ComponentModel;assembly=WindowsBase&quot;
                          xmlns:ntw=&quot;clr-namespace:Neovolve.Toolkit.Workflow;assembly=Neovolve.Toolkit.Workflow&quot;
                          xmlns:ntwd=&quot;clr-namespace:Neovolve.Toolkit.Workflow.Design&quot;&gt;
        <sap:ActivityDesigner.Icon&gt;
            <DrawingBrush&gt;
                <DrawingBrush.Drawing&gt;
                    <ImageDrawing&gt;
                        <ImageDrawing.Rect&gt;
                            <Rect Location=&quot;0,0&quot;
                                  Size=&quot;16,16&quot;&gt;
                            </Rect&gt;
                        </ImageDrawing.Rect&gt;
                        <ImageDrawing.ImageSource&gt;
                            <BitmapImage UriSource=&quot;bin_closed.png&quot;&gt;</BitmapImage&gt;
                        </ImageDrawing.ImageSource&gt;
                    </ImageDrawing&gt;
                </DrawingBrush.Drawing&gt;
            </DrawingBrush&gt;
        </sap:ActivityDesigner.Icon&gt;
        <sap:ActivityDesigner.Resources&gt;
            <conv:ModelToObjectValueConverter x:Key=&quot;modelItemConverter&quot;
                                              x:Uid=&quot;sadm:ModelToObjectValueConverter_1&quot; /&gt;
            <conv:ArgumentToExpressionConverter x:Key=&quot;expressionConverter&quot; /&gt;
            <DataTemplate x:Key=&quot;Collapsed&quot;&gt;
                <TextBlock HorizontalAlignment=&quot;Center&quot;
                           FontStyle=&quot;Italic&quot;
                           Foreground=&quot;Gray&quot;&gt;
                    Double-click to view
                </TextBlock&gt;
            </DataTemplate&gt;
            <DataTemplate x:Key=&quot;Expanded&quot;&gt;
                <StackPanel Orientation=&quot;Vertical&quot;&gt;
                    
                        <StackPanel Orientation=&quot;Horizontal&quot;&gt;
                            <sapv:TypePresenter Width=&quot;120&quot;
                                            Margin=&quot;5&quot;
                                            AllowNull=&quot;false&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Filter=&quot;DisposalTypeFilter&quot;
                                            Label=&quot;Target type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ArgumentType, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                            <TextBox Text=&quot;{Binding ModelItem.Body.Argument.Name}&quot;
                                 MinWidth=&quot;80&quot; /&gt;
                            <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.Instance, Mode=TwoWay, Converter={StaticResource expressionConverter}, ConverterParameter=In}&quot;
                                                OwnerActivity=&quot;{Binding ModelItem, Mode=OneTime}&quot;
                                                Margin=&quot;2&quot; /&gt;
                        </StackPanel&gt;
    
                    <sap:WorkflowItemPresenter Item=&quot;{Binding ModelItem.Body.Handler}&quot;
                                               HintText=&quot;Drop activity&quot;
                                               Margin=&quot;6&quot; /&gt;
                </StackPanel&gt;
            </DataTemplate&gt;
            <Style x:Key=&quot;ExpandOrCollapsedStyle&quot;
                   TargetType=&quot;{x:Type ContentPresenter}&quot;&gt;
                <Setter Property=&quot;ContentTemplate&quot;
                        Value=&quot;{DynamicResource Collapsed}&quot; /&gt;
                <Style.Triggers&gt;
                    <DataTrigger Binding=&quot;{Binding Path=ShowExpanded}&quot;
                                 Value=&quot;true&quot;&gt;
                        <Setter Property=&quot;ContentTemplate&quot;
                                Value=&quot;{DynamicResource Expanded}&quot; /&gt;
                    </DataTrigger&gt;
                </Style.Triggers&gt;
            </Style&gt;
        </sap:ActivityDesigner.Resources&gt;
        <Grid&gt;
            <ContentPresenter Style=&quot;{DynamicResource ExpandOrCollapsedStyle}&quot;
                              Content=&quot;{Binding}&quot; /&gt;
        </Grid&gt;
    </sap:ActivityDesigner&gt;{% endhighlight %}

The designer uses a TypePresenter to allow modification of the generic type of the activity. The configuration of the TypePresenter uses the [Filter property][2] to restrict the types available to those that implement IDisposable. The designer users an ExpressionTextBox to provide the disposable resource to the activity. The expression can either instantiate the resource directly or provide it by referencing a variable in the parent workflow. Finally, the designer provides a WorkflowItemPresenter that allows designer interaction with the Body activity that gets executed by the activity.

    namespace Neovolve.Toolkit.Workflow.Design.Presentation
    {
        using System;
        using System.Diagnostics;
        using Neovolve.Toolkit.Workflow.Activities;
    
        public partial class DisposalScopeDesigner
        {
            [DebuggerNonUserCode]
            public DisposalScopeDesigner()
            {
                InitializeComponent();
            }
    
            public Boolean DisposalTypeFilter(Type typeToValidate)
            {
                if (typeToValidate == null)
                {
                    return false;
                }
    
                if (typeof(IDisposable).IsAssignableFrom(typeToValidate))
                {
                    return true;
                }
    
                return false;
            }
    
            protected override void OnModelItemChanged(Object newItem)
            {
                base.OnModelItemChanged(newItem);
    
                GenericArgumentTypeUpdater.Attach(ModelItem);
            }
        }
    }{% endhighlight %}

The code behind the designer provides the filter of the TypePresenter (see [this post][2] for details) and the designer support for modifying the generic type of the activity (see [this post][3] for details).

The example in the [previous post][0] can now be refactored to the following.![image][4]

A workflow variable against held against a parent activity is no longer required to define the resource as this is now exposed directly by the DisposalScope activity. The variable that the activity provides is type safe via the generic definition as can be seen above with the ReadByte method for the FileStream type.

This is now a much cleaner solution as far as a design time experience goes and satisfies all the above design goals.&#160; 

You can download this activity in my latest [Neovolve.Toolkit 1.1 Beta][5] on the CodePlex site.

[0]: /post/2011/03/09/Working-with-unmanaged-and-IDisposable-resources-in-WF.aspx
[1]: http://msmvps.com/blogs/theproblemsolver/archive/2010/08/22/workflows-and-no-persist-zones.aspx
[2]: /post/2011/02/17/Restricting-the-types-available-in-TypePresenter-in-WF-designers.aspx
[3]: /post/2010/09/30/Creating-updatable-generic-Windows-Workflow-activities.aspx
[4]: //blogfiles/image_84.png
[5]: http://neovolve.codeplex.com/releases/view/53499
