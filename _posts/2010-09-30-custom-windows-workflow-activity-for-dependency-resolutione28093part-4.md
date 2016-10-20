---
title: Custom Windows Workflow activity for dependency resolution–Part 4
categories: .Net
tags: Extensibility
date: 2010-09-30 16:17:57 +10:00
---

The [posts in this series][0] have looked at providing a custom activity for dependency resolution in Windows Workflow. The series will now take a look at providing designer support for this activity. This post will cover the IRegisterMetadata interface and support for custom morphing.

## Designer Support

The first action to take when creating WF4 activity designer support is to create a new Visual Studio project. The name of this project should be prefixed with the name of the assembly that contains the activities related to the designers. The project should have the suffix of “Design”. In the case of my [Toolkit][1] project, the assembly that contains the custom activities is called Neovolve.Toolkit.Workflow.dll and the designer assembly is called Neovolve.Toolkit.Workflow.Design.dll.

<!--more-->

I’m not a fan of this restriction as my original intention was to group the designers in the same assembly as the activities. I wanted this for the purpose of portability so as to minimise the number of assemblies that developers needed to reference in order to leverage my toolkit. 

The restriction of this project segregation and specific naming is because of the [IRegisterMetadata][2] implementation. In addition to the project segregation, these assemblies must be in the same directory for the IRegisterMetadata implementation to be picked up and executed. These restrictions are not part of the MSDN documentation but are provided in [this forum post][3]. I have found that adding the following post build script to the designer project is very useful to ensure that the assemblies are co-located.

> _copy "$(TargetDir)Neovolve.Toolkit.Workflow.Design.*" "$(SolutionDir)\Neovolve.Toolkit.Workflow\$(OutDir)"_

This script is helpful for testing your activities and their designers with a separate Visual Studio instance. You will obviously need to change the project names to match your own projects for this script to work.

While this setup is restrictive in a sense, the segregation of these projects does have a benefit. A developer may use your custom activities in the activity assembly. Their usage is supplemented with any design time experience provided by the designer assembly. They will not need to redistribute your designer assembly with their own assembly once their development has completed.

It is important to note that the activity assembly should never reference the designer assembly. This is one of the reasons that IRegisterMetadata exists. 

## IRegisterMetadata

An implementation of IRegisterMetadata provides the ability to describe metadata for an activity type in a way that is decoupled from the activity itself. This is the way that an activity designer is associated with an activity because the activity assembly does not have any reference to the designer assembly.

```csharp
namespace Neovolve.Toolkit.Workflow.Design
{
    using System;
    using System.Activities;
    using System.Activities.Presentation.Metadata;
    using System.Activities.Presentation.Model;
    using System.ComponentModel;
    using Neovolve.Toolkit.Workflow.Activities;
    using Neovolve.Toolkit.Workflow.Design.Presentation;
    
    public class RegisterMetadata : IRegisterMetadata
    {
        private static readonly Type _activityActionGenericType =
            typeof(
                ActivityAction
                    <Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>).
                GetGenericTypeDefinition();
    
        private static readonly Type _instanceResolverT16GenericType =
            typeof(
                InstanceResolver
                    <Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object, Object>).
                GetGenericTypeDefinition();
    
        public void Register()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();
    
            builder.AddCustomAttributes(typeof(ExecuteBookmark), new DesignerAttribute(typeof(ExecuteBookmarkDesigner)));
            builder.AddCustomAttributes(typeof(ExecuteBookmark<>), new DesignerAttribute(typeof(ExecuteBookmarkTDesigner)));
            builder.AddCustomAttributes(typeof(GetWorkflowInstanceId), new DesignerAttribute(typeof(GetWorkflowInstanceIdDesigner)));
            builder.AddCustomAttributes(_instanceResolverT16GenericType, new DesignerAttribute(typeof(InstanceResolverDesigner)));
            builder.AddCustomAttributes(typeof(SystemFailureEvaluator), new DesignerAttribute(typeof(SystemFailureEvaluatorDesigner)));
    
            MetadataStore.AddAttributeTable(builder.CreateTable());
    
            MorphHelper.AddPropertyValueMorphHelper(_activityActionGenericType, MorphExtension.MorphActivityAction);
        }
    
        internal static Type InstanceResolverT16GenericType
        {
            get
            {
                return _instanceResolverT16GenericType;
            }
        }
    }
}
```

The RegisterMetadata class seen here is the IRegisterMetadata implementation for my custom workflows. This class does two things. Firstly it associates activity designers with their activities. Secondly it takes the opportunity to add a custom morph action into the MorphHelper class.

Visual Studio searches for the designer assembly when it loads the assembly containing the custom activities. It will then look for a class that implements IRegisterMetadata and execute its Register method.

## MorphHelper

The [previous post][4] provided the implementation for supporting updatable generic activity types. Part of the implementation of this process is a reference to the MorphHelper class. 

MorphHelper is used to copy information between ModelItems that are used to describe an activity in the designer. It is used by updatable generic activity types to copy properties and child activity information (among other things) from the original activity type to the new activity type.

