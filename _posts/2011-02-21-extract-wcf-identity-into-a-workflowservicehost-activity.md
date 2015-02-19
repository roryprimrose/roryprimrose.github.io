---
title: Extract WCF identity into a WorkflowServiceHost activity
categories : .Net
tags : WCF, WF, WIF
date: 2011-02-21 11:02:54 +10:00
---

I came across an issue with the combination of WCF, WF, WIF and AppFabric in December that had me a little worried. The issue was how to get the identity of the user calling a WCF service inside a WorkflowServiceHost workflow when using WIF to manage security and AppFabric for WF persistence.   

The WIF documentation says the following:  

> _As a note, when WIF is enabled inside WCF, the WCF ServiceSecurityContext does not work for obtaining the caller’s identity and claims; the application code must use the IClaimsPrincipal to access the caller’s information. You can obtain the IClaimsPrincipal instance using Thread.CurrentPrincipal. This is available for every authentication type as soon as WIF is enabled for the WCF application._ 

I had been developing my service application using Thread.CurrentPrincipal based on this information. Everything was fine until I started using WF persistence. Unfortunately enabling persistence (via AppFabric) had the affect of wiping out Thread.CurrentPrincipal.  

The requirement for persistence is so that the service can throw a fault back to the client and allow the workflow instance to remain alive (persisted) so that it can be called again. An example of this scenario is where a validation fault is thrown in the middle of an existing service session. The service needs to be persisted so that it can be called again by the client otherwise the session will be lost. So the question is how do we get the identity of the user when we have both WIF and WF persistence running a WCF service?   

I put the question out on the [MSDN forum][0] at emailed a few of the prominent WF guru’s for help. Both [Zulfiqar][1] and [Maurice][2] have the answer to this riddle. It turns out that the WIF documentation is not entirely correct as the security information about the client is still available via ServiceSecurityContext.   

I have created a custom WF activity that will make the calling identity available to the workflow. I wanted my activity to be more specific than the OperationContextScope activity available in the [WF security pack][3] on Codeplex in order for it to be intuitive and easy to use. This activity does however take the same implementation as the OperationContextScope activity.  

The ReceiveIdentityInspector activity below hooks into the WCF pipeline using a private inspector class that implements [IReceiveMessageCallback][4]. This interface allows the inspector class to look at the client identity of the WCF service that has executed the Receive activity in the workflow service. The inspector instance is added as a property on the WF context so that it gets executed. The activity can then get the identity from the inspector and make it available to the parent workflow. The Body property of the activity is of type Receive as it only supports a Receive activity as its single child activity. This helps to make a nice drag\drop experience on the designer and prevent the user dragging inappropriate activities on to it.  

{% highlight csharp linenos %}
namespace Neovolve.Toolkit.Workflow.Activities
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

    [ToolboxBitmap(typeof(ReceiveIdentityInspector<>), "user.png")]
    [ContentProperty("Body")]
    [DefaultTypeArgument(typeof(IIdentity))]
    public sealed class ReceiveIdentityInspector<T> : NativeActivity<T> where T : class, IIdentity
    {
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            if (Body == null)
            {
                ValidationError nullBodyFailure = new ValidationError(Resources.OperationIdentityInspector_NullBodyFailure, false, "Body");

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

            Debug.Assert(inspector != null, "No inspector was found in the context properties");

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
}
{% endhighlight %}

The designer of the activity allows for a child activity to be dragged and dropped onto it by using a WorkflowItemPresenter. It also supports the normal collapse/expand style behaviour.

{% highlight xml linenos %}
<sap:ActivityDesigner x:Class="Neovolve.Toolkit.Workflow.Design.Presentation.ReceiveIdentityInspectorDesigner"
                      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                      xmlns:sap="clr-namespace:System.Activities.Presentation;assembly=System.Activities.Presentation"
                      xmlns:Activities="clr-namespace:System.ServiceModel.Activities;assembly=System.ServiceModel.Activities">
  <sap:ActivityDesigner.Icon>
    <DrawingBrush>
      <DrawingBrush.Drawing>
        <ImageDrawing>
          <ImageDrawing.Rect>
            <Rect Location="0,0" Size="16,16" ></Rect>
          </ImageDrawing.Rect>
          <ImageDrawing.ImageSource>
            <BitmapImage UriSource="user.png" ></BitmapImage>
          </ImageDrawing.ImageSource>
        </ImageDrawing>
      </DrawingBrush.Drawing>
    </DrawingBrush>
  </sap:ActivityDesigner.Icon>
    <sap:ActivityDesigner.Resources>
        <DataTemplate x:Key="Collapsed">
            <TextBlock HorizontalAlignment="Center"
                       FontStyle="Italic"
                       Foreground="Gray">
                Double-click to view
            </TextBlock>
        </DataTemplate>
        <DataTemplate x:Key="Expanded">
      <sap:WorkflowItemPresenter AllowedItemType="{x:Type Activities:Receive}" Item="{Binding ModelItem.Body}"
                                       HintText="Drop activity"
                                       Margin="6" />
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

The code behind the designer adds support for the generic IIdentity type of the activity to be modified using a [previously posted][5] helper class.

{% highlight csharp linenos %}
namespace Neovolve.Toolkit.Workflow.Design.Presentation
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
}
{% endhighlight %}

