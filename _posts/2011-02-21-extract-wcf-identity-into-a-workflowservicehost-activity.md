---
title: Extract WCF identity into a WorkflowServiceHost activity
categories : .Net
tags : WCF, WF, WIF
date: 2011-02-21 11:02:54 +10:00
---

<p>I came across an issue with the combination of WCF, WF, WIF and AppFabric in December that had me a little worried. The issue was how to get the identity of the user calling a WCF service inside a WorkflowServiceHost workflow when using WIF to manage security and AppFabric for WF persistence. </p>  <p>The WIF documentation says the following:</p>  <blockquote>   <p><em>As a note, when WIF is enabled inside WCF, the WCF <code>ServiceSecurityContext</code> does not work for obtaining the caller’s identity and claims; the application code must use the <a>IClaimsPrincipal</a> to access the caller’s information. You can obtain the <a>IClaimsPrincipal</a> instance using<code>Thread.CurrentPrincipal</code>. This is available for every authentication type as soon as WIF is enabled for the WCF application.</em></p> </blockquote>  <p>I had been developing my service application using Thread.CurrentPrincipal based on this information. Everything was fine until I started using WF persistence. Unfortunately enabling persistence (via AppFabric) had the affect of wiping out Thread.CurrentPrincipal.</p>  <p>The requirement for persistence is so that the service can throw a fault back to the client and allow the workflow instance to remain alive (persisted) so that it can be called again. An example of this scenario is where a validation fault is thrown in the middle of an existing service session. The service needs to be persisted so that it can be called again by the client otherwise the session will be lost. So the question is how do we get the identity of the user when we have both WIF and WF persistence running a WCF service? </p>  <p>I put the question out on the <a href="http://social.msdn.microsoft.com/Forums/en-US/wfprerelease/thread/7ff447ff-5a7b-40e3-ad9e-db8c9847bfbb/#491737e4-1f06-4aa2-b3cd-9b3cfcb51cd7" target="_blank">MSDN forum</a> at emailed a few of the prominent WF guru’s for help. Both <a href="http://zamd.net/2010/07/04/using-wif-with-workflow-services/" target="_blank">Zulfiqar</a> and <a href="http://msmvps.com/blogs/theproblemsolver/archive/2010/09/21/using-the-wcf-operationcontext-from-a-receive-activity.aspx" target="_blank">Maurice</a> have the answer to this riddle. It turns out that the WIF documentation is not entirely correct as the security information about the client is still available via ServiceSecurityContext. </p>  <p>I have created a custom WF activity that will make the calling identity available to the workflow. I wanted my activity to be more specific than the OperationContextScope activity available in the <a href="http://wf.codeplex.com/releases/view/48114" target="_blank">WF security pack</a> on Codeplex in order for it to be intuitive and easy to use. This activity does however take the same implementation as the OperationContextScope activity.</p>  <p>The ReceiveIdentityInspector activity below hooks into the WCF pipeline using a private inspector class that implements <a href="http://msdn.microsoft.com/en-us/library/system.servicemodel.activities.ireceivemessagecallback.aspx" target="_blank">IReceiveMessageCallback</a>. This interface allows the inspector class to look at the client identity of the WCF service that has executed the Receive activity in the workflow service. The inspector instance is added as a property on the WF context so that it gets executed. The activity can then get the identity from the inspector and make it available to the parent workflow. The Body property of the activity is of type Receive as it only supports a Receive activity as its single child activity. This helps to make a nice drag\drop experience on the designer and prevent the user dragging inappropriate activities on to it.</p>  <pre class="brush: csharp;">namespace Neovolve.Toolkit.Workflow.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Activities.Validation;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Principal;
    using System.ServiceModel;
    using System.ServiceModel.Activities;
    using System.Windows.Markup;
    using Neovolve.Toolkit.Workflow.Properties;

    [ToolboxBitmap(typeof(ReceiveIdentityInspector&lt;&gt;), &quot;user.png&quot;)]
    [ContentProperty(&quot;Body&quot;)]
    [DefaultTypeArgument(typeof(IIdentity))]
    public sealed class ReceiveIdentityInspector&lt;T&gt; : NativeActivity&lt;T&gt; where T : class, IIdentity
    {
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (Body == null)
            {
                ValidationError nullBodyFailure = new ValidationError(Resources.OperationIdentityInspector_NullBodyFailure, false, &quot;Body&quot;);

                metadata.AddValidationError(nullBodyFailure);
            }

            metadata.AddChild(Body);
        }

        protected override void Execute(NativeActivityContext context)
        {
            if (Body == null)
            {
                return;
            }

            OperationIdentityInspector inspector = new OperationIdentityInspector();

            context.Properties.Add(inspector.GetType().FullName, inspector);

            context.ScheduleActivity(Body, OnBodyCompleted);
        }

        private void OnBodyCompleted(NativeActivityContext context, ActivityInstance instance)
        {
            OperationIdentityInspector inspector = context.Properties.Find(typeof(OperationIdentityInspector).FullName) as OperationIdentityInspector;

            Debug.Assert(inspector != null, &quot;No inspector was found in the context properties&quot;);

            IIdentity operationIdentity = inspector.Identity;

            if (operationIdentity == null)
            {
                return;
            }

            T specificIdentity = operationIdentity as T;

            if (specificIdentity == null)
            {
                String message = String.Format(
                    CultureInfo.CurrentCulture, 
                    Resources.OperationIdentityInspector_UnexpectedIdentityType, 
                    typeof(T).FullName, 
                    operationIdentity.GetType().FullName);

                throw new InvalidCastException(message);
            }

            Result.Set(context, specificIdentity);
        }

        [DefaultValue((String)null)]
        [Browsable(false)]
        public Receive Body
        {
            get;
            set;
        }

        [DataContract]
        private class OperationIdentityInspector : IReceiveMessageCallback
        {
            public void OnReceiveMessage(OperationContext operationContext, ExecutionProperties activityExecutionProperties)
            {
                if (operationContext == null)
                {
                    return;
                }

                if (operationContext.ServiceSecurityContext == null)
                {
                    return;
                }

                Identity = operationContext.ServiceSecurityContext.PrimaryIdentity;
            }

            [DataMember]
            public IIdentity Identity
            {
                get;
                set;
            }
        }
    }
}{% endhighlight %}

