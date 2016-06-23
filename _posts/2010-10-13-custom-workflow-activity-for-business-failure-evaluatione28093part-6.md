---
title: Custom Workflow activity for business failure evaluation–Part 6
categories: .Net
tags: WF
date: 2010-10-13 13:34:00 +10:00
---

The [previous post][0] in this series provided the custom activity that manages multiple business failures in WF. Providing adequate designer support was one of the [design goals][1] of this series. This post will outline the designer support for the BusinessFailureEvaluator&lt;T&gt; and BusinessFailureScope&lt;T&gt; activities.  

**BusinessFailureEvaluator&lt;T&gt;**

The BusinessFailureEvaluator&lt;T&gt; evaluates a single business failure. There is no support for child activities which makes the designer very simple.  

<!--more-->

{% highlight xml %}
<sap:ActivityDesigner x:Class="Neovolve.Toolkit.Workflow.Design.Presentation.BusinessFailureEvaluatorDesigner"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" 
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:sap="clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation"
    xmlns:sapv="clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation">
  <sap:ActivityDesigner.Icon>
    <DrawingBrush>
      <DrawingBrush.Drawing>
        <ImageDrawing>
          <ImageDrawing.Rect>
            <Rect Location="0,0" Size="16,16" ></Rect>
          </ImageDrawing.Rect>
          <ImageDrawing.ImageSource>
            <BitmapImage UriSource="shield.png" ></BitmapImage>
          </ImageDrawing.ImageSource>
        </ImageDrawing>
      </DrawingBrush.Drawing>
    </DrawingBrush>
  </sap:ActivityDesigner.Icon>
</sap:ActivityDesigner>
{% endhighlight %}

The XAML for the designer simply identifies the image to use for the activity on the design surface. 
    
![image][2]
    
The activity has a generic type argument that defaults to Int32 when the activity is dropped onto the designer. This type is not always suitable for the purposes of the application so the generic type argument needs to be updatable.
    
{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Design.Presentation
{
    using System;
    using System.Diagnostics;
    using Neovolve.Toolkit.Workflow.Activities;

    public partial class BusinessFailureEvaluatorDesigner
    {
        [DebuggerNonUserCode]
        public BusinessFailureEvaluatorDesigner()
        {
            InitializeComponent();
        }

        protected override void OnModelItemChanged(Object newItem)
        {
            base.OnModelItemChanged(newItem);

            GenericArgumentTypeUpdater.Attach(ModelItem);
        }
    }
}
{% endhighlight %}

The code behind the designer supports this by [attaching an ArgumentType property][3] to the ModelItem when it is assigned to the designer. 
    
![image][4]

This attached property allows the generic type of the activity to be changed to another type.

**BusinessFailureScope&lt;T&gt;**

The BusinessFailureScope&lt;T&gt; allows for multiple business failures to be stored against the scope so that they can be thrown together rather than one at a time.

{% highlight xml %}
<sap:ActivityDesigner x:Class="Neovolve.Toolkit.Workflow.Design.Presentation.BusinessFailureScopeDesigner"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:sap="clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation" 
    xmlns:sacdt="clr-namespace:System.Activities.Core.Presentation.Themes;assembly=System.Activities.Core.Presentation"
    xmlns:sapv="clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation">
  <sap:ActivityDesigner.Icon>
    <DrawingBrush>
      <DrawingBrush.Drawing>
        <ImageDrawing>
          <ImageDrawing.Rect>
            <Rect Location="0,0" Size="16,16" ></Rect>
          </ImageDrawing.Rect>
          <ImageDrawing.ImageSource>
            <BitmapImage UriSource="shield_go.png" ></BitmapImage>
          </ImageDrawing.ImageSource>
        </ImageDrawing>
      </DrawingBrush.Drawing>
    </DrawingBrush>
  </sap:ActivityDesigner.Icon>
  <ContentPresenter x:Uid="ContentPresenter_1" Style="{x:Static sacdt:DesignerStylesDictionary.SequenceStyle}" Content="{Binding}" />
</sap:ActivityDesigner>
{% endhighlight %}

The XAML for the designer does two things. Firstly it identifies the icon the activity uses on the designer. Secondly, it identifies that the style of the content presenter is the same one that the SequenceDesigner uses for the Sequence activity. This style provides the support for displaying arrows, drag/drop behaviour and animation on the designer for working with child activities. This style is available from the DesignerStylesDictionary.SequenceStyle property. The System.Activities.Core.Presentation assembly exposes this type and is a reference of the designer project.

![image][5]

Like the BusinessFailureEvalator&lt;T&gt; activity, the BusinessFailureScope has a generic type argument that defaults to Int32 when the activity is dropped onto the designer. The code behind the designer makes this type updatable in the same way.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Design.Presentation
{
    using System;
    using System.Diagnostics;
    using Neovolve.Toolkit.Workflow.Activities;

    public partial class BusinessFailureScopeDesigner
    {
        [DebuggerNonUserCode]
        public BusinessFailureScopeDesigner()
        {
            InitializeComponent();
        }

        protected override void OnModelItemChanged(Object newItem)
        {
            base.OnModelItemChanged(newItem);

            GenericArgumentTypeUpdater.Attach(ModelItem);
        }
    }
}
{% endhighlight %}

The property attached by GenericArgumentTypeUpdater allows generic type to be changed using the ArgumenType property.
    
![image][6]

This post has demonstrated the designer support for the BusinessFailureEvaluator&lt;T&gt; and BusinessFailureScope&lt;T&gt; activities. Workflows can now use these two activities to manage business failures.

[0]: /2010/10/13/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-5/
[1]: /2010/10/11/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-1/
[2]: /files/image_46.png
[3]: /2010/09/30/Creating-updatable-generic-Windows-Workflow-activities/
[4]: /files/image_47.png
[5]: /files/image_45.png
[6]: /files/image_50.png
