---
title: Custom Windows Workflow activity for dependency resolution–Part 6
categories: .Net
tags: WPF
date: 2010-10-01 13:10:00 +10:00
---

The [previous post][0] in this series provided a custom updatable generic type argument implementation for the InstanceResolver activity. This post will look at the the XAML designer support for this activity.

The designer support for the InstanceResolver intends to display only the number of dependency resolutions that are configured according to the ArgumentCount property. Each dependency resolution shown needs to provide editing functionality for the resolution type, resolution name and the name of the handler reference.![][1]![image][2]

The XAML for the designer defines the activity icon, display for each argument and the child activity to execute. Each of the arguments is bound to an attached property that defines whether that argument is visible to the designer.

<!--more-->

{% highlight xml %}
<sap:ActivityDesigner x:Class="Neovolve.Toolkit.Workflow.Design.Presentation.InstanceResolverDesigner"
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
                        <BitmapImage UriSource="brick.png"></BitmapImage>
                    </ImageDrawing.ImageSource>
                </ImageDrawing>
            </DrawingBrush.Drawing>
        </DrawingBrush>
    </sap:ActivityDesigner.Icon>
    <sap:ActivityDesigner.Resources>
    <conv:ModelToObjectValueConverter x:Key="modelItemConverter"
                                            x:Uid="sadm:ModelToObjectValueConverter_1" />
        <conv:ArgumentToExpressionConverter x:Key="expressionConverter" />
        <ObjectDataProvider MethodName="GetValues"
                            ObjectType="{x:Type s:Enum}"
                            x:Key="EnumSource">
            <ObjectDataProvider.MethodParameters>
                <x:Type TypeName="ntw:GenericArgumentCount" />
            </ObjectDataProvider.MethodParameters>
        </ObjectDataProvider>
        <DataTemplate x:Key="Collapsed">
            <TextBlock HorizontalAlignment="Center"
                        FontStyle="Italic"
                        Foreground="Gray">
                Double-click to view
            </TextBlock>
        </DataTemplate>
        <DataTemplate x:Key="Expanded">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>
        <StackPanel Orientation="Vertical">
            <StackPanel Orientation="Horizontal"
                            Margin="2">
            <TextBlock VerticalAlignment="Center">Argument Count:</TextBlock>
            <ComboBox x:Name="ArgumentCountList"
                                ItemsSource="{Binding Source={StaticResource EnumSource}}"
                                SelectedItem="{Binding Path=ModelItem.Arguments, Mode=TwoWay}"/>
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2" Visibility="{Binding Path=ModelItem.ArgumentVisible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="false"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument1.Name}"
                                MinWidth="80" />
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName1, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2" Visibility="{Binding Path=ModelItem.Argument1Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="false"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType1, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument1.Name}"
                                MinWidth="80" />
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName1, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument2Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType2, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument2.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName2, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument3Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType3, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument3.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName3, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument4Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType4, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument4.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName4, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument5Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType5, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument5.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName5, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument6Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType6, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument6.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName6, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument7Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType7, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument7.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName7, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument8Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType8, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument8.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName8, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument9Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType9, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument9.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName9, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument10Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType10, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument10.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName10, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument11Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType11, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument11.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName11, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument12Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType12, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument12.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName12, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument13Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType13, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument13.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName13, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument14Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType14, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument14.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName14, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument15Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType15, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument15.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName15, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
            <StackPanel Orientation="Horizontal"
                            Margin="2"
                            Grid.Row="2" Visibility="{Binding Path=ModelItem.Argument16Visible}">
            <sapv:TypePresenter Width="120"
                                        Margin="5"
                                        AllowNull="true"
                                        BrowseTypeDirectly="false"
                                        Label="Target type"
                                        Type="{Binding Path=ModelItem.ArgumentType16, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
                                        Context="{Binding Context}" />
            <TextBox Text="{Binding ModelItem.Body.Argument16.Name}"
                                MinWidth="80" />
    
            <TextBlock VerticalAlignment="Center"
                                Margin="2">
                        with name
            </TextBlock>
    
            <sapv:ExpressionTextBox Expression="{Binding Path=ModelItem.ResolutionName16, Converter={StaticResource expressionConverter}}"
                                            ExpressionType="s:String"
                                            OwnerActivity="{Binding ModelItem}"
                                            Margin="2" />
            </StackPanel>
    
        </StackPanel>
            
                <sap:WorkflowItemPresenter Item="{Binding ModelItem.Body.Handler}"
                                            HintText="Drop activity"
                                            Grid.Row="1"
                                            Margin="6" />
            </Grid>
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

