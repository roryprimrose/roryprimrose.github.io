---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part I - Know Your Heritage
date: 2005-01-27 12:21:00 +10:00
---

The first decision that needs to be made when creating a web custom control is what the control will inherit from. At the minimum, you will inherit from System.Web.UI.Control which will hook the control into the web page rendering. The Control object only exposes ID, DataBindings, EnableViewState and Visible properties. This is great for very simple controls, but controls are usually required to have much better rendering capabilities.

Inheriting from System.Web.UI.WebControls.WebControl will intrinsically provide better rendering support. WebControl itself inherits from Control and exposes a lot of properties that typical web controls will use, such as Background Color, Borders, Font etc etc. Depending on how you allow a WebControl object to render itself, a lot of these properties will be handled for you, usually by adding values to the style attribute of the controls rendered HTML tag.

I almost always inherit from WebControl, but in this example, I will inherit from Control because I want to create a very simple lightweight Label control. By lightweight, I mean that I want it to render a Text value, but no HTML tags. Because there are no tags, there isn't a point allowing the implementer of the control to manipulate properties such as BackColor.

The next thing to understand about web custom controls is the ways in which they can be rendered. You can create child controls as member variables, or as objects that are simply added to the objects Controls collection. Regardless of their scope, they are normally added to the Controls collection by overriding the CreateChildControls method. Controls created in this manner are usually referred to as Composite Controls.

I am not a huge fan of composite controls, so I normally create Rendered Controls which are created by writing HTML data directly to an HTMLTextWriter. There are times when this method can take a lot of code to render the control. Because of this, sometimes I will create instances of controls inside one of the Render methods in order to build a child control hierarchy using OOP, then render the top level control out to the HTMLTextWriter using its RenderControl method. For example, this method is perfect when you want a rendered control, but need to render a TABLE hierarchy as part of its output. I still however consider this a rendered control.

My lightweight Label control looks like this:

{% highlight vb.net %}
# Region " Imports "

 Imports System.ComponentModel

 Imports System.Web.UI

# End  Region

< _
 DefaultProperty("Text"), _
 ToolboxData("<{0}:Label runat=server></{0}:Label>") _
> _
 Public  Class Label

# Region " Declarations "

 Inherits Control

# End  Region

# Region " Sub Procedures "

 Protected  Overrides  Sub Render( ByVal output As System.Web.UI.HtmlTextWriter)

  output.Write([Text])

 End  Sub

# End  Region

# Region " Properties "

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

# End  Region

 End  Class
{% endhighlight %}

This control doesn't render start or end tags and no styles can be defined for it. The style of the rendered control is inherited via CSS from its parents. It is a very simple control that just renders the Text property of the control. Note that the overridden Render method doesn't call MyBase.Render as it is not required in this case. The Rendering of the control is completely handled by this class.