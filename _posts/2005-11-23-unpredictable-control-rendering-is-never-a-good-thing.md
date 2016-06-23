---
title: Unpredictable control rendering is never a good thing
categories: .Net
tags: ASP.Net
date: 2005-11-23 22:55:00 +10:00
---

Probably the things I struggle with most in the IT industry (and usually when looking for a new job) revolve around the questions of:

* How good am I?
* How much do I know?

These questions are really hard for anyone answer although having a degree and/or other certification helps to indicate what you should know.

Following from these questions, I have often thought about whether I would be good enough to work for this company or that company. Take Microsoft for example. Would I be good enough for them? I don't know, but I like to think that they are the benchmark for a programmer's skill as they are a leader in the industry. Every now and then though, I come across some Microsoft code where I can't really understand why they developed something the way they did. This time, I have come across how checkboxes and radio buttons render themselves in ASP.Net 2.0. I would like to know whether there was some kind of valid design reason for the rendering behavior of these controls or whether it is just, in my humble opinion, poor design.

<!--more-->

The inconsistency of these controls is around whether a SPAN tag is rendered around the control. In ASP.Net, almost every control is surrounded by some type of parent tag. Against this parent tag, attributes like id, class, title and styles are typically rendered. The parent tag renders an HTML structure that represents the control as an entity. A client-side script developer can reference that parent tag and understand that it represents a control and any children it contains. A page developer can assign styles and CSS classes to the control based on the HTML structure and expect a consistent style behavior to be implemented.

The radio button control inherits from the checkbox control and the only major difference between them is how they render their INPUT tag. The checkbox and radio button controls render an INPUT tag (as expected) and a LABEL tag if there is a Text property value. The SPAN tag surrounding the INPUT (and optional LABEL) tag will only be rendered if there is a class value, any style values, the control is disabled or there are custom attributes assigned to the control. If none of these cases are encountered, then there is no SPAN tag. What this means is that there may be a parent tag, or there may not be.

This inconsistency will become a problem especially when classes are used in tags above the control in the HTML hierarchy and selectors are used to style the input fields. Take the following ASPX declaration for example:

{% highlight aspx-vb %}
<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Default.aspx.vb" Inherits="_Default" %> 
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"> 

<html xmlns="http://www.w3.org/1999/xhtml" > 
<head runat="server"> 
    <title>Untitled Page</title> 
    <style type="text/css" media="all"> 
  
        .TestClass span input 
        { 
            border: 1px solid black; 
            margin: 15px; 
        } 
  
    </style> 
</head> 
<body> 
    <form id="form1" runat="server"> 
        <div class="TestClass"> 
            <div> 
                <ASP:CHECKBOX id="chkTest1" runat="server" /> 
            </div> 
            <div> 
                <ASP:CHECKBOX id="chkTest2" runat="server" text="Test 2" /> 
            </div> 
            <div> 
                <ASP:CHECKBOX id="chkTest3" runat="server" tooltip="different structure" /> 
            </div> 
            <div> 
                <ASP:CHECKBOX id="chkTest4" runat="server" text="Test 2" tooltip="different structure again" /> 
            </div> 
        </div> 
    </form> 
</body> 
</html> 
{% endhighlight %}

The intention behind this example is that the INPUT tags will be styled with a border and margins based on a CSS class assigned further up the HTML hierarchy. The CSS class and selector is saying that where a tag is found with the CSS class of TestClass, apply the style to any controls like checkboxes and radio buttons. This demonstrates a likely scenario where the developer is under the assumption that a checkbox will always render a SPAN tag as the parent element.

When this is run, the HTML output is this:

 {% highlight aspx-vb %}
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"> 
<html xmlns="http://www.w3.org/1999/xhtml"> 
<head> 
   <title>Untitled Page </title> 
    <style media="all" type="text/css"> 
  
        .TestClass span input 
        { 
            border: 1px solid black; 
            margin: 15px; 
        } 
  
    </style> 
</head> 
<body> 
    <form id="form1" action="Default.aspx" method="post" name="form1"> 
        <div> 
            <input id="__VIEWSTATE" name="__VIEWSTATE" type="hidden" value="/wEPDwUKMTU1MjcxNDA3NWQYAQUeX19Db250cm9sc1JlcXVpcmVQb3N0Qm 
Fja0tleV9fFgQFCGNoa1Rlc3QxBQhjaGtUZXN0MgUIY2hrVGVzdDMFC 
GNoa1Rlc3Q0073yAnmuscc5SVp45f2hz4h5v5k=" /> 
        </div> 
        <div class="TestClass"> 
            <div> 
                <input id="chkTest1" name="chkTest1" type="checkbox" /> 
            </div> 
            <div> 
                <input id="chkTest2" name="chkTest2" type="checkbox" /><label for="chkTest2">Test 2</label> 
            </div> 
            <div> 
                <span title="different structure"> 
                    <input id="chkTest3" name="chkTest3" type="checkbox" /></span> 
            </div> 
            <div> 
                <span title="different structure again"> 
                    <input id="chkTest4" name="chkTest4" type="checkbox" /><label for="chkTest4">Test 2</label></span> 
            </div> 
        </div> 
        <div> 
            <input id="__EVENTVALIDATION" name="__EVENTVALIDATION" type="hidden" value="/wEWBQLW6MyrCwKtweqtDQKtwa6cAwKtwcL3CwKtwaa/BzjQrPLOlUVjvfSfhjgmYNkpkQmM" /> 
        </div> 
    </form> 
</body> 
</html> 
{% endhighlight %}

The first two checkboxes don't apply the style, but the other two do. I am sure that there will be developers every now and then that can't figure out why their selector based styles are not being used for their radio buttons and checkbox controls. If the Microsoft developer of these web controls was consistent and always rendered a SPAN tag as the parent element, this wouldn't be a problem.


