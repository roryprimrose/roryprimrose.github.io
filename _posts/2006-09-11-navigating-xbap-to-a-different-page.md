---
title: Navigating XBAP to a different page
categories : .Net, IT Related
tags : WPF
date: 2006-09-11 13:42:51 +10:00
---

Surprisingly, one of the first major problems I had with WPF was when I tried to figure out how to navigate to a new WPF page inside an XBAP application. Sure, there are a few controls which encapsulate this functionality (like the Frame and Hyperlink controls), but I wanted to fire a navigation from a button click or double click of a listbox item. I was researching the net for a day and a half and coming up empty.

Could it be this difficult? Should it be? The answer to these questions is no, but there just isn't enough information out there because WPF is still and emerging technology that is not widely adopted (it's still only just turned RC1 after all).

One of the bits of information I came across was from the [MSDN Magazine][1]. I found the following code sample a little suspect though:

{% highlight csharp %}
void viewHyperlink_Click(object sender, RoutedEventArgs e)
{
    // View Order
    ViewOrderPage page = new ViewOrderPage(GetSelectedOrder());
    NavigationWindow window =
        (NavigationWindow)this.Parent; // Don’t do this!
    
    window.Navigate(page);
}
{% endhighlight %}

It said &quot;Don't do this!&quot;, but I was desperate and tried it anyway. It didn't work. Thankfully, later on in the article, it gave an example of how it is done in a way that works:

{% highlight csharp %}
void viewHyperlink_Click(object sender, RoutedEventArgs e)
{
    // View Order
    ViewOrderPage page = new ViewOrderPage(GetSelectedOrder());
    NavigationService ns =
        NavigationService.GetNavigationService(this);
    
    ns.Navigate(page);
}
{% endhighlight %}

That's better!

The [MSDN Magazine][1] article I found ([App Fundamentals, Build A Great User Experience With Windows Presentation Foundation][0]) is still a very good article despite the code above.

[0]: http://msdn.microsoft.com/msdnmag/issues/06/10/AppFundamentals/
[1]: https://msdn.microsoft.com/en-us/magazine/
