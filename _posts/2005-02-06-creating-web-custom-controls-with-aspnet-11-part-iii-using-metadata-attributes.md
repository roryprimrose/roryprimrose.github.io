---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part III - Using Metadata Attributes
date: 2005-02-06 19:03:00 +10:00
---

 Metadata attributes are attributes that are assigned to classes, variables, structures, enums, procedures, functions and properties (and no doubt others that I haven't come across). They are usually used to inform the IDE how to handle the objects that the metadata attributes are assigned to. They can also be used by run-time code to handle additional information.

Metadata attributes are defined on items with an &lt; &gt; combination before the declaration of the Item. For example, defining Category and Description attributes to a Text property would look like this:

<!--more-->

{% highlight vb.net %}
 < _
 Category("Appearance"), _
 Description("The value rendered for the control.") _
 > _
 Property [Text]() As  String
 Get
 ' Return the stored value
 Return  CType (ViewState.Item("Text"), String )
 End  Get
 Set ( ByVal Value As  String )
 ' Store the new value
 ViewState.Item("Text") = Value
 End  Set
 End  Property
{% endhighlight %}

I am going to look at a few of the common metadata attributes used in creating web custom controls. Some are self explanatory, while others have some very funny behaviors.

This is definitely not a complete list of metadata attributes that you will come across when creating web custom controls. It is also not an exhaustive description of the functionality of the attributes. Many of them, such as Designers and ControlBuilders, do a lot more than what I describe. I am only covering the most common attributes that I use, and a description of my normal usage of them.

**DefaultValue Attribute**  
 To begin with, lets look at the DefaultValue metadata attribute which is used for property definitions. As the name suggests, this metadata attribute defines the default value of the property. 

This attribute doesn't actually affect anything returned from the property, but it does two things that affect a controls behavior in the IDE. Firstly, this attribute lets the design-time property grid of the IDE know what the default value is. You will notice that property values in the property grid are bold where they are not the default value. The second thing it does is determine how the xml of the aspx page is to be built. If the property value is the specified default value, the data for the property is not written to the xml of the control at design-time.

I have noticed a few interesting things about the DefaultValue metadata attribute. String properties don't need to specify DefaultValue if the default value is an empty string. This seems to be the default value of DefaultValue (and no I am not trying to be funny). If a non-empty string is the value set for DefaultValue, an empty property value will be persisted in the page xml as a space. 

 Boolean properties also have interesting behavior. If an empty string is specified in the xml data for a Boolean property (such as &lt;CC1:ImageButton Selected=""&gt;) then the value stored in ViewState is a Boolean with the value of True. If the property attribute isn't defined at all in the xml, then the ViewState value is Nothing, in which case my code will return the default value, usually being False. This is different to normal Boolean behavior. For example, if you run code like this:

{% highlight vb.net %}
 Dim bTest As  Boolean = System.Boolean.Parse(vbNullString)
{% endhighlight %}

or this:

{% highlight vb.net %}
 Dim bTest As  Boolean = System.Boolean.Parse("")
{% endhighlight %}

then an exception will be raised. Our good friends at Microsoft are obviously doing a little extra behind the scenes to capture this issue. They must be identifying that the xml attribute is defined and converting an empty string to "True" when building the ViewStates internal value.

Enum properties also have a bit of an interesting behavior. You need to specify the type of object as well as the string representation of the value. If you don't specify the type, it won't see the string value defined in DefaultValue as the same as the Enum typed value, therefore the property grid will display it as bold and it will still be persisted in the xml. If the attribute isn't defined in the xml, like the Boolean case, the ViewState value is Nothing. If an empty string is defined as the attribute value for the property, an error will occur because an empty string can't be converted to Enum.

**Category Attribute**  
 The Category attribute value is assigned to properties definitions. It will determine which Category the property is put into when the controls properties are viewed by groups in the IDE's property grid. By default, if this attribute isn't defined, Misc will be used.

**Description Attribute**  
 The Description attribute is another useful one. It simply allows you to define a description for an item, such as a class or a property. This is especially useful for properties because the Description attribute value will be displayed in the IDE's property grid. This is a good opportunity to inform the implementer of your control what the property purpose is.

The Description attribute is also useful for people in the VB.Net world who use VBCommenter. Where the Description attribute has a value, and a new VBCommenter block is created, it will take the Description attribute value and put it in the summary tag for you.

**ToolboxData Attribute**  
 The ToolboxData attribute is assigned to a class and is where you define how the IDE will write the aspx xml data for the control when it is added from the toolbox. Where assemblies and their Namespaces are registered with an aspx page, one of the property values of the registration tag is the TagPrefix value that is associated with the assembly. The TagPrefix default value is cc1. This value should be picked up as a variable along with the rest of the toolbox data you specify in this attribute. 

My ToolboxData attribute values normally follow this format:

{% highlight aspx-vb %}
ToolboxData("<{0}:ImageButton runat=server></{0}:ImageButton>")
{% endhighlight %}

The {0} part of the value is where the TagPrefix value from the page registration is used. To not include this would be very dangerous. For the other part of the tag name, I have never had a reason to use a different value other than the class name of my control. The TagPrefix value ensures that there are no naming collisions. For example, I am creating this ImageButton control, but there is already the intrinsic ASP.Net ImageButton control. The ASP.Net control is declared as ASP:ImageButton and with a TagPrefix value of cc1 my ImageButton control will be declared as cc1:ImageButton. By default I also need the runat= server declaration put in.

**Designer Attribute**  
 The Designer attribute is assigned to a class and allows you to change the HTML data that is rendered for the control at design-time. You may want to render the control in a different way at design-time because of the value of the controls properties or because information isn't available at design-time as it would be at run-time.

The Repeater control is a good example of this. When a Repeater and its ItemTemplate is declared in a page, the rendered design-time view displays five items databound to it. No data is actually bound to it, so a Designer is used to bind some example data to the control.

 Designers are not always required however. My lightweight label control in my first article didn't need a Designer because the default rendering of the control was appropriate. When my label control doesn't have a value, nothing is rendered by the control itself as it doesn't render any tags. In this case, the IDE by default will render the equivalent of "[" & control.id & "]" which is fine as far as my label control is concerned.

I will provide an example of Designer support in a later article.

**ControlBuilder Attribute**  
 The ControlBuilder attribute is assigned to a class and is used to help the IDE understand how to interpret xml in an aspx file into control object types. For example, I may have a toolbar control that can hold one or more of my ImageButton controls. When the xml of the aspx file is read, the IDE doesn't establish a relationship of child control definitions and the actual class type of the child control. If my toolbar is defined as this:

{% highlight aspx-vb %}
 <CC1:Toolbar id="tbrTest" runat="server"> 

 <CC1:ImageButton id="btnTest1" runat="server">

 <CC1:ImageButton id="btnTest2" runat="server">

 <CC1:ImageButton id="btnTest3" runat="server">

</CC1:Toolbar>
{% endhighlight %}

 then the toolbar control doesn't know that the child controls are of the type ImageButton. A ControlBuilder will inform the Toolbar control that a tag that contains the name ImageButton is the ImageButton class. Now that the ControlBuilder is informing the Toolbar control what the correct type of child controls are being created for it, it can take any appropriate action.

I usually use ControlBuilders because I only want certain types of controls to be allowed as child controls. As the ControlBuilder will tell my control what the type of child control is, if it doesn't like it, it can remove it from its Controls collection. If my control is only told about literal controls, it won't know whether to allow the child control or not.

I will provide an example of ControlBuilder support in a later article.

**PersistenceMode Attribute**  
 The PersistenceMode attribute is assigned to a property and marks how the property value is to be persisted in its parent controls declaration in the xml of the aspx file. Lets take the example of a generic control that exposes a Text property.

A value of Attribute will result in:

{% highlight aspx-vb %}
 <CC1:MyControl id="tbrTest" runat="server" text="my&value"> 

</CC1:MyControl>
{% endhighlight %}

A value of InnerProperty will result in:

{% highlight aspx-vb %}
 <CC1:MyControl id="tbrTest" runat="server"> 

 <Text>my&value</Text> 

</CC1:MyControl>
{% endhighlight %}

A value of InnerDefaultProperty will result in:

{% highlight aspx-vb %}
 <CC1:MyControl id="tbrTest" runat="server">my&value</CC1:MyControl> 
{% endhighlight %}

A value of EncodedInnerDefaultProperty will result in:

{% highlight aspx-vb %}
 <CC1:MyControl id="tbrTest" runat= "server">my&amp;value</CC1:MyControl> 
{% endhighlight %}

In my control development, I have always put my property values as Attribute (which is the default) as I normally support child controls which would be the nested tag definitions.

**ParseChildren Attribute**  
 The ParseChildren attribute is assigned to a class and will define whether child xml nodes in the aspx file are parsed as child controls or are interpreted as property values. If I had a property that was defined with a PersistenceMode of InnerProperty, then ParseChildren attribute would have to be False. Following the example above, if ParseChildren wasn't False, a control of the type Text would attempt to be created and this would fail. 

I always declare ParseChildren as True as I want nested tags of the xml data to be parsed as child controls. This means that the properties of my controls must be persisted as attribute values in the xml of the aspx page. The default value of ParseChildren is False.

**PersistChildren Attribute**  
 The PersistChildren attribute is a very funny one. I have come across many problems getting child controls to be persisted in the xml of the apx page, especially when the top control is changed through the IDE's property grid. I can't give a definitive answer on how this one should be declared as I have needed to do different things for different controls. I will however cover an example of this attribute in a future article.

**ToolboxItem Attribute**  
 The ToolboxItem attribute is assigned to a class. It is a True/False value that determines whether the control can be added to the Toolbox of the IDE. I declare this as False for child controls that I don't want to be used as top level controls. For example, if I was creating a Table control, I would allow the Table class to be hosted in the toolbox, but not the cell control. This attribute value is True by default.


