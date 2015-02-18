---
title: Custom Windows Workflow activity for dependency resolution–Part 5
categories : .Net
date: 2010-09-30 17:04:00 +10:00
---

The [previous post][0] covers the initial background for designer support of custom Windows Workflow activities. This post outlines a customised version of the updatable generic type support outlined in [this post][1] that is specific to the InstanceResolver activity.

One of my [original design goals][2] for this custom activity was to provide adequate designer support. The initial version of this custom activity resolved a single dependency. This was clearly limited as I often have multiple instance resolutions that I use in a workflow. A simple workaround would be to nest several of these activities to achieve the desired result however this would result in a very messy workflow design.

The [implementation][3] of the InstanceResolver activity avoids this scenario by supporting up to 16 dependency resolutions. This presents a usability issue with the designer support for the activity. The activity will provide 16 potential dependency resolutions even when just one or two are used. The activity designer addresses this by leveraging the ArgumentCount property of InstanceResolver that determines how many arguments are used by the activity. One area that this property value is used is in the behaviour of the updatable generic type support.

The InstanceResolverTypeUpdater class shown below is very similar to the GenericTypeUpdater provided in [this post][1]. {% highlight csharp linenos %}
namespace Neovolve.Toolkit.Workflow.Design
{
    using System;
    using System.Activities;
    using System.Activities.Presentation;
    using System.Activities.Presentation.Model;
    using System.Linq;
    using Neovolve.Toolkit.Workflow.Activities;
     
    public static class InstanceResolverTypeUpdater
    {
        private const String DisplayName = "DisplayName";
    
        public static void AttachUpdatableArgumentTypes(ModelItem modelItem)
        {
            AttachUpdatableArgumentTypes(modelItem, Int32.MaxValue);
        }
    
        public static void AttachUpdatableArgumentTypes(ModelItem modelItem, Int32 maximumUpdatableTypes)
        {
            Type[] genericArguments = modelItem.ItemType.GetGenericArguments();
    
            if (genericArguments.Any() == false)
            {
                return;
            }
    
            Int32 argumentCount = genericArguments.Length;
            Int32 updatableArgumentCount = Math.Min(argumentCount, maximumUpdatableTypes);
            EditingContext editingContext = modelItem.GetEditingContext();
            AttachedPropertiesService attachedPropertiesService = editingContext.Services.GetService<AttachedPropertiesService>();
    
            for (Int32 index = 0; index < updatableArgumentCount; index++)
            {
                AttachUpdatableArgumentType(modelItem, attachedPropertiesService, index, updatableArgumentCount);
            }
        }
    
        public static void UpdateModelType(ModelItem modelItem, Type newType, GenericArgumentCount previousArguments)
        {
            EditingContext editingContext = modelItem.GetEditingContext();
            Object instanceOfNewType = Activator.CreateInstance(newType);
            ModelItem newModelItem = ModelFactory.CreateItem(editingContext, instanceOfNewType);
    
            using (ModelEditingScope editingScope = newModelItem.BeginEdit("Change type argument"))
            {
                MorphHelper.MorphObject(modelItem, newModelItem);
                MorphHelper.MorphProperties(modelItem, newModelItem);
    
                Type itemType = modelItem.ItemType;
    
                if (itemType.IsSubclassOf(typeof(Activity)) && newType.IsSubclassOf(typeof(Activity)))
                {
                    GenericArgumentCount argumentCount =
                        (GenericArgumentCount)modelItem.Properties[InstanceResolverDesignerExtension.Arguments].ComputedValue;
    
                    if (DisplayNameRequiresUpdate(modelItem, previousArguments))
                    {
                        // Update to the new display name
                        String newDisplayName = InstanceResolver.GenerateDisplayName(newType, argumentCount);
    
                        newModelItem.Properties[DisplayName].SetValue(newDisplayName);
                    }
                }
    
                DesignerUpdater.UpdateModelItem(modelItem, newModelItem);
    
                editingScope.Complete();
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
    
        private static Boolean DisplayNameRequiresUpdate(ModelItem modelItem, GenericArgumentCount previousArgumentCount)
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
            String defaultDisplayName = InstanceResolver.GenerateDisplayName(modelItem.ItemType, previousArgumentCount);
    
            currentDisplayName = currentDisplayName.Replace(" ", String.Empty);
            defaultDisplayName = defaultDisplayName.Replace(" ", String.Empty);
    
            if (String.Equals(currentDisplayName, defaultDisplayName, StringComparison.Ordinal))
            {
                return true;
            }
    
            return false;
        }
    
        private static Type GetTypeArgument(ModelItem modelItem, Int32 argumentIndex)
        {
            return modelItem.ItemType.GetGenericArguments()[argumentIndex];
        }
    
        private static void UpdateTypeArgument(ModelItem modelItem, Int32 argumentIndex, Type newGenericType)
        {
            Type itemType = modelItem.ItemType;
            GenericArgumentCount previousArgumentCount =
                (GenericArgumentCount)modelItem.Properties[InstanceResolverDesignerExtension.Arguments].ComputedValue;
            Type[] genericTypes = itemType.GetGenericArguments();
    
            // Replace the type being changed
            genericTypes[argumentIndex] = newGenericType;
    
            Type newType = itemType.GetGenericTypeDefinition().MakeGenericType(genericTypes);
    
            UpdateModelType(modelItem, newType, previousArgumentCount);
        }
    }
}
{% endhighlight %}

This implementation is different to the GenericTypeUpdater in that it provides some specialised support regarding the ArgumentCount property of the ModelItem. The ArgumentCount property affects this updatable generic type support in two ways. 

Firstly it only attaches updatable generic type properties for as many generic types as is defined by ArgumentCount. If ArgumentCount = One, only one AttachedProperty<Type> is attached to the ModelItem in the designer. If ArgumentCount = Two, then the activity has two AttachedProperty<Type> attached to the ModelItem. As so it goes on.![image[10]][4]

Secondly the support limits updating the default display name of the activity to the number of types defined by ArgumentCount even though the InstanceResolver class defines 16 generic arguments. This means that the display name is for example InstanceResolver<String, ITestInstance> where ArgumentCount = Two rather than InstanceResolver<String, ITestInstance, Object, Object, Object……T16>.![image[12]][5]

This post has provided a custom updatable generic type support specific to the InstanceResolver activity. The next post will look at the XAML designer support for the InstanceResolver activity and further uses of attached properties.

[0]: /post/2010/09/30/Custom-Windows-Workflow-activity-for-dependency-resolutione28093Part-4.aspx
[1]: /post/2010/09/30/Creating-updatable-generic-Windows-Workflow-activities.aspx
[2]: /post/2010/09/16/Custom-Windows-Workflow-activity-for-dependency-resolutione28093Part-1.aspx
[3]: /post/2010/09/30/Custom-Windows-Workflow-activity-for-dependency-resolutione28093Part-3.aspx
[4]: //blogfiles/image%5B10%5D.png
[5]: //blogfiles/image%5B12%5D.png
