---
title: WF Retry activity
categories: .Net, Applications
tags: Sync Framework, WCF, WF
date: 2011-02-21 11:49:08 +10:00
---

One of the tools missing out of the WF toolbox is the ability to run some retry logic. Applications often have known scenarios where something can go wrong such that a retry of the last action could get a successful outcome. One such example is a connection timeout to a database. You may want to try a couple of times before throwing the exception in order to get more success over time.

The specific scenario I am addressing is a little different. I have created some custom MSF providers that will allow me to run a MSF sync session behind a WCF service. The main issue with this design is that it is possible that multiple clients may want to sync the same data set at the same time. MSF stores the metadata (Replica) that describes the items to sync in a SQLCE file. It will throw an exception if the replica is already open and in use. This doesn’t work well in a services environment. The way I will attempt to manage this is to limit the amount of time the replica is in use and then implement some retry logic around opening the replica in order to run a sync.

<!--more-->

The Retry activity shown below will execute its child activity and watch for a match on a nominated exception type or derivative. It will then determine whether it can make another attempt at the child activity by comparing the current number of attempts against a MaxAttempts property. If another attempt will be made then it will determine whether the child activity will be invoked immediately or whether a delay should occur first.

```csharp
namespace Neovolve.Toolkit.Workflow.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Activities.Statements;
    using System.Activities.Validation;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Markup;
    using Neovolve.Toolkit.Workflow.Properties;
    
    [ToolboxBitmap(typeof(Retry), "arrow_retry.png")]
    [ContentProperty("Body")]
    public sealed class Retry : NativeActivity, IActivityTemplateFactory
    {
        private static readonly TimeSpan _defaultRetryInterval = new TimeSpan(0, 0, 0, 1);
    
        private readonly Variable<Int32> _attemptCount = new Variable<Int32>();
    
        private readonly Variable<TimeSpan> _delayDuration = new Variable<TimeSpan>();
    
        private readonly Delay _internalDelay;
    
        public Retry()
        {
            _internalDelay = new Delay
                                {
                                    Duration = new InArgument<TimeSpan>(_delayDuration)
                                };
            Body = new ActivityAction();
            MaxAttempts = 5;
            ExceptionType = typeof(TimeoutException);
            RetryInterval = _defaultRetryInterval;
        }
    
        [DebuggerNonUserCode]
        public Activity Create(DependencyObject target)
        {
            return new Retry
                    {
                        Body =
                            {
                                Handler = new Sequence()
                            }
                    };
        }
    
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            metadata.AddImplementationChild(_internalDelay);
            metadata.AddImplementationVariable(_attemptCount);
            metadata.AddImplementationVariable(_delayDuration);
    
            RuntimeArgument maxAttemptsArgument = new RuntimeArgument("MaxAttempts", typeof(Int32), ArgumentDirection.In, true);
            RuntimeArgument retryIntervalArgument = new RuntimeArgument("RetryInterval", typeof(TimeSpan), ArgumentDirection.In, true);
    
            metadata.Bind(MaxAttempts, maxAttemptsArgument);
            metadata.Bind(RetryInterval, retryIntervalArgument);
    
            Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>
                                                    {
                                                        maxAttemptsArgument, 
                                                        retryIntervalArgument
                                                    };
    
            metadata.SetArgumentsCollection(arguments);
    
            if (Body == null)
            {
                ValidationError validationError = new ValidationError(Resources.Retry_NoChildActivitiesDefined, true, "Body");
    
                metadata.AddValidationError(validationError);
            }
    
            if (typeof(Exception).IsAssignableFrom(ExceptionType) == false)
            {
                ValidationError validationError = new ValidationError(Resources.Retry_InvalidExceptionType, false, "ExceptionType");
    
                metadata.AddValidationError(validationError);
            }
        }
    
        protected override void Execute(NativeActivityContext context)
        {
            ExecuteAttempt(context);
        }
    
        private static Boolean ShouldRetryAction(Type exceptionType, Exception thrownException)
        {
            if (exceptionType == null)
            {
                return false;
            }
    
            return exceptionType.IsAssignableFrom(thrownException.GetType());
        }
    
        private void ActionFailed(NativeActivityFaultContext faultcontext, Exception propagatedexception, ActivityInstance propagatedfrom)
        {
            Int32 currentAttemptCount = _attemptCount.Get(faultcontext);
    
            currentAttemptCount++;
    
            _attemptCount.Set(faultcontext, currentAttemptCount);
    
            Int32 maxAttempts = MaxAttempts.Get(faultcontext);
    
            if (currentAttemptCount >= maxAttempts)
            {
                // There are no further attempts to make
                return;
            }
    
            if (ShouldRetryAction(ExceptionType, propagatedexception) == false)
            {
                return;
            }
    
            faultcontext.CancelChild(propagatedfrom);
            faultcontext.HandleFault();
    
            TimeSpan retryInterval = RetryInterval.Get(faultcontext);
    
            if (retryInterval == TimeSpan.Zero)
            {
                ExecuteAttempt(faultcontext);
            }
            else
            {
                // We are going to wait before trying again
                _delayDuration.Set(faultcontext, retryInterval);
    
                faultcontext.ScheduleActivity(_internalDelay, DelayCompleted);
            }
        }
    
        private void DelayCompleted(NativeActivityContext context, ActivityInstance completedinstance)
        {
            ExecuteAttempt(context);
        }
    
        private void ExecuteAttempt(NativeActivityContext context)
        {
            if (Body == null)
            {
                return;
            }
    
            context.ScheduleAction(Body, null, ActionFailed);
        }
    
        [Browsable(false)]
        public ActivityAction Body
        {
            get;
            set;
        }
    
        [DefaultValue(typeof(TimeoutException))]
        public Type ExceptionType
        {
            get;
            set;
        }
    
        public InArgument<Int32> MaxAttempts
        {
            get;
            set;
        }
    
        public InArgument<TimeSpan> RetryInterval
        {
            get;
            set;
        }
    }
}
```

