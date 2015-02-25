---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part II - Using Custom Attributes
date: 2005-01-31 17:29:00 +10:00
---

Custom attributes rendered in HTML tags are quite useless by themselves, however they do become quite powerful when a little script is added. I usually add custom attribute values to a controls rendered HTML tag to store server-side property values that are relevant to the client. This allows script running on the client to use and change those values in order to make a richer control.

In the last article, I created a very simple Label control that inherited from Control. This time I am going to create an Image Button control. It turns out that there is already an ImageButton control that is intrinsic to ASP.Net. The ASP.Net ImageButton control does almost everything I want, so a lot of the work is done for me and I don't have to worry about coding for styles, postback or server event implementation. Because of this, inheriting from System.Web.UI.WebControls.ImageButton instead of Control or WebControl will cut out a lot of work in developing the control.

Building on the ASP.Net ImageButton, there are a few extra features I want to add with my ImageButton control. I want it to be able to raise a client click event and optionally do a postback, handle mouseover and mousedown images and to be able to act as a toggle button as well as a press button. To support these features, there are several properties that will be exposed by the server control. These properties include ClientClickHandler, ImageDownUrl and ImageUpUrl among others. The ClientClickHandler property value is the JavaScript function to call on the client when the control is clicked. This property has no use on the server at either design-time or run-time, but must be stored in the client tag so it can be used by the client code. The ImageDownUrl and ImageUpUrl property values are also required on the client, but their values may also be used for design-time rendering on the server.

 Before I get into how to render the custom attributes, lets have a look at how controls render themselves. A control that inherits from Control only has a Render method, whereas a control that inherits from WebControl, such as the ASP.Net ImageButton, has a little more to play with. WebControl controls expose Render, RenderBeginTag, RenderContents, RenderChildren and RenderEndTag methods. The ordering of calls are that the Render method fires first and it calls RenderBeginTag, RenderContents and then RenderEndTag. The RenderContents method calls RenderChildren.

Custom attributes are rendered to the start tag of the control. This means that for a WebControl inherited control, the work is done in MyBase.RenderBeginTag. We could override this method ourselves to attempt to render the custom attributes along with the rest of the start tag, but that would take away from the power of the WebControls internal code. The WebControl object already exposes some nice properties we can use, such as the Attributes and Style properties. We can add our custom attributes to the Attributes collection property and the rendering will be done for us, along with all the appropriate style definitions and the handling of the id attribute.

As we don't want to override MyBase.RenderBeginTag, a better place to put this code is in the controls PreRender event. The PreRender event is a great place to set up how the control is going to be rendered, including our custom attributes, control specific inline styles and registering any client scripts that are needed by the control (more on this in further articles). This is where I hit a snag with the ASP.Net ImageButton control. There is a bug in the ImageButton control in that is doesn't fire the PreRender event. This isn't a major issue though as the WebControl object also exposes an OnPreRender method. We can get around this bug by overriding that method instead of handling the event.

So far, the control has properties defined like this:

{% highlight vb.net %}
 < _
     Description("The Key value to identify the purpose of the button."), _
     Category("Client") _
 > _
 Public  Property Key() As  String
     Get

         ' Return the stored value
         Return  CType (ViewState.Item("Key"), String )

     End  Get
     Set ( ByVal Value As  String )

         ' Store the new value
         ViewState.Item("Key") = Value

     End  Set
 End  Property

 < _
     Description("The client function to call when the control is clicked."), _
     Category("Client") _
 > _
 Public  Property ClientClickHandler() As  String
     Get

         ' Return the stored value
         Return  CType (ViewState.Item("ClientClickHandler"), String )

     End  Get
     Set ( ByVal Value As  String )

         ' Ensure that no brackets or parameters are defined
         If Value <> vbNullString _
             AndAlso Value.IndexOf("(") > -1 Then Value = Value.Substring(0, Value.IndexOf("("))

         ' Store the new value
         ViewState.Item("ClientClickHandler") = Value

     End  Set
 End  Property

 < _
    Description("The Url to the image to use when the mouse is down on the control or the control is selected."), _
    Category("Appearance") _
 > _
 Public  Property ImageDownUrl() As  String
     Get

     ' Return the stored value
     Return  CType (ViewState.Item("ImageDownUrl"), String )

     End  Get
     Set ( ByVal Value As  String )

     ' Store the new value
     ViewState.Item("ImageDownUrl") = Value

     End  Set
 End  Property

 < _
    Description("The Url to the image to use when the mouse is hover the control."), _
    Category("Appearance") _
 > _
 Public  Property ImageHoverUrl() As  String
     Get

         ' Return the stored value
         Return  CType (ViewState.Item("ImageHoverUrl"), String )

     End  Get
     Set ( ByVal Value As  String )

         ' Store the new value
         ViewState.Item("ImageHoverUrl") = Value

     End  Set
 End  Property

 < _
    Description("Determines whether the button is selected or not."), _
    Category("Appearance"), _
    DefaultValue( False ) _
 > _
 Public  Property Selected() As  Boolean
     Get

         ' Get the stored value
         Dim objValue As  Object = ViewState.Item("Selected")

         ' Check if the type of object is a boolean
         If  TypeOf objValue Is  Boolean  Then

             ' Return the stored value
             Return  CType (objValue, Boolean )

         Else  ' This is not a boolean

             ' Return the default value
             Return  False

         End  If  ' End checking if the type of object is a boolean

     End  Get

     Set ( ByVal Value As  Boolean )

         ' Store the new value
         ViewState.Item("Selected") = Value

     End  Set

 End  Property

 < _
    Description("Determines whether the button is a toggle button or not."), _
    Category("Appearance"), _
    DefaultValue( False ) _
 > _
 Public  Property IsToggle() As  Boolean
     Get

         ' Get the stored value
         Dim objValue As  Object = ViewState.Item("IsToggle")

         ' Check if the type of object is a boolean
         If  TypeOf objValue Is  Boolean  Then

             ' Return the stored value
             Return  CType (objValue, Boolean )

         Else  ' This is not a boolean

             ' Return the default value
             Return  False

         End  If  ' End checking if the type of object is a boolean

     End  Get

     Set ( ByVal Value As  Boolean )

         ' Store the new value
         ViewState.Item("IsToggle") = Value

     End  Set

 End  Property
{% endhighlight %}

 Take note of how the properties handle ViewState data. For string properties, I am doing a direct convert of the ViewState value. If the ViewState doesn't have a value, Nothing will be returned. vbNullString is defined as Nothing and is a string, therefore it will be a safe conversion. The only way this would fall down is if code outside this property or assembly modified that particular ViewState item to some other object type. Only the implementer of the control would do this and basically, if they break it, they pay for it. For properties that have any other data types, I check if the object in the ViewState is of the same type and return the value if it is, otherwise I return a default value.