<p>The designer of the activity allows for a child activity to be dragged and dropped onto it by using a WorkflowItemPresenter. It also supports the normal collapse/expand style behaviour.</p>

<pre class="brush: xml;">&lt;sap:ActivityDesigner x:Class=&quot;Neovolve.Toolkit.Workflow.Design.Presentation.ReceiveIdentityInspectorDesigner&quot;
                      xmlns=&quot;http://schemas.microsoft.com/winfx/2006/xaml/presentation&quot;
                      xmlns:x=&quot;http://schemas.microsoft.com/winfx/2006/xaml&quot;
                      xmlns:sap=&quot;clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation&quot;
                      xmlns:Activities=&quot;clr-namespace:System.ServiceModel.Activities;assembly=System.ServiceModel.Activities&quot;&gt;
  &lt;sap:ActivityDesigner.Icon&gt;
    &lt;DrawingBrush&gt;
      &lt;DrawingBrush.Drawing&gt;
        &lt;ImageDrawing&gt;
          &lt;ImageDrawing.Rect&gt;
            &lt;Rect Location=&quot;0,0&quot; Size=&quot;16,16&quot; &gt;&lt;/Rect&gt;
          &lt;/ImageDrawing.Rect&gt;
          &lt;ImageDrawing.ImageSource&gt;
            &lt;BitmapImage UriSource=&quot;user.png&quot; &gt;&lt;/BitmapImage&gt;
          &lt;/ImageDrawing.ImageSource&gt;
        &lt;/ImageDrawing&gt;
      &lt;/DrawingBrush.Drawing&gt;
    &lt;/DrawingBrush&gt;
  &lt;/sap:ActivityDesigner.Icon&gt;
    &lt;sap:ActivityDesigner.Resources&gt;
        &lt;DataTemplate x:Key=&quot;Collapsed&quot;&gt;
            &lt;TextBlock HorizontalAlignment=&quot;Center&quot;
                       FontStyle=&quot;Italic&quot;
                       Foreground=&quot;Gray&quot;&gt;
                Double-click to view
            &lt;/TextBlock&gt;
        &lt;/DataTemplate&gt;
        &lt;DataTemplate x:Key=&quot;Expanded&quot;&gt;
      &lt;sap:WorkflowItemPresenter AllowedItemType=&quot;{x:Type Activities:Receive}&quot; Item=&quot;{Binding ModelItem.Body}&quot;
                                       HintText=&quot;Drop activity&quot;
                                       Margin=&quot;6&quot; /&gt;
        &lt;/DataTemplate&gt;
        &lt;Style x:Key=&quot;ExpandOrCollapsedStyle&quot;
               TargetType=&quot;{x:Type ContentPresenter}&quot;&gt;
            &lt;Setter Property=&quot;ContentTemplate&quot;
                    Value=&quot;{DynamicResource Collapsed}&quot; /&gt;
            &lt;Style.Triggers&gt;
                &lt;DataTrigger Binding=&quot;{Binding Path=ShowExpanded}&quot;
                             Value=&quot;true&quot;&gt;
                    &lt;Setter Property=&quot;ContentTemplate&quot;
                            Value=&quot;{DynamicResource Expanded}&quot; /&gt;
                &lt;/DataTrigger&gt;
            &lt;/Style.Triggers&gt;
        &lt;/Style&gt;
    &lt;/sap:ActivityDesigner.Resources&gt;
    &lt;Grid&gt;
        &lt;ContentPresenter Style=&quot;{DynamicResource ExpandOrCollapsedStyle}&quot;
                          Content=&quot;{Binding}&quot; /&gt;
    &lt;/Grid&gt;
