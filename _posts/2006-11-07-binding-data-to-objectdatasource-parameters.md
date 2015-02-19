---
title: Binding data to ObjectDataSource parameters
categories : .Net, IT Related
tags : ASP.Net
date: 2006-11-07 17:51:45 +10:00
---

Have you ever needed to have nested databound controls using multiple ObjectDataSources, where the nested ObjectDataSource has a select parameter that requires a databound value itself?

You have no doubt tried the following:

{% highlight xml linenos %}
<asp:ObjectDataSource ID="odsCountry" runat="server" SelectMethod="GetCountry" TypeName="CountriesBLL">
    <SelectParameters>
        <asp:Parameter Name="countryCode" DefaultValue='<%# Eval("CountryCode") %>' />
    </SelectParameters>
</asp:ObjectDataSource>
{% endhighlight %}

If you try this, you will get an error that says:

> _Databinding expressions are only supported on objects that have a DataBinding event. System.Web.UI.WebControls.Parameter does not have a DataBinding event._

So you can't bind to your value to the parameter. There is a really simple way around this. Redirect the bound value through a control that does support databinding.

{% highlight xml linenos %}
<asp:TextBox runat="server" ID="txtCountryCode" Visible="false" Text='<%# Eval("CountryCode") %>' />
<asp:ObjectDataSource ID="odsCountry" runat="server" SelectMethod="GetCountry" TypeName="CountriesBLL">
    <SelectParameters>
        <asp:ControlParameter Name="countryCode" ControlID="txtCountryCode" PropertyName="Text" />
    </SelectParameters>
</asp:ObjectDataSource>
{% endhighlight %}

By doing it this way, the containing ObjectDataSource will provide the required value to the TextBox which is not rendered to the browser. The nested ObjectDataSource that requires the databound value can then obtain the value from the TextBox using a ControlParameter. Easy.


