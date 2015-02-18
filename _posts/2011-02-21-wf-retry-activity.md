---
title: WF Retry activity
categories : .Net, Applications
tags : Sync Framework, WCF, WF
date: 2011-02-21 11:49:08 +10:00
---

One of the tools missing out of the WF toolbox is the ability to run some retry logic. Applications often have known scenarios where something can go wrong such that a retry of the last action could get a successful outcome. One such example is a connection timeout to a database. You may want to try a couple of times before throwing the exception in order to get more success over time.

The specific scenario I am addressing is a little different. I have created some custom MSF providers that will allow me to run a MSF sync session behind a WCF service. The main issue with this design is that it is possible that multiple clients may want to sync the same data set at the same time. MSF stores the metadata (Replica) that describes the items to sync in a SQLCE file. It will throw an exception if the replica is already open and in use. This doesn’t work well in a services environment. The way I will attempt to manage this is to limit the amount of time the replica is in use and then implement some retry logic around opening the replica in order to run a sync.

The Retry activity shown below will execute its child activity and watch for a match on a nominated exception type or derivative. It will then determine whether it can make another attempt at the child activity by comparing the current number of attempts against a MaxAttempts property. If another attempt will be made then it will determine whether the child activity will be invoked immediately or whether a delay should occur first.

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
    
        [ToolboxBitmap(typeof(Retry), &quot;arrow_retry.png&quot;)]
        [ContentProperty(&quot;Body&quot;)]
        public sealed class Retry : NativeActivity, IActivityTemplateFactory
        {
            private static readonly TimeSpan _defaultRetryInterval = new TimeSpan(0, 0, 0, 1);
    
            private readonly Variable<Int32&gt; _attemptCount = new Variable<Int32&gt;();
    
            private readonly Variable<TimeSpan&gt; _delayDuration = new Variable<TimeSpan&gt;();
    
            private readonly Delay _internalDelay;
    
            public Retry()
            {
                _internalDelay = new Delay
                                 {
                                     Duration = new InArgument<TimeSpan&gt;(_delayDuration)
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
    
                RuntimeArgument maxAttemptsArgument = new RuntimeArgument(&quot;MaxAttempts&quot;, typeof(Int32), ArgumentDirection.In, true);
                RuntimeArgument retryIntervalArgument = new RuntimeArgument(&quot;RetryInterval&quot;, typeof(TimeSpan), ArgumentDirection.In, true);
    
                metadata.Bind(MaxAttempts, maxAttemptsArgument);
                metadata.Bind(RetryInterval, retryIntervalArgument);
    
                Collection<RuntimeArgument&gt; arguments = new Collection<RuntimeArgument&gt;
                                                        {
                                                            maxAttemptsArgument, 
                                                            retryIntervalArgument
                                                        };
    
                metadata.SetArgumentsCollection(arguments);
    
                if (Body == null)
                {
                    ValidationError validationError = new ValidationError(Resources.Retry_NoChildActivitiesDefined, true, &quot;Body&quot;);
    
                    metadata.AddValidationError(validationError);
                }
    
                if (typeof(Exception).IsAssignableFrom(ExceptionType) == false)
                {
                    ValidationError validationError = new ValidationError(Resources.Retry_InvalidExceptionType, false, &quot;ExceptionType&quot;);
    
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
    
                if (currentAttemptCount &gt;= maxAttempts)
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
    
            public InArgument<Int32&gt; MaxAttempts
            {
                get;
                set;
            }
    
            public InArgument<TimeSpan&gt; RetryInterval
            {
                get;
                set;
            }
        }
    }{% endhighlight %}

The CacheMetadata method has some logic in it to validate the state of the activity. This validation will display a warning if no child activity has been defined. It will also raise an error if the ExceptionType property has not been assigned a type that derives from System.Exception. The activity implements IActivityTemplateFactory and uses the Create method to include a child Sequence activity when Retry is added to an activity on the designer.

The designer xaml of the Retry activity looks like the following.

    <sap:ActivityDesigner x:Class=&quot;Neovolve.Toolkit.Workflow.Design.Presentation.RetryDesigner&quot;
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
                            <BitmapImage UriSource=&quot;arrow_retry.png&quot;&gt;</BitmapImage&gt;
                        </ImageDrawing.ImageSource&gt;
                    </ImageDrawing&gt;
                </DrawingBrush.Drawing&gt;
            </DrawingBrush&gt;
        </sap:ActivityDesigner.Icon&gt;
        <sap:ActivityDesigner.Resources&gt;
            <conv:ModelToObjectValueConverter x:Key=&quot;modelItemConverter&quot;
                                              x:Uid=&quot;sadm:ModelToObjectValueConverter_1&quot; /&gt;
            <DataTemplate x:Key=&quot;Collapsed&quot;&gt;
                <TextBlock HorizontalAlignment=&quot;Center&quot;
                           FontStyle=&quot;Italic&quot;
                           Foreground=&quot;Gray&quot;&gt;
                    Double-click to view
                </TextBlock&gt;
            </DataTemplate&gt;
            <DataTemplate x:Key=&quot;Expanded&quot;&gt;
                <StackPanel Orientation=&quot;Vertical&quot;&gt;
                    <Grid Name=&quot;contentGrid&quot;&gt;
                        <Grid.ColumnDefinitions&gt;
                            <ColumnDefinition Width=&quot;Auto&quot; /&gt;
                            <ColumnDefinition Width=&quot;*&quot; /&gt;
                        </Grid.ColumnDefinitions&gt;
                        <Grid.RowDefinitions&gt;
                            <RowDefinition /&gt;
                        </Grid.RowDefinitions&gt;
    
                        <TextBlock Grid.Row=&quot;0&quot;
                                   Grid.Column=&quot;0&quot;
                                   HorizontalAlignment=&quot;Left&quot;
                                   VerticalAlignment=&quot;Center&quot;&gt;
                            Exception Type:
                        </TextBlock&gt;
                        <sapv:TypePresenter HorizontalAlignment=&quot;Left&quot;
                                            VerticalAlignment=&quot;Center&quot;
                                            Margin=&quot;6&quot;
                                            Grid.Row=&quot;0&quot;
                                            Grid.Column=&quot;1&quot;
                                            Filter=&quot;ExceptionTypeFilter&quot;
                                            AllowNull=&quot;false&quot;
                                            BrowseTypeDirectly=&quot;false&quot;
                                            Label=&quot;Exception type&quot;
                                            Type=&quot;{Binding Path=ModelItem.ExceptionType, Mode=TwoWay, Converter={StaticResource modelItemConverter}}&quot;
                                            Context=&quot;{Binding Context}&quot; /&gt;
                    </Grid&gt;
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

The designer supports the common expand/collapse style behaviour and allows for a single child activity via the WorkflowItemPresenter control. It also allows for modification of the activities ExceptionType property via the designer using the TypePresenter control. A [recent post][0] discusses the use of the Filter property on the TypePresenter to restrict the available types to be those derived from System.Exception.![image][1]

The Retry activity will make as many attempts as it can to successfully execute the child activity. One thing to note about this however is that there is no concept of transactional support within the activity. Any workflow variables that have changed or any other side affects from the child activity will remain when it fails and is retried. For example, if the child activity increments a workflow variable or makes a change to the file system then these changes will remain in that state when the child activity is executed again. This must be taken into consideration when using this activity. The number of tasks actioned within the Retry should be as few as possible and should not alter any in-memory or persisted state.

[0]: /post/2011/02/17/Restricting-the-types-available-in-TypePresenter-in-WF-designers.aspx
[1]: //blogfiles/image_78.png
