---
title: WPF and the link that just won’t click
categories : .Net
tags : WPF
date: 2011-06-15 20:56:10 +10:00
---

I’ve been playing with WPF over the last month. It has been great to finally work on a program that is well suited to this technology. One of the implementation details that has surprised me is that the Hyperlink control doesn’t do anything when you click the link.

I can only guess what the design reason is behind this. The only thing the control seems to do is fire off a RequestNavigate event. Every implementation of this control in a form then needs to manually handle this event to fire off the navigation uri. This is obviously going to be duplicated effort as each usage of the hyperlink control will execute the same logic to achieve this outcome.

I have put together the following custom control for my project to suit my purposes.

    namespace Neovolve.Switch.Controls
    {
        using System;
        using System.Diagnostics;
        using System.Windows.Documents;
    
        public class ClickableLink : Hyperlink
        {
            protected override void OnClick()
            {
                base.OnClick();
    
                Uri navigateUri = ResolveAddressValue(NavigateUri);
    
                if (navigateUri == null)
                {
                    return;
                }
    
                String address = navigateUri.ToString();
    
                ProcessStartInfo startInfo = new ProcessStartInfo(address);
                
                Process.Start(startInfo);
            }
            
            private static Uri ResolveAddressValue(Uri navigateUri)
            {
                if (navigateUri == null)
                {
                    return null;
                }
    
                // Disallow file urls
                if (navigateUri.IsFile)
                {
                    return null;
                }
    
                if (navigateUri.IsUnc)
                {
                    return null;
                }
    
                String address = navigateUri.ToString();
    
                if (String.IsNullOrWhiteSpace(address))
                {
                    return null;
                }
    
                if (address.Contains(&quot;@&quot;) && address.StartsWith(&quot;mailto:&quot;, StringComparison.OrdinalIgnoreCase) == false)
                {
                    address = &quot;mailto:&quot; + address;
                }
                else if (address.StartsWith(&quot;http://&quot;, StringComparison.OrdinalIgnoreCase) == false &&
                         address.StartsWith(&quot;https://&quot;, StringComparison.OrdinalIgnoreCase) == false)
                {
                    address = &quot;http://&quot; + address;
                }
    
                try
                {
                    return new Uri(address);
                }
                catch (UriFormatException)
                {
                    return null;
                }
            }
        }
    }{% endhighlight %}
