---
title: Creating updatable generic Windows Workflow activities
categories: .Net
date: 2010-09-30 14:17:00 +10:00
---

This post is a segue from the current series on building a custom activity for supporting dependency resolution in Windows Workflow ([here][0], [here][1], [here][2] and [here][3] so far). This post will outline how to support updating generic type arguments of generic activities in the designer. This technique is used in the designer support for the InstanceResolver activity that has been discussed throughout the series.

The implementation of this is modelled from the support for this functionality in the generic WF4 activities such as ForEach&lt;T&gt; and ParallelForEach&lt;T&gt;. Unfortunately the logic that drives this is marked as internal and is therefore not available to developers who create custom generic activities.

In the case of the ForEach&lt;T&gt; activity, the default generic type value used is int. ![image][4]

<!--more-->

This can be changed in the property grid of the activity using the TypeArgument property. ![image][5]

Changing this value will update the definition of the activity with the new type argument. For example, the type could be change to Boolean.![image][6]

This post will use my ExecuteBookmark&lt;T&gt; activity to demonstrate this functionality. This activity provides the reusable structure for persisting and resuming workflows.

```csharp
namespace Neovolve.Toolkit.Workflow.Activities
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
     
    [ToolboxBitmap(typeof(ExecuteBookmark), "book_open.png")]
    [DefaultTypeArgument(typeof(String))]
    public sealed class ExecuteBookmark<T> : NativeActivity<T>
    {
        protected override void Execute(NativeActivityContext context)
        {
            String bookmarkName = context.GetValue(BookmarkName);
    
            if (String.IsNullOrWhiteSpace(bookmarkName))
            {
                throw new ArgumentNullException("BookmarkName");
            }
                
            context.CreateBookmark(bookmarkName, BookmarkResumed);
        }
    
        private void BookmarkResumed(NativeActivityContext context, Bookmark bookmark, Object value)
        {
            T newValue = (T)Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
    
            Result.Set(context, newValue);
        }
    
        [RequiredArgument]
        [Category("Inputs")]
        [Description("The name used to identify the bookmark")]
        public InArgument<String> BookmarkName
        {
            get;
            set;
        }
            
        protected override Boolean CanInduceIdle
        {
            get
            {
                return true;
            }
        }
    }
}
```

This activity defines the default type of String. Designer support for changing this type is required after dropping the activity on the designer because the DefaultArgumentTypeAttribute avoids the developer having to define the generic type up front. It has the additional benefit of allowing the developer to change the activity type once it is is already on the designer as the workflow is developed and refactored.

The ArgumentType property does not exist on the ExecuteBookmark&lt;T&gt; class. It is an AttachedProperty&lt;Type&gt; instance attached to the ModelItem that represents the activity on the design surface. The setter of this property provides the notification that the type is being changed. The designer attaches the property to the ModelItem in the activity designer when a new ModelItem instance is assigned.

```csharp
namespace Neovolve.Toolkit.Workflow.Design.Presentation
{
    using System;
    using System.Diagnostics;
    using Neovolve.Toolkit.Workflow.Activities;
    
    public partial class ExecuteBookmarkTDesigner
    {
        [DebuggerNonUserCode]
        public ExecuteBookmarkTDesigner()
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
```

The designer calls down into a custom GenericArgumentTypeUpdater class to attach the updatable type functionality to the ModelItem. Unlike the internal Microsoft implementation, this class supports multiple generic type arguments.

