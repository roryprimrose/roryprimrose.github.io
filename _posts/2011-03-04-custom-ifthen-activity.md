---
title: Custom IfThen activity
categories : .Net
tags : WF
date: 2011-03-04 14:48:35 +10:00
---

The two most common WF activities to use when implementing decision branching are the If activity and the Flowchart activity. Using a Flowchart is overkill for a simple/singular decision branch so the If is often chosen. Unfortunately the If activity that comes with WF4 forces an Else branch to be visible on the designer even if you are not using it. This can make workflows unnecessarily cluttered.![image][0]

The solution is to author a custom activity that only executes an If-Then branch rather than an If-Then-Else branch and provide appropriate designer support for this. A simplistic example of such an activity was provided in the January MSDNMag by Leon Welicki in his [Authoring Control Flow Activities in WF 4][1] article.

I have created an IfThen activity that addresses this designer concern. The IfThen activity below executes its Body activity if the Condition value is true. It also includes some validation logic and a default activity structure provided via IActivityTemplateFactory.

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
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows;
    using System.Windows.Markup;
    using Neovolve.Toolkit.Workflow.Properties;
    
    [ToolboxBitmap(typeof(Retry), "arrow_branch.png")]
    [ContentProperty("Body")]
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
    
            RuntimeArgument conditionArgument = new RuntimeArgument("Condition", typeof(Boolean), ArgumentDirection.In, true);
    
            metadata.Bind(Condition, conditionArgument);
    
            Collection<RuntimeArgument> arguments = new Collection<RuntimeArgument>
                                                    {
                                                        conditionArgument
                                                    };
    
            metadata.SetArgumentsCollection(arguments);
    
            if (Body == null
                || Body.Handler == null)
            {
                ValidationError validationError = new ValidationError(Resources.Activity_NoChildActivitiesDefined, true, "Body");
    
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
    
        public InArgument<Boolean> Condition
        {
            get;
            set;
        }
    }
}
{% endhighlight %}

The designer support provides the ability to define the Condition expression and the child activity to execute.

{% highlight xml linenos %}
<sap:ActivityDesigner x:Class="Neovolve.Toolkit.Workflow.Design.Presentation.IfThenDesigner"
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
                        <BitmapImage UriSource="arrow_branch.png"></BitmapImage>
                    </ImageDrawing.ImageSource>
                </ImageDrawing>
            </DrawingBrush.Drawing>
        </DrawingBrush>
    </sap:ActivityDesigner.Icon>
    <sap:ActivityDesigner.Resources>
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
                <TextBlock HorizontalAlignment="Left"
                                VerticalAlignment="Center" Margin="3">
                        Condition
                </TextBlock>
                <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.Condition, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:Boolean"
                                            OwnerActivity="{Binding ModelItem}" Margin="3" />
                <TextBlock HorizontalAlignment="Left"
                                VerticalAlignment="Center" Margin="3">
                        Then
                </TextBlock>
                <sap:WorkflowItemPresenter Item="{Binding ModelItem.Body.Handler}"
                                            HintText="Drop activity" Margin="3" />
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

Using this new activity, the above example can now be redefined as the following.![image][2]

As you can see here, the IfThen activity is able to streamline your workflow designer for those scenarios where you do not need an Else branch in your logic.

You can grab this activity from my Neovolve.Toolkit project out on CodePlex with the latest [Neovolve.Toolkit 1.1 Beta release][3].

[0]: /files/image_79.png
[1]: http://msdn.microsoft.com/en-us/magazine/gg535667.aspx
[2]: /files/image_80.png
[3]: http://neovolve.codeplex.com/releases/view/53499
