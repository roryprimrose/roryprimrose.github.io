---
title: Custom Workflow activity for business failure evaluation–Part 6
categories : .Net
tags : WF
date: 2010-10-13 13:34:00 +10:00
---

<p>The <a href="/post/2010/10/13/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-5.aspx">previous post</a> in this series provided the custom activity that manages multiple business failures in WF. Providing adequate designer support was one of the <a href="/post/2010/10/11/Custom-Workflow-activity-for-business-failure-evaluatione28093Part-1.aspx">design goals</a> of this series. This post will outline the designer support for the BusinessFailureEvaluator&lt;T&gt; and BusinessFailureScope&lt;T&gt; activities.</p>  <p><strong>BusinessFailureEvaluator&lt;T&gt;</strong></p>  <p>The BusinessFailureEvaluator&lt;T&gt; evaluates a single business failure. There is no support for child activities which makes the designer very simple.</p>  <pre class="brush: xml;">&lt;sap:ActivityDesigner x:Class=&quot;Neovolve.Toolkit.Workflow.Design.Presentation.BusinessFailureEvaluatorDesigner&quot;
    xmlns=&quot;http://schemas.microsoft.com/winfx/2006/xaml/presentation&quot; 
    xmlns:x=&quot;http://schemas.microsoft.com/winfx/2006/xaml&quot;
    xmlns:sap=&quot;clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation&quot;
    xmlns:sapv=&quot;clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation&quot;&gt;
  &lt;sap:ActivityDesigner.Icon&gt;
    &lt;DrawingBrush&gt;
      &lt;DrawingBrush.Drawing&gt;
        &lt;ImageDrawing&gt;
          &lt;ImageDrawing.Rect&gt;
            &lt;Rect Location=&quot;0,0&quot; Size=&quot;16,16&quot; &gt;&lt;/Rect&gt;
          &lt;/ImageDrawing.Rect&gt;
          &lt;ImageDrawing.ImageSource&gt;
            &lt;BitmapImage UriSource=&quot;shield.png&quot; &gt;&lt;/BitmapImage&gt;
          &lt;/ImageDrawing.ImageSource&gt;
        &lt;/ImageDrawing&gt;
      &lt;/DrawingBrush.Drawing&gt;
    &lt;/DrawingBrush&gt;
  &lt;/sap:ActivityDesigner.Icon&gt;
&lt;/sap:ActivityDesigner&gt;{% endhighlight %}
The XAML for the designer simply identifies the image to use for the activity on the design surface. 

<p><a href="//blogfiles/image_46.png"><img src="//blogfiles/image_46.png" /></a></p>

<p>The activity has a generic type argument that defaults to Int32 when the activity is dropped onto the designer. This type is not always suitable for the purposes of the application so the generic type argument needs to be updatable.</p>

<pre class="brush: csharp;">namespace Neovolve.Toolkit.Workflow.Design.Presentation
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
}{% endhighlight %}

<p>The code behind the designer supports this by <a href="/post/2010/09/30/Creating-updatable-generic-Windows-Workflow-activities.aspx">attaching an ArgumentType property</a> to the ModelItem when it is assigned to the designer. </p>

<p><a href="//blogfiles/image_47.png"><img src="//blogfiles/image_47.png" /></a></p>

<p>This attached property allows the generic type of the activity to be changed to another type.</p>

<p><strong>BusinessFailureScope&lt;T&gt;</strong></p>

<p>The BusinessFailureScope&lt;T&gt; allows for multiple business failures to be stored against the scope so that they can be thrown together rather than one at a time.</p>

<pre class="brush: xml;">&lt;sap:ActivityDesigner x:Class=&quot;Neovolve.Toolkit.Workflow.Design.Presentation.BusinessFailureScopeDesigner&quot;
    xmlns=&quot;http://schemas.microsoft.com/winfx/2006/xaml/presentation&quot;
    xmlns:x=&quot;http://schemas.microsoft.com/winfx/2006/xaml&quot;
    xmlns:sap=&quot;clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation&quot; 
    xmlns:sacdt=&quot;clr-namespace:System.Activities.Core.Presentation.Themes;assembly=System.Activities.Core.Presentation&quot;
    xmlns:sapv=&quot;clr-namespace:System.Activities.Presentation.View;assembly=System.Activities.Presentation&quot;&gt;
  &lt;sap:ActivityDesigner.Icon&gt;
    &lt;DrawingBrush&gt;
      &lt;DrawingBrush.Drawing&gt;
        &lt;ImageDrawing&gt;
          &lt;ImageDrawing.Rect&gt;
            &lt;Rect Location=&quot;0,0&quot; Size=&quot;16,16&quot; &gt;&lt;/Rect&gt;
          &lt;/ImageDrawing.Rect&gt;
          &lt;ImageDrawing.ImageSource&gt;
            &lt;BitmapImage UriSource=&quot;shield_go.png&quot; &gt;&lt;/BitmapImage&gt;
          &lt;/ImageDrawing.ImageSource&gt;
        &lt;/ImageDrawing&gt;
      &lt;/DrawingBrush.Drawing&gt;
    &lt;/DrawingBrush&gt;
  &lt;/sap:ActivityDesigner.Icon&gt;
  &lt;ContentPresenter x:Uid=&quot;ContentPresenter_1&quot; Style=&quot;{x:Static sacdt:DesignerStylesDictionary.SequenceStyle}&quot; Content=&quot;{Binding}&quot; /&gt;
&lt;/sap:ActivityDesigner&gt;{% endhighlight %}

<p>The XAML for the designer does two things. Firstly it identifies the icon the activity uses on the designer. Secondly, it identifies that the style of the content presenter is the same one that the SequenceDesigner uses for the Sequence activity. This style provides the support for displaying arrows, drag/drop behaviour and animation on the designer for working with child activities. This style is available from the DesignerStylesDictionary.SequenceStyle property. The System.Activities.Core.Presentation assembly exposes this type and is a reference of the designer project.</p>

<p><a href="//blogfiles/image_45.png"><img src="//blogfiles/image_45.png" /></a></p>

<p>Like the BusinessFailureEvalator&lt;T&gt; activity, the BusinessFailureScope has a generic type argument that defaults to Int32 when the activity is dropped onto the designer. The code behind the designer makes this type updatable in the same way.</p>

<pre class="brush: csharp;">namespace Neovolve.Toolkit.Workflow.Design.Presentation
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
}{% endhighlight %}

<p>The property attached by GenericArgumentTypeUpdater allows generic type to be changed using the ArgumenType property.</p>

<p><a href="//blogfiles/image_50.png"><img src="//blogfiles/image_50.png" /></a></p>

<p>This post has demonstrated the designer support for the BusinessFailureEvaluator&lt;T&gt; and BusinessFailureScope&lt;T&gt; activities. Workflows can now use these two activities to manage business failures.</p>
