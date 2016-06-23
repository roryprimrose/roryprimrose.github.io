---
title: WCF netTcpBinding service in WS2008 Beta 2
categories: .Net
tags: WCF
date: 2007-09-18 21:35:00 +10:00
---

Yesterday I got the opportunity to start playing with WCF net.tcp services hosted in WAS on Windows Server 2008 (Beta 2). There were a couple of hiccups, but it was surprisingly painless.

Firstly, I got the service up and running over wsHttpBinding. One thing I noticed about this is that, unlike XP Pro (and I think Vista), IIS7 on WS2008 doesn't need anonymous set against the svc file for the service endpoint when the rest of the site was running under Windows Authentication. Straight away, this service was working using a remote client over http.

Next up was trying netTcpBinding. First job was getting the required features installed. This [MSDN article][0] provides the details as:

<!--more-->

_To install the WCF non-HTTP Activation Components_

1. _From the **Start** button, click the **Control Panel**, click **Programs**, and then click **Programs and Features**._
1. _From the **Tasks** menu, click **Turn Windows features on or off**._
1. _Find the Microsoft.NET Framework 3.0 node, select and expand it._
1. _Select the **WCF Non-Http Activation Components** box and save the setting._

A few configuration changes were required in the host web.config. Because I wanted to use the same svc endpoint, I had to provide specific addresses in the configuration for each of the bindings because they have different base addresses.

Next I added the net.tcp binding to the site in IIS. This is done through the "Site Bindings" dialog of the site. So what is the format of the "Binding Information" field? I had no idea off the top of my head and unfortunately the help documentation only describes http/https binding configurations. After a quick web search, [Nicholas Allen][1] to the rescue:

> _The format for TCP was picked to be as similar as possible to what already existed for HTTP. We have two parts to configure, again separated by colons. There is no equivalent for the list of addresses. The two parts are_

> 1. _Port number_
> 1. _[Host name comparison mode][2] (blank or a wildcard symbol)_

I chose a port number and entered 8081:* as my binding information.

Running my remote client using the new address and binding didn't work. No connection. Windows Firewall gets me every time. To test if the firewall is the issue, I usually turn off the firewall temporarily and run the client. In this case, the connection was successful. I turned the firewall back on (very important!) and added the TCP port 8081 as an exception in the firewall rules. I also added 8080 as I moved away from the default port 80 for my http binding.

Now I have a connection, but I get an error saying that the protocol net.tcp is not supported. After a quick web search, I found this [MSDN article][3] (same as above). It indicates some commands that need to be run. The one that stood out to me is:

> _%windir%\system32\inetsrv\appcmd.exe set app "Default Web Site/&lt;WCF Application&gt;" /enabledProtocols:http,net.tcp_

One of the things I am trying to do is to avoid command executions. So with the hint of "enabledProtocols", I go searching through the IIS admin UI. I stumble across the "Advanced Settings" dialog for the site. I added net.tcp to the enabled protocols.

Job done, service up and running over both http and net.tcp.

[0]: http://msdn2.microsoft.com/en-us/library/ms731053.aspx
[1]: http://blogs.msdn.com/drnick/archive/2006/11/29/format-for-configuring-http-and-tcp-activation.aspx
[2]: http://blogs.msdn.com/drnick/archive/2006/10/19/how-hostnamecomparisonmode-works.aspx
[3]: http://msdn2.microsoft.com/en-us/library/ms731053.aspx