```csharp
namespace Neovolve.Toolkit.Workflow.Design
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Model;
    using System.Linq;
    
    public static class GenericArgumentTypeUpdater
    {
        private const String DisplayName = "DisplayName";
    
        public static void Attach(ModelItem modelItem)
        {
            Attach(modelItem, Int32.MaxValue);
        }
    
        public static void Attach(ModelItem modelItem, Int32 maximumUpdatableTypes)
        {
            Type[] genericArguments = modelItem.ItemType.GetGenericArguments();
    
            if (genericArguments.Any() == false)
            {
                return;
            }
    
            Int32 argumentCount = genericArguments.Length;
            Int32 updatableArgumentCount = Math.Min(argumentCount, maximumUpdatableTypes);
            EditingContext context = modelItem.GetEditingContext();
            AttachedPropertiesService attachedPropertiesService = context.Services.GetService<AttachedPropertiesService>();
    
            for (Int32 index = 0; index < updatableArgumentCount; index++)
            {
                AttachUpdatableArgumentType(modelItem, attachedPropertiesService, index, updatableArgumentCount);
            }
        }
    
        private static void AttachUpdatableArgumentType(
            ModelItem modelItem, AttachedPropertiesService attachedPropertiesService, Int32 argumentIndex, Int32 argumentCount)
        {
            String propertyName = "ArgumentType";
    
            if (argumentCount > 1)
            {
                propertyName += argumentIndex + 1;
            }
    
            AttachedProperty<Type> attachedProperty = new AttachedProperty<Type>
                                                        {
                                                            Name = propertyName, 
                                                            OwnerType = modelItem.ItemType, 
                                                            IsBrowsable = true
                                                        };
    
            attachedProperty.Getter = (ModelItem arg) => GetTypeArgument(arg, argumentIndex);
            attachedProperty.Setter = (ModelItem arg, Type newType) => UpdateTypeArgument(arg, argumentIndex, newType);
    
            attachedPropertiesService.AddProperty(attachedProperty);
        }
    
        private static bool DisplayNameRequiresUpdate(ModelItem modelItem)
        {
            String currentDisplayName = (String)modelItem.Properties[DisplayName].ComputedValue;
    
            // Sometimes the display name is empty
            if (String.IsNullOrWhiteSpace(currentDisplayName))
            {
                return true;
            }
    
            // The default calculation of a generic type does not include spaces in the generic type arguments
            // However an activity might include these as the default display name
            // Strip spaces to provide a more accurate match
            String defaultDisplayName = GetActivityDefaultName(modelItem.ItemType);
    
            currentDisplayName = currentDisplayName.Replace(" ", String.Empty);
            defaultDisplayName = defaultDisplayName.Replace(" ", String.Empty);
    
            if (String.Equals(currentDisplayName, defaultDisplayName, StringComparison.Ordinal))
            {
                return true;
            }
    
            return false;
        }
    
        private static String GetActivityDefaultName(Type activityType)
        {
            Activity activity = (Activity)Activator.CreateInstance(activityType);
    
            return activity.DisplayName;
        }
    
        private static Type GetTypeArgument(ModelItem modelItem, Int32 argumentIndex)
        {
            return modelItem.ItemType.GetGenericArguments()[argumentIndex];
        }
    
        private static void UpdateTypeArgument(ModelItem modelItem, Int32 argumentIndex, Type newGenericType)
        {
            Type itemType = modelItem.ItemType;
            Type[] genericTypes = itemType.GetGenericArguments();
    
            // Replace the type being changed
            genericTypes[argumentIndex] = newGenericType;
    
            Type newType = itemType.GetGenericTypeDefinition().MakeGenericType(genericTypes);
            EditingContext editingContext = modelItem.GetEditingContext();
            Object instanceOfNewType = Activator.CreateInstance(newType);
            ModelItem newModelItem = ModelFactory.CreateItem(editingContext, instanceOfNewType);
    
            using (ModelEditingScope editingScope = newModelItem.BeginEdit("Change type argument"))
            {
                MorphHelper.MorphObject(modelItem, newModelItem);
                MorphHelper.MorphProperties(modelItem, newModelItem);
    
                if (itemType.IsSubclassOf(typeof(Activity)) && newType.IsSubclassOf(typeof(Activity)))
                {
                    if (DisplayNameRequiresUpdate(modelItem))
                    {
                        // Update to the new display name
                        String newDisplayName = GetActivityDefaultName(newType);
    
                        newModelItem.Properties[DisplayName].SetValue(newDisplayName);
                    }
                }
    
                DesignerUpdater.UpdateModelItem(modelItem, newModelItem);
    
                editingScope.Complete();
            }
        }
    }
}
```

The class determines how many generic type arguments on the activity will be updatable. It then loops through this number and creates an attached property on the ModelItem for each of these. The AttachedProperty is marked as IsBrowsable = true so that it is displayed in the property grid.