&lt;/sap:ActivityDesigner&gt;{% endhighlight %}

<p>The code behind the designer adds support for the generic IIdentity type of the activity to be modified using a <a href="/post/2010/09/30/Creating-updatable-generic-Windows-Workflow-activities.aspx">previously posted</a> helper class.</p>

<pre class="brush: csharp;">namespace Neovolve.Toolkit.Workflow.Design.Presentation
{
    using System;
    using System.Diagnostics;
    using Neovolve.Toolkit.Workflow.Activities;

    public partial class ReceiveIdentityInspectorDesigner
    {
        [DebuggerNonUserCode]
        public ReceiveIdentityInspectorDesigner()
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

<p>The designer looks like the following. The ReceiveIdentityInspector&lt;IIdentity&gt; seen below contains a Receive activity as its only child activity.</p>

<p><img style="background-image: none; border-right-width: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_72.png" width="326" height="413" /></p>

<p>The activity will expose the identity of the user calling the GetData service operation and make it available in its Result property.</p>

<p><img style="background-image: none; border-right-width: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_73.png" width="366" height="181" /></p>

<p>In this example the user identity is stored in a workflow variable called CallingIdentity. The workflow can then refer to this variable when it needs to take any identity related action.</p>

<p><img style="background-image: none; border-right-width: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_74.png" width="563" height="138" /></p>

<p>One side effect of this implementation is that the user identity will now automatically participate in workflow persistence because it is stored in a workflow variable. This can be useful for security validation of a workflow that simulates a service session using content correlation. </p>

<p>In the following example, a service has a StartSession operation that returns a System.Guid and must be the first operation invoked on the service. A service session is simulated as all other methods on the service accept this Guid value. Content correlation then ensures that correct workflow instance is associated with the subsequent service calls matching on the Guid. </p>

<p>The security scenario to cover is if another authenticated user hijacks the session by providing the correct Guid for the session of another user. </p>

<p>Persisting the identity that called the StartSession service operation in a high enough scope will make it available to the entire service session lifetime. In this case the ReceiveIdentityInspector surrounding StartSession will store the identity in a workflow variable called StartSessionIdentity. The workflow is then persisted after the StartSession service operation has completed and waits for the client to invoke the next service operation using the correct Guid value.</p>

<p><img style="background-image: none; border-right-width: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_75.png" width="345" height="260" /></p>

<p>Another service operation in the workflow (GetItemMetadata) will also have a ReceiveIdentityInspector surrounding its Receive activity. The identity inspector will store the identity of this service operation in a workflow variable called GetItemMetadataIdentity. One of the first things this service operation must do is ensure that the user invoking the service operation is the same user who “owns” the service session. </p>

<p><img style="background-image: none; border-right-width: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_76.png" width="335" height="481" /></p>

<p>The <em>Invalid Identity</em> activity seen above will run this security check by evaluating the following condition. </p>

<p><img style="background-image: none; border-right-width: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px; padding-top: 0px" title="image" border="0" alt="image" src="//blogfiles/image_77.png" width="503" height="190" /></p>

<p>A SecurityException will be thrown if this condition is not met.</p>

<p>We have seen here how a custom activity can make it very easy to get the identity of a client calling a workflow service and then use that identity for further validation across the lifetime of the workflow. This activity can be found my <a href="http://neovolve.codeplex.com/releases/view/53499" target="_blank">Neovolve.Toolkit 1.1 project</a> (in beta at time of posting) on CodePlex.</p>