There is a lot of duplication in this XAML for each of the argument definitions and I am quite embarrassed by this poor implementation. It is a result of having next to zero WPF experience and needing to trade-off my desire for perfection with the demand for my time to work on other projects. I attempted a UserControl to reduce this duplication but hit many hurdles around binding the expression of the ResolutionName properties through to the ExpressionTextBox in the UserControl. Hopefully greater minds will be able to contribute a better solution for this part of the series.

The code behind this designer detects a new ModelItem being assigned and then attaches properties to it that are bound to in the XAML.

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Design.Presentation
{
    using System;
    using System.Diagnostics;
    using Neovolve.Toolkit.Workflow.Activities;
    
    public partial class InstanceResolverDesigner
    {
        [DebuggerNonUserCode]
        public InstanceResolverDesigner()
        {
            InitializeComponent();
        }
    
        protected override void OnModelItemChanged(Object newItem)
        {
            InstanceResolverDesignerExtension.Attach(ModelItem);
    
            base.OnModelItemChanged(newItem);
        }
    }
}
{% endhighlight %}

The InstanceResolverDesignerExtension class creates attached properties to manage the InstanceResolver.ArgumentCount property and the set of properties that control the argument visibility state. 

{% highlight csharp %}
namespace Neovolve.Toolkit.Workflow.Design
{
    using System;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Model;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    
    public class InstanceResolverDesignerExtension
    {
        internal const String Arguments = "Arguments";
    
        private const String ArgumentCount = "ArgumentCount";
    
        private readonly Dictionary<String, AttachedProperty> _attachedProperties = new Dictionary<String, AttachedProperty>();
    
        private GenericArgumentCount _previousArguments;
    
        public static void Attach(ModelItem modelItem)
        {
            EditingContext editingContext = modelItem.GetEditingContext();
            InstanceResolverDesignerExtension designerExtension = editingContext.Services.GetService<InstanceResolverDesignerExtension>();
    
            if (designerExtension == null)
            {
                designerExtension = new InstanceResolverDesignerExtension();
    
                editingContext.Services.Publish(designerExtension);
            }
    
            designerExtension.AttachToModelItem(modelItem);
        }
    
        private static AttachedProperty<Visibility> AttachArgumentVisibleProperty(
            AttachedPropertiesService attachedPropertiesService, ModelItem modelItem, Int32 argumentNumber)
        {
            String propertyName;
    
            if (argumentNumber > 0)
            {
                propertyName = "Argument" + argumentNumber + "Visible";
            }
            else
            {
                propertyName = "ArgumentVisible";
            }
    
            AttachedProperty<Visibility> attachedProperty = new AttachedProperty<Visibility>
                                                            {
                                                                IsBrowsable = false, 
                                                                Name = propertyName, 
                                                                OwnerType = modelItem.ItemType, 
                                                                Getter = delegate(ModelItem modelReference)
                                                                            {
                                                                                Int32 argumentCount =
                                                                                    (Int32)modelReference.Properties[Arguments].ComputedValue;
    
                                                                                if (argumentNumber == 0 && argumentCount == 1)
                                                                                {
                                                                                    return Visibility.Visible;
                                                                                }
    
                                                                                if (argumentNumber != 0 && argumentCount > 1 &&
                                                                                    argumentNumber <= argumentCount)
                                                                                {
                                                                                    return Visibility.Visible;
                                                                                }
    
                                                                                return Visibility.Collapsed;
                                                                            }, 
                                                                Setter = delegate(ModelItem modelReference, Visibility newValue)
                                                                            {
                                                                                Debug.WriteLine("Visibility updated to " + newValue);
                                                                            }
                                                            };
    
            attachedPropertiesService.AddProperty(attachedProperty);
    
            return attachedProperty;
        }
    