The designer looks like the following. The ReceiveIdentityInspector&lt;IIdentity&gt; seen below contains a Receive activity as its only child activity.

![image][6]

The activity will expose the identity of the user calling the GetData service operation and make it available in its Result property.
    
![image][7]

In this example the user identity is stored in a workflow variable called CallingIdentity. The workflow can then refer to this variable when it needs to take any identity related action.
    
![image][8]

One side effect of this implementation is that the user identity will now automatically participate in workflow persistence because it is stored in a workflow variable. This can be useful for security validation of a workflow that simulates a service session using content correlation. 

In the following example, a service has a StartSession operation that returns a System.Guid and must be the first operation invoked on the service. A service session is simulated as all other methods on the service accept this Guid value. Content correlation then ensures that correct workflow instance is associated with the subsequent service calls matching on the Guid. 

The security scenario to cover is if another authenticated user hijacks the session by providing the correct Guid for the session of another user. 

Persisting the identity that called the StartSession service operation in a high enough scope will make it available to the entire service session lifetime. In this case the ReceiveIdentityInspector surrounding StartSession will store the identity in a workflow variable called StartSessionIdentity. The workflow is then persisted after the StartSession service operation has completed and waits for the client to invoke the next service operation using the correct Guid value.
    
![image][9]

Another service operation in the workflow (GetItemMetadata) will also have a ReceiveIdentityInspector surrounding its Receive activity. The identity inspector will store the identity of this service operation in a workflow variable called GetItemMetadataIdentity. One of the first things this service operation must do is ensure that the user invoking the service operation is the same user who “owns” the service session. 
    
![image][10]

The _Invalid Identity_ activity seen above will run this security check by evaluating the following condition. 
    
![image][11]

A SecurityException will be thrown if this condition is not met.

We have seen here how a custom activity can make it very easy to get the identity of a client calling a workflow service and then use that identity for further validation across the lifetime of the workflow. This activity can be found my [Neovolve.Toolkit 1.1 project][12] (in beta at time of posting) on CodePlex.

[0]: http://social.msdn.microsoft.com/Forums/en-US/wfprerelease/thread/7ff447ff-5a7b-40e3-ad9e-db8c9847bfbb/#491737e4-1f06-4aa2-b3cd-9b3cfcb51cd7
[1]: http://zamd.net/2010/07/04/using-wif-with-workflow-services/
[2]: http://msmvps.com/blogs/theproblemsolver/archive/2010/09/21/using-the-wcf-operationcontext-from-a-receive-activity.aspx
[3]: http://wf.codeplex.com/releases/view/48114
[4]: http://msdn.microsoft.com/en-us/library/system.servicemodel.activities.ireceivemessagecallback.aspx
[5]: /2010/09/30/Creating-updatable-generic-Windows-Workflow-activities/
[6]: /files/image_72.png
[7]: /files/image_73.png
[8]: /files/image_74.png
[9]: /files/image_75.png
[10]: /files/image_76.png
[11]: /files/image_77.png
[12]: http://neovolve.codeplex.com/releases/view/53499