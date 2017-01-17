---
title: Full support for custom types in TFS Build 2010 build definition editor
categories: .Net
tags: TeamBuild, TFS, WiX
date: 2010-06-24 13:01:00 +10:00
---

I have been putting together a customised build process with TFS Build 2010. Several parts of the functionality added to the build process involve custom types that are used as arguments for the build definition. Deploy and configuration update information are some examples of this. I wanted to get the same designer experience out of the Process tab property grid that is available for properties like the test list definition.

![][0]

The _Automated Tests_ property seen here has a set of sub-items for each test item specified (items in the collection property). Each of these is expandable and the display text of each parent property is customised according to the values of the child properties.

<!--more-->

I used the TestSpecList and TestSpec classes via Reflector as a template to determine what I needed to implement to get this support. It involved a combination of expandable object converters and type descriptors. It is not rocket science and happens to be what I was playing with about six years ago.

Unfortunately I just couldn’t get the same result for my custom types. My custom collection types for the build definition (_Configuration Updates_ and _Wix deployment list_seen below) simply got displayed as _(Collection)_.

![][1]

The best I could do was to change the workflow argument from CollectionOfT to T[]. This would then display an expandable set of the items in the array. Each of these could then display their own display text by overriding ToString of the type in the array.

The disadvantage with this is that the property grid does not display multiple levels of expandable properties. This means you can’t edit the properties of an item in a list without going into the collection editor for the top level property. It also means that you don’t get direct access to the editor assigned to a child property.

I browsed the source for this property grid usage in the Process tab with a bit of Reflector surfing. The process tab property grid is assigned a Microsoft.TeamFoundation.Build.Controls.TeamBuildWorkflowDescriptor instance which is populated with properties, property values and property metadata. This is created by the Microsoft.TeamFoundation.Build.Controls.ControlProcessParameterPropertyGrid control which prepares all the property data from the build definition. This uses XAML serialization to figure out the property information for the build definition.

I was [guessing][2] that the reason my expandable object converters and type descriptors were not being picked up was because the property grid was being provided a dynamic type that had been interpreted from XAML serialization. This would mean that the type being displayed in the grid did not have the type converter or type descriptor definitions for the custom types and was only displaying a guess of the type in the grid.

It then occurred to me that the way to make this type available to the code that populates the property grid would be to place my custom assembly in the same location. I copied my assembly into the PrivateAssemblies directory under the Visual Studio installation and viola, I now have expandable collection items.

![][3]

The _Configuration Updates_ and _Wix deployment list_ properties are now expandable to display the children which are also expandable. The WixProjectFile item selected in the screenshot has a custom editor associated with it and this can be used directly from the Process tab without having to go through the collection editor of the _Wix deployment list_ property. Similarly, all the property values displayed are editable here and the description of the selected property appears at the bottom of the property grid.

Using expandable object converters and type descriptors makes the property grid much more powerful and easy to use. The simple act of placing the assembly in PrivateAssemblies is the way to open up all these possibilities.

Kudos to [Jim Lamb][4] and [Ewald Hofman][5] for their excellent posts on TFS Build 2010 customisation.

[0]: /files/image_19.png
[1]: /files/image_20.png
[2]: http://social.msdn.microsoft.com/Forums/en-US/tfsbuild/thread/624bbb4b-9996-4945-bb82-56e72a718b9f
[3]: /files/image_21.png
[4]: http://blogs.msdn.com/b/jimlamb/archive/2010/02/12/how-to-create-a-custom-workflow-activity-for-tfs-build-2010.aspx
[5]: http://www.ewaldhofman.nl/?tag=/build+2010+customization