Ok, now lets render these property values to the control HTML tag as custom attributes. This is all that needs to be done:

{% highlight vb.net %}
 Protected  Overrides  Sub OnPreRender( ByVal e As System.EventArgs)

     With Attributes

         ' Ensure that the id attribute is rendered
         If ID = vbNullString Then .Add("id", ClientID)

         ' Add the attributes if they are specified
         If Key <> vbNullString Then .Add("Key", Key)

         If ImageDownUrl <> vbNullString Then .Add("ImageDownUrl", ImageDownUrl)

         If ImageHoverUrl <> vbNullString Then .Add("ImageHoverUrl", ImageHoverUrl)

         If ClientClickHandler <> vbNullString Then .Add("ClientClickHandler", ClientClickHandler)

         .Add("Selected", Selected.ToString.ToLower)
         .Add("IsToggle", IsToggle.ToString.ToLower)

     End  With  ' End With Attributes

     ' Allow the base class code to run
     Call  MyBase .OnPreRender(e)

 End  Sub
{% endhighlight %}

The first thing to notice about this code is that I am ensuring that an id attribute is rendered. In the xml of the aspx file, no attribute has to be defined for the control. Also a control can be created at run-time and added as a child of another control and not have its id property set. Regardless of whether an id property value has been defined, the Control object (which all ASP.Net controls are derived from) exposes a ClientID value, which has the value of the control as ASP.Net sees it when rendered to the client. The ClientID property value is typically prefixed with all the parent controls id values, delimited by the _ character. The code here checks if there is no id value and if so, adds a custom attribute that happens to be called id and assigns it the value of ClientID. Simply calling Attributes.Add("id", ClientID) will cause a duplicate id attribute to be rendered where the id property did already have a value and we definitely don't want that. I ensure that the rendered tag always as an id value specified so that client script can identify the controls element by its id value.

The next thing to notice is that for string properties, I am only going to render the attribute if it has a value. My client code will handle the case where the attribute isn't defined so I don't want to waste the bandwidth. This may seem trivial, but a couple of months ago, I created a ListView control based on the TABLE tag set. Imagine if I had three empty custom attributes for each of the cells and 1000's of cells are rendered because a stupidly large ListView control hierarchy was created. Little things like this can cut down on a lot of bloat.

As a side note on custom attributes, if you want to add specific inline styles to your control, instead of adding a style custom attribute by Attributes.Add("style", "key: value;"), you should add values by using Style.Add("name", "value"). Also, the data stored in custom attributes are strings. Anything that can't be represented as a string will not work for custom attributes. Keep in mind that the client code also needs to be able to interpret the data for it to be useful, so for example, a serialized object may be a little difficult to manage in JavaScript.

I set up a test example with some dummy property values for the code so far and the rendered output is this:

{% highlight aspx-vb %}
<input type="image" name="ImageButton3" id="ImageButton3" Key="Test" ImageDownUrl="images/down.gif" ImageHoverUrl="images/hover.gif" ClientClickHandler="Image_OnClick" Selected="false" IsToggle="false" src="images/up.gif" alt="" border="0" style="Z-INDEX: 103; LEFT: 395px; POSITION: absolute; TOP: 122px" />
{% endhighlight %}

 We have now been able to take server information that has been defined at either design-time or run-time and render it down to the client. Client code can now take advantage of that information. I have written a JavaScript wrapper function that makes it easy to read custom attributes from an HTML element. HTML elements have two methods called getAttribute and setAttribute and these can be used directly, but I prefer to use a helper function that includes some business rules. For example, I want it to handle the case where no valid element has been specified, or the attribute doesn't exist. I also want to use default values if no value is found in the custom attribute.

My wrapper function looks like this:

{% highlight csharp %}
function AttributeValue(Item, Name, Default)
{
    // Set Default to "" if not provided
    if (AttributeValue.arguments.length < 3) 
    {
        Default = "";
    }
    else
    {
        Default = Default.toString();
    }

    if ((Item == null ) || (Name == null ))
    {
        return Default;
    }
    else  if (Item.getAttribute == null )
    {
        return Default;
    }
    else
    {
        var sValue = Item.getAttribute(Name, false );
        
        if ((sValue != null) && (sValue != ""))
        {
            return sValue.toString();
        }
        else
        {
            return Default;
        }
    }
}
{% endhighlight %}

In the next article, I will touch on Metadata attributes which can be very useful.