The CacheMetadata method has some logic in it to validate the state of the activity. This validation will display a warning if no child activity has been defined. It will also raise an error if the ExceptionType property has not been assigned a type that derives from System.Exception. The activity implements IActivityTemplateFactory and uses the Create method to include a child Sequence activity when Retry is added to an activity on the designer.

The designer xaml of the Retry activity looks like the following.

```xml
<sap:ActivityDesigner x:Class="Neovolve.Toolkit.Workflow.Design.Presentation.RetryDesigner"
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
                        <BitmapImage UriSource="arrow_retry.png"></BitmapImage>
                    </ImageDrawing.ImageSource>
                </ImageDrawing>
            </DrawingBrush.Drawing>
        </DrawingBrush>
    </sap:ActivityDesigner.Icon>
    <sap:ActivityDesigner.Resources>
        <conv:ModelToObjectValueConverter x:Key="modelItemConverter"
                                            x:Uid="sadm:ModelToObjectValueConverter_1" />
        <DataTemplate x:Key="Collapsed">
            <TextBlock HorizontalAlignment="Center"
                        FontStyle="Italic"
                        Foreground="Gray">
                Double-click to view
            </TextBlock>
        </DataTemplate>
        <DataTemplate x:Key="Expanded">
            <StackPanel Orientation="Vertical">
                <Grid Name="contentGrid">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition />
                    </Grid.RowDefinitions>
    
                    <TextBlock Grid.Row="0"
                                Grid.Column="0"
                                HorizontalAlignment="Left"
                                VerticalAlignment="Center">
                        Exception Type:
                    </TextBlock>
                    <sapv:TypePresenter HorizontalAlignment="Left"
                                        VerticalAlignment="Center"
                                        Margin="6"
                                        Grid.Row="0"
                                        Grid.Column="1"
                                        Filter="ExceptionTypeFilter"
                                        AllowNull="false"
                                        BrowseTypeDirectly="false"
                                        Label="Exception type"
                                        Type="{Binding Path=ModelItem.ExceptionType, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
                </Grid>
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
```

The designer supports the common expand/collapse style behaviour and allows for a single child activity via the WorkflowItemPresenter control. It also allows for modification of the activities ExceptionType property via the designer using the TypePresenter control. A [recent post][0] discusses the use of the Filter property on the TypePresenter to restrict the available types to be those derived from System.Exception.![image][1]

The Retry activity will make as many attempts as it can to successfully execute the child activity. One thing to note about this however is that there is no concept of transactional support within the activity. Any workflow variables that have changed or any other side affects from the child activity will remain when it fails and is retried. For example, if the child activity increments a workflow variable or makes a change to the file system then these changes will remain in that state when the child activity is executed again. This must be taken into consideration when using this activity. The number of tasks actioned within the Retry should be as few as possible and should not alter any in-memory or persisted state.

[0]: /2011/02/17/restricting-the-types-available-in-typepresenter-in-wf-designers/
[1]: /files/image_78.png
