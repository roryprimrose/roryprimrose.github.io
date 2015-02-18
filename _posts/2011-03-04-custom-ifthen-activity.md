---
title: Custom IfThen activity
categories : .Net
tags : WF
date: 2011-03-04 14:48:35 +10:00
---

The two most common WF activities to use when implementing decision branching are the If activity and the Flowchart activity. Using a Flowchart is overkill for a simple/singular decision branch so the If is often chosen. Unfortunately the If activity that comes with WF4 forces an Else branch to be visible on the designer even if you are not using it. This can make workflows unnecessarily cluttered.![image][0]

The solution is to author a custom activity that only executes an If-Then branch rather than an If-Then-Else branch and provide appropriate designer support for this. A simplistic example of such an activity was provided in the January MSDNMag by Leon Welicki in his [Authoring Control Flow Activities in WF 4][1] article.

I have created an IfThen activity that addresses this designer concern. The IfThen activity below executes its Body activity if the Condition value is true. It also includes some validation logic and a default activity structure provided via IActivityTemplateFactory.

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
    
        [ToolboxBitmap(typeof(Retry), &quot;arrow_branch.png&quot;)]
        [ContentProperty(&quot;Body&quot;)]
        public sealed class IfThen : NativeActivity, IActivityTemplateFactory
        {
            public IfThen()
            {
                Body = new ActivityAction();
            }
    
            [DebuggerNonUserCode]
            public Activity Create(DependencyObject target)
            {
                return new IfThen
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
    
                RuntimeArgument conditionArgument = new RuntimeArgument(&quot;Condition&quot;, typeof(Boolean), ArgumentDirection.In, true);
    
                metadata.Bind(Condition, conditionArgument);
    
                Collection<RuntimeArgument&gt; arguments = new Collection<RuntimeArgument&gt;
                                                        {
                                                            conditionArgument
                                                        };
    
                metadata.SetArgumentsCollection(arguments);
    
                if (Body == null
                    || Body.Handler == null)
                {
                    ValidationError validationError = new ValidationError(Resources.Activity_NoChildActivitiesDefined, true, &quot;Body&quot;);
    
                    metadata.AddValidationError(validationError);
                }
            }
    
            protected override void Execute(NativeActivityContext context)
            {
                Boolean condition = context.GetValue(Condition);
    
                if (condition == false)
                {
                    return;
                }
    
                if (Body == null)
                {
                    return;
                }
    
                context.ScheduleAction(Body);
            }
    
            [Browsable(false)]
            public ActivityAction Body
            {
                get;
                set;
            }
    
            public InArgument<Boolean&gt; Condition
            {
                get;
                set;
            }
        }
    }{% endhighlight %}

The designer support provides the ability to define the Condition expression and the child activity to execute.

    <sap:ActivityDesigner x:Class=&quot;Neovolve.Toolkit.Workflow.Design.Presentation.IfThenDesigner&quot;
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
                            <BitmapImage UriSource=&quot;arrow_branch.png&quot;&gt;</BitmapImage&gt;
                        </ImageDrawing.ImageSource&gt;
                    </ImageDrawing&gt;
                </DrawingBrush.Drawing&gt;
            </DrawingBrush&gt;
        </sap:ActivityDesigner.Icon&gt;
        <sap:ActivityDesigner.Resources&gt;
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
                    <TextBlock HorizontalAlignment=&quot;Left&quot;
                                   VerticalAlignment=&quot;Center&quot; Margin=&quot;3&quot;&gt;
                            Condition
                    </TextBlock&gt;
                    <sapv:ExpressionTextBox Expression=&quot;{Binding Path=ModelItem.Condition, Converter={StaticResource expressionConverter}}&quot;
                                                ExpressionType=&quot;s:Boolean&quot;
                                                OwnerActivity=&quot;{Binding ModelItem}&quot; Margin=&quot;3&quot; /&gt;
                    <TextBlock HorizontalAlignment=&quot;Left&quot;
                                   VerticalAlignment=&quot;Center&quot; Margin=&quot;3&quot;&gt;
                            Then
                    </TextBlock&gt;
                    <sap:WorkflowItemPresenter Item=&quot;{Binding ModelItem.Body.Handler}&quot;
                                               HintText=&quot;Drop activity&quot; Margin=&quot;3&quot; /&gt;
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

Using this new activity, the above example can now be redefined as the following.![image][2]

As you can see here, the IfThen activity is able to streamline your workflow designer for those scenarios where you do not need an Else branch in your logic.

You can grab this activity from my Neovolve.Toolkit project out on CodePlex with the latest [Neovolve.Toolkit 1.1 Beta release][3].

[0]: //blogfiles/image_79.png
[1]: http://msdn.microsoft.com/en-us/magazine/gg535667.aspx
[2]: //blogfiles/image_80.png
[3]: http://neovolve.codeplex.com/releases/view/53499