Consider what happens when you change ParallelForEach&lt;String&gt; to ParallelForEach&lt;Boolean&gt;. Any property information assigned to the activity (like the enumerable reference and child activity definition) is copied between the activity definitions even though the two activity types are not the same. This is the power of MorphHelper.

Understandably MorphHelper will not know how to transform any possible data/type structure between ModelItem instances. Thankfully the helper class is extensible as it allows custom morph actions to be added via the AddPropertyValueMorphHelper method. Reflector shows that this is how WF4 configures MorphHelper for the morph actions that come out of the box. These are wired up in the WF4 IRegisterMetadata implementation defined in the System.Activities.Core.Presentation.DesignerMetadata class.

The DesignerMetadata class contains the following default morph actions.

```csharp
MorphHelper.AddPropertyValueMorphHelper(typeof(InArgument<>), new PropertyValueMorphHelper(MorphHelpers.ArgumentMorphHelper));
MorphHelper.AddPropertyValueMorphHelper(typeof(OutArgument<>), new PropertyValueMorphHelper(MorphHelpers.ArgumentMorphHelper));
MorphHelper.AddPropertyValueMorphHelper(typeof(InOutArgument<>), new PropertyValueMorphHelper(MorphHelpers.ArgumentMorphHelper));
MorphHelper.AddPropertyValueMorphHelper(typeof(ActivityAction<>), new PropertyValueMorphHelper(MorphHelpers.ActivityActionMorphHelper));
```

There is support for morphing InArgument&lt;&gt;, OutArgument&lt;&gt;, InOutArgument&lt;&gt; and ActivityAction&lt;&gt; properties between ModelItem types.

The issue I had with creating the updatable type support for InstanceResolver was that the 16 handlers defined against ActivityAction&lt;T1…T1&gt; for the activity were not copied from the old ModelItem to the new ModelItem. The reason for this turned about to be that the morph action for ActivityAction&lt;&gt; only supports a single generic argument whereas the InstanceResolver activity has 16. This is another scenario where the Microsoft implementation is limited to a single generic argument like the limitations of the DefaultTypeArgumentAttribute (as indicated in [this post][0]).

The extensibility support for MorphHelper does however mean that a custom implementation can be provided for ActivityAction&lt;T1…T16&gt;.

```csharp
namespace Neovolve.Toolkit.Workflow.Design
{
    using System;
    using System.Activities;
    using System.Activities.Presentation.Model;
    
    internal static class MorphExtension
    {
        public static Object MorphActivityAction(ModelItem originalValue, ModelProperty newModelProperty)
        {
            Type newActivityActionType = newModelProperty.PropertyType;
            ActivityDelegate newActivityDelegate = (ActivityDelegate)Activator.CreateInstance(newActivityActionType);
            ModelItem newModelItem = ModelFactory.CreateItem(originalValue.GetEditingContext(), newActivityDelegate);
            Type[] genericArguments = newActivityActionType.GetGenericArguments();
    
            for (Int32 index = 1; index <= genericArguments.Length; index++)
            {
                String argumentName = "Argument" + index;
                ModelItem argumentItem = originalValue.Properties[argumentName].Value;
    
                if (argumentItem != null)
                {
                    Type[] delegateTypeList = new[]
                                                {
                                                    genericArguments[index - 1]
                                                };
                    DelegateInArgument argument =
                        (DelegateInArgument)Activator.CreateInstance(typeof(DelegateInArgument<>).MakeGenericType(delegateTypeList));
    
                    argument.Name = (String)argumentItem.Properties["Name"].Value.GetCurrentValue();
                    newModelItem.Properties[argumentName].SetValue(argument);
                }
            }
    
            const String HandlerName = "Handler";
            ModelItem handerItem = originalValue.Properties[HandlerName].Value;
    
            if (handerItem != null)
            {
                // Copy the activity of the activity action
                newModelItem.Properties[HandlerName].SetValue(handerItem);
                originalValue.Properties[HandlerName].SetValue(null);
            }
    
            return newModelItem;
        }
    }
}
```

This code is modelled from the Microsoft implementation of ActivityAction&lt;&gt; morphing. This implementation however has full support for multiple generic types. 

With this method registered in MorphHelper via the above RegisterMetadata class, changing a generic type in my InstanceResolver class now correctly morphs the internals of the activity from the old type to the new type.

This post has covered support for IRegisterMetadata and MorphHelper. The next post will look at specialised implementation for updatable generic type support for the InstanceResolver activity.

[0]: /2010/09/30/custom-windows-workflow-activity-for-dependency-resolutione28093part-3/
[1]: http://neovolve.codeplex.com/SourceControl/changeset/view/67193#1413007
[2]: http://msdn.microsoft.com/en-us/library/microsoft.windows.design.metadata.iregistermetadata(VS.90).aspx
[3]: http://social.msdn.microsoft.com/Forums/en/wfprerelease/thread/43424aaf-1e35-4629-9b98-de1fb80079b7
[4]: /2010/09/30/creating-updatable-generic-windows-workflow-activities/