        private void AttachArgumentsProperty(ModelItem modelItem, AttachedPropertiesService attachedPropertiesService)
        {
            AttachedProperty<GenericArgumentCount> argumentsProperty = new AttachedProperty<GenericArgumentCount>
                                                                        {
                                                                            Name = Arguments, 
                                                                            IsBrowsable = true, 
                                                                            OwnerType = modelItem.ItemType, 
                                                                            Getter = delegate(ModelItem modelReference)
                                                                                    {
                                                                                        return
                                                                                            (GenericArgumentCount)
                                                                                            modelReference.Properties[ArgumentCount].ComputedValue;
                                                                                    }, 
                                                                            Setter = delegate(ModelItem modelReference, GenericArgumentCount newValue)
                                                                                    {
                                                                                        _previousArguments =
                                                                                            (GenericArgumentCount)
                                                                                            modelReference.Properties[ArgumentCount].ComputedValue;
    
                                                                                        modelReference.Properties[ArgumentCount].ComputedValue =
                                                                                            newValue;
    
                                                                                        // Update the activity to use InstanceResolver with the correct number of arguments
                                                                                        UpgradeInstanceResolverType(modelReference);
                                                                                    }
                                                                        };
    
            attachedPropertiesService.AddProperty(argumentsProperty);
        }
    
        private void AttachToModelItem(ModelItem modelItem)
        {
            // Get the number of arguments from the model
            GenericArgumentCount argumentCount = (GenericArgumentCount)modelItem.Properties[ArgumentCount].ComputedValue;
            EditingContext editingContext = modelItem.GetEditingContext();
            AttachedPropertiesService attachedPropertiesService = editingContext.Services.GetService<AttachedPropertiesService>();
    
            // Store the previous argument count value to track the changes to the property
            _previousArguments = argumentCount;
    
            // Attach updatable type arguments
            InstanceResolverTypeUpdater.AttachUpdatableArgumentTypes(modelItem, (Int32)argumentCount);
    
            AttachArgumentsProperty(modelItem, attachedPropertiesService);
    
            // Start from the second argument because there must always be at least one instance resolution per InstaceResolver
            for (Int32 index = 0; index <= 16; index++)
            {
                // Create an attached property that calculates the designer visibility for a stack panel
                AttachedProperty<Visibility> argumentVisibleProperty = AttachArgumentVisibleProperty(attachedPropertiesService, modelItem, index);
    
                _attachedProperties[argumentVisibleProperty.Name] = argumentVisibleProperty;
            }
        }
    
        private void UpgradeInstanceResolverType(ModelItem modelItem)
        {
            Type[] originalGenericTypes = modelItem.ItemType.GetGenericArguments();
            Type[] genericTypes = new Type[16];
    
            originalGenericTypes.CopyTo(genericTypes, 0);
    
            if (originalGenericTypes.Length < genericTypes.Length)
            {
                Type defaultGenericType = typeof(Object);
    
                // This type is being upgraded from InstanceResolver to InstanceResolver
                // Initialize the generic types with Object
                for (Int32 index = originalGenericTypes.Length; index < genericTypes.Length; index++)
                {
                    genericTypes[index] = defaultGenericType;
                }
            }
    
            Type newType = RegisterMetadata.InstanceResolverT16GenericType.MakeGenericType(genericTypes);
    
            InstanceResolverTypeUpdater.UpdateModelType(modelItem, newType, _previousArguments);
        }
    }
}
{% endhighlight %}

An attached property is used for the ArgumentCount property in order to track changes to the property. Updates to this property then cause an update to the ModelItem.There did not seem to be any other way to detect changes to this property from the designer code when binding directly to the property on the activity.

There were a few other designer quirks discovered throughout this exercise. 

1. The binding to the visibility attached properties did not correctly update in the designer when the values were changed. The property is actually only a Getter in its functionality as it is a calculation based on the current argument number against the total number of displayed arguments. The way to get the binding to be updatable was to provide a Setter that did nothing.
1. Attached properties marked as IsBrowsable = false are not available via the ModelItem.Properties collection. The solution here was to cache these attached properties against the extension (service) stored against the services of the associated EditingContext.
1. Attached properties cannot be removed once they are attached. This affected the desired outcome for the property grid experience.
1. Attached properties cannot have custom attributes assigned to them so there is no support for Description or Category values in the property grid.
    
The designer support using this implementation is usable but far from perfect. The biggest issue is synchronisation between the ModelItem on the designer and the property grid. The property grid can get a little out of sync especially with changes to the Arguments property. The problem here is the ability to refresh/update the contents of the property grid when certain events occur on the ModelItem in the designer. Interacting with the activity on the design surface directly seems to provide the most reliable result.

This post has covered the designer support for the InstanceResolver activity and provided information on some gotchas for working with activity designers. The posts in this series have covered all aspects of implementing a custom activity for resolving dependencies within WF4 workflows.

[0]: /2010/09/30/custom-windows-workflow-activity-for-dependency-resolutione28093part-5/
[1]: /files/image_36.png
[2]: /files/image_37.png
