---
title: Extract WCF identity into a WorkflowServiceHost activity
categories : .Net
tags : WCF, WF, WIF
date: 2011-02-21 11:02:54 +10:00
---


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


    
    



    
    
    


[0]: http://social.msdn.microsoft.com/Forums/en-US/wfprerelease/thread/7ff447ff-5a7b-40e3-ad9e-db8c9847bfbb/#491737e4-1f06-4aa2-b3cd-9b3cfcb51cd7
[1]: http://zamd.net/2010/07/04/using-wif-with-workflow-services/
[2]: http://msmvps.com/blogs/theproblemsolver/archive/2010/09/21/using-the-wcf-operationcontext-from-a-receive-activity.aspx
[3]: http://wf.codeplex.com/releases/view/48114
[4]: http://msdn.microsoft.com/en-us/library/system.servicemodel.activities.ireceivemessagecallback.aspx
[5]: /2010/09/30/Creating-updatable-generic-Windows-Workflow-activities/
[6]: /blogfiles/image_72.png
[7]: /blogfiles/image_73.png
[8]: /blogfiles/image_74.png
[9]: /blogfiles/image_75.png
[10]: /blogfiles/image_76.png
[11]: /blogfiles/image_77.png
[12]: http://neovolve.codeplex.com/releases/view/53499