The getter Func&lt;T&gt; of the attached property simply returns the generic type argument of the current activity type for the index related to the attached property. The setter is where all the action happens. It is the logic behind the attached property that was copied from Microsoft internal implementation.

Updating the type involves calculating what the new type will be. For example, SomeActivity&lt;String, Boolean&gt; could be updated to SomeActivity&lt;String, Int32&gt;. This new type is determined and an instance of it is created. The instance of the new type is used to create a new ModelItem for the designer.

An ModelEditingScope is used at this point in order to group a set of designer changes into one unit. This means that there will be one Undo/Redo command in Visual Studio rather than one for each individual designer change detected in this process. 

The editing scope uses a MorphHelper to create the new type from the old type. This process will copy across all the supported changes from the old type to the new type (properties, child activities etc). 

The next job is to detect if the activity has the default display name value. If this is the case, then the display name will be updated to the default display name of the new activity type. This is done because the display name of generic activities is normally calculated as TypeName&lt;TypeName, TypeName, etc, etc&gt;.

Lastly, the class makes a call into a DesignerUpdater helper class that is used to ensure that the updated activity is selected.

```csharp
namespace Neovolve.Toolkit.Workflow.Design
{
    using System;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Model;
    using System.Activities.Presentation.View;
    using System.Windows.Threading;
    
    internal sealed class DesignerUpdater
    {
        public static void UpdateModelItem(ModelItem originalItem, ModelItem updatedItem)
        {
            DesignerUpdater class2 = new DesignerUpdater(originalItem, updatedItem);
    
            Action method = class2.UpdateDesigner;
    
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, method);
        }
    
        internal DesignerUpdater(ModelItem originalItem, ModelItem newItem)
        {
            _originalModelItem = originalItem;
            _newModelItem = newItem;
        }
    
        private readonly ModelItem _originalModelItem;
    
        private readonly ModelItem _newModelItem;
    
        public void UpdateDesigner()
        {
            EditingContext editingContext = _originalModelItem.GetEditingContext();
            DesignerView designerView = editingContext.Services.GetService<DesignerView>();
    
            if ((designerView.RootDesigner != null) && (((WorkflowViewElement)designerView.RootDesigner).ModelItem == _originalModelItem))
            {
                designerView.MakeRootDesigner(_newModelItem);
            }
    
            Selection.SelectOnly(editingContext, _newModelItem);
        }
    }
}
```

The final piece of the puzzle is support for changing the type within the designer surface itself. This is modelled from the InvokeMethod activity that allows for custom types to be defined in the designer.

![image][7]

The way to get this to work is to add the following into the XAML of the activity designer.

```xml
<sap:ActivityDesigner.Resources>
    <conv:ModelToObjectValueConverter x:Key="modelItemConverter"
        x:Uid="sadm:ModelToObjectValueConverter_1" />
</sap:ActivityDesigner.Resources>
    
<sapv:TypePresenter Width="120"
    Margin="5"
    AllowNull="false"
    BrowseTypeDirectly="false"
    Label="Target type"
    Type="{Binding Path=ModelItem.TypeArgument, Mode=TwoWay, Converter={StaticResource modelItemConverter}}"
    Context="{Binding Context}" />
```

This will provide the dropdown list of types for the designer. The first important item to note is that the TypePresenter is bound to the attached property created by GenericArgumentTypeUpdater. The second important item is the binding of the EditingContext. Without the editing context, the dropdown list and associated dialog support will not display references to assemblies and types related to the current workflow.

Using these techniques will allow a custom activity to provide updatable generic type support as part of its design time experience.

[0]: /2010/09/15/dependency-injection-options-for-windows-workflow-4/
[1]: /2010/09/16/custom-windows-workflow-activity-for-dependency-resolutione28093part-1/
[2]: /2010/09/29/custom-windows-workflow-activity-for-dependency-resolutione28093part-2/
[3]: /2010/09/30/custom-windows-workflow-activity-for-dependency-resolutione28093part-3/
[4]: /files/image_32.png
[5]: /files/image_33.png
[6]: /files/image_34.png
[7]: /files/image_35.png
