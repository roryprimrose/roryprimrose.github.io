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

The code implementation of the custom DisposalScope&lt;T&gt; activity handles these goals.

{% highlight csharp linenos %}
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
    
    [ToolboxBitmap(typeof(ExecuteBookmark), "bin_closed.png")]
    [DefaultTypeArgument(typeof(IDisposable))]
    [ContentProperty("Body")]
    public sealed class DisposalScope<T> : NativeActivity<T>, IActivityTemplateFactory where T : class, IDisposable
    {
        public DisposalScope()
        {
            NoPersistHandle = new Variable<NoPersistHandle>();
        }
    
        public Activity Create(DependencyObject target)
        {
            ActivityAction<T> body = new ActivityAction<T>
                                        {
                                            Handler = new Sequence(), 
                                            Argument = new DelegateInArgument<T>("instance")
                                        };
    
            return new DisposalScope<T>
                    {
                        Body = body
                    };
        }
    
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            metadata.AddImplementationVariable(NoPersistHandle);
    
            RuntimeArgument instanceArgument = new RuntimeArgument("Instance", typeof(T), ArgumentDirection.In, true);
    
            metadata.Bind(Instance, instanceArgument);
    
            Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>
                                                    {
                                                        instanceArgument
                                                    };
    
            metadata.SetArgumentsCollection(arguments);
    
            if (Body == null || Body.Handler == null)
            {
                ValidationError validationError = new ValidationError(Resources.Activity_NoChildActivitiesDefined, true, "Body");
    
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
        public ActivityAction<T> Body
        {
            get;
            set;
        }
    
        [DefaultValue((String)null)]
        [RequiredArgument]
        public InArgument<T> Instance
        {
            get;
            set;
        }
    
        private Variable<NoPersistHandle> NoPersistHandle
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The DisposalScope&lt;T&gt; activity enforces a no persist zone. Attempts at persistence by a child activity will result in throwing an exception. The resource is released on either a successful or fault outcome. There is some validation in the activity that ensures that a Body activity has been defined. The activity also uses the IActivityTemplateFactory to create the activity with a Sequence activity for its Body property when it is created on the WF designer.

The designer of the activity handles most of the design time experience.

{% highlight xml linenos %}
<sap:ActivityDesigner x:Class="Neovolve.Toolkit.Workflow.Design.Presentation.DisposalScopeDesigner"
                        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:s="clr-namespace:System;assembly=mscorlib"
                        xmlns:sap="clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation"
                        xmlns:sapv="clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation"
                        xmlns:conv="clr-namespace:System.Activities.Presentation.Converters;assembly=System.Activities.Presentation"
                        xmlns:sadm="clr-namespace:System.Activities.Presentation.Model;assembly=System.Activities.Presentation"
                        xmlns:ComponentModel="clr-namespace:System.ComponentModel;assembly=WindowsBase"
                        xmlns:ntw="clr-namespace:Neovolve.Toolkit.Workflow;assembly=Neovolve.Toolkit.Workflow"
                        xmlns:ntwd="clr-namespace:Neovolve.Toolkit.Workflow.Design">
    <sap:ActivityDesigner.Icon>
        <DrawingBrush>
            <DrawingBrush.Drawing>
                <ImageDrawing>
                    <ImageDrawing.Rect>
                        <Rect Location="0,0"
                                Size="16,16">
                        </Rect>
                    </ImageDrawing.Rect>
                    <ImageDrawing.ImageSource>
                        <BitmapImage UriSource="bin_closed.png"></BitmapImage>
                    </ImageDrawing.ImageSource>
                </ImageDrawing>
            </DrawingBrush.Drawing>
        </DrawingBrush>
    </sap:ActivityDesigner.Icon>
    <sap:ActivityDesigner.Resources>
        <conv:ModelToObjectValueConverter x:Key="modelItemConverter"
                                            x:Uid="sadm:ModelToObjectValueConverter_1" />
        <conv:ArgumentToExpressionConverter x:Key="expressionConverter" />
        <DataTemplate x:Key="Collapsed">
            <TextBlock HorizontalAlignment="Center"
                        FontStyle="Italic"
                        Foreground="Gray">
                Double-click to view
            </TextBlock>
        </DataTemplate>
        <DataTemplate x:Key="Expanded">
            <StackPanel Orientation="Vertical">
                    
                    <StackPanel Orientation="Horizontal">
                        <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="false"
                                        BrowseTypeDirectly="false"
                                        Filter="DisposalTypeFilter"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
                        <TextBox Text="{Binding ModelItem.Body.Argument.Name}"
                                MinWidth="80" />
                        <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.Instance, Mode=TwoWay, Converter={StaticResource expressionConverter}, ConverterParameter=In}"
                                            OwnerActivity="{Binding ModelItem, Mode=OneTime}"
                                            Margin="2" />
                    </StackPanel>
    
                <sap:WorkflowItemPresenter Item="{Binding ModelItem.Body.Handler}"
                                            HintText="Drop activity"
                                            Margin="6" />
            </StackPanel>
        </DataTemplate>
        <Style x:Key="ExpandOrCollapsedStyle"
                TargetType="{x:Type ContentPresenter}">
            <Setter Property="ContentTemplate"
                    Value="{DynamicResource Collapsed}" />
            <Style.Triggers>
                <DataTrigger Binding="{Binding Path=ShowExpanded}"
                                Value="true">
                    <Setter Property="ContentTemplate"
                            Value="{DynamicResource Expanded}" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </sap:ActivityDesigner.Resources>
    <Grid>
        <ContentPresenter Style="{DynamicResource ExpandOrCollapsedStyle}"
                            Content="{Binding}" />
    </Grid>
</sap:ActivityDesigner>
{% endhighlight %}

The designer uses a TypePresenter to allow modification of the generic type of the activity. The configuration of the TypePresenter uses the [Filter property][2] to restrict the types available to those that implement IDisposable. The designer users an ExpressionTextBox to provide the disposable resource to the activity. The expression can either instantiate the resource directly or provide it by referencing a variable in the parent workflow. Finally, the designer provides a WorkflowItemPresenter that allows designer interaction with the Body activity that gets executed by the activity.

{% highlight csharp linenos %}
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
}
{% endhighlight %}

The code behind the designer provides the filter of the TypePresenter (see [this post][2] for details) and the designer support for modifying the generic type of the activity (see [this post][3] for details).

The example in the [previous post][0] can now be refactored to the following.![image][4]

A workflow variable against held against a parent activity is no longer required to define the resource as this is now exposed directly by the DisposalScope activity. The variable that the activity provides is type safe via the generic definition as can be seen above with the ReadByte method for the FileStream type.

This is now a much cleaner solution as far as a design time experience goes and satisfies all the above design goals.

You can download this activity in my latest [Neovolve.Toolkit 1.1 Beta][5] on the CodePlex site.

[0]: /2011/03/09/working-with-unmanaged-and-idisposable-resources-in-wf/
[1]: http://msmvps.com/blogs/theproblemsolver/archive/2010/08/22/workflows-and-no-persist-zones.aspx
[2]: /2011/02/17/restricting-the-types-available-in-typepresenter-in-wf-designers/
[3]: /2010/09/30/creating-updatable-generic-windows-workflow-activities/
[4]: /files/image_84.png
[5]: http://neovolve.codeplex.com/releases/view/53499
