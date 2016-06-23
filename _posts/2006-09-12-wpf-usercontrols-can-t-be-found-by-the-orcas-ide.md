---
title: WPF UserControls can't be found by the Orcas IDE
categories: .Net, Applications, IT Related
tags: WPF
date: 2006-09-12 11:09:35 +10:00
---

I have created some WPF UserControls, but referencing them in XAML is causing grief in the VS IDE. If I have a UserControl that is in the applications assembly, I set up the namespace mapping with something like the following:

<!--more-->

{% highlight xml %}
<Page x:Class="XAMLBrowserApplication1.Page1"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Page1"
        xmlns:stuff="clr-namespace:XAMLBrowserApplication1.Controls">
    <StackPanel>
        <TextBlock Text="my test" />
        <stuff:UserControl1 />
    </StackPanel>
</Page>
{% endhighlight %}

The IDE throws a wobbly saying _Assembly '' was not found. The 'clr-namespace' URI refers to an assembly that is not referenced by the project_. I haven't specified an assembly because the UserControl is in the same assembly as the XAML. This causes the control &lt;stuff:UserControl1 /&gt; to not be resolved which in turn makes the page invalid. Because the page is invalid, it can't be set as the StartupUri of the application. The application still runs, but I loose designer support.

Just to check, I created a control library project and created a UserControl in the external assembly. I added the control library as a reference to the application and updated the XAML to be the following:

{% highlight xml %}
<Page x:Class="XAMLBrowserApplication1.Page1"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Page1"
        xmlns:stuff="clr-namespace:CustomControlLibrary1;assembly=CustomControlLibrary1">
    <StackPanel>
        <TextBlock Text="my test" />
        <stuff:UserControl1 />
    </StackPanel>
</Page>    
{% endhighlight %}

Now the IDE throws a wobbly saying _Assembly 'CustomControlLibrary1' was not found. The 'clr-namespace' URI refers to an assembly that is not referenced by the project_.

Either this is a bug, or I am a complete nugget. I haven't been able to figure this one out for a few days. Does anyone have any answers for this?