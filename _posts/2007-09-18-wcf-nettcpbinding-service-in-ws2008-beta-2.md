---
title: WCF netTcpBinding service in WS2008 Beta 2
categories : .Net
tags : WCF
date: 2007-09-18 21:35:00 +10:00
---

<P>Yesterday I got the opportunity to start playing with WCF net.tcp services hosted in WAS on Windows Server 2008 (Beta 2). There were a couple of hiccups, but it was surprisingly painless.</P>
<P>Firstly, I got the service up and running over wsHttpBinding. One thing I noticed about this is that, unlike XP Pro (and I think Vista), IIS7 on WS2008 doesn't need anonymous set against the svc file for the service endpoint when the rest of the site was running under Windows Authentication. Straight away, this service was working using a remote client over http.</P>
<P>Next up was trying netTcpBinding. First job was getting the required features installed. This <A href="http://msdn2.microsoft.com/en-us/library/ms731053.aspx" target=_blank mce_href="http://msdn2.microsoft.com/en-us/library/ms731053.aspx">MSDN article</A> provides the details as:</P>
<BLOCKQUOTE>
<P><EM>To install the WCF non-HTTP Activation Components</EM></P>
<OL>
<LI>
<P><EM>From the <B>Start</B> button, click the <B>Control Panel</B>, click <B>Programs</B>, and then click <B>Programs and Features</B>.</EM></P>
<LI>
<P><EM>From the <B>Tasks</B> menu, click <B>Turn Windows features on or off</B>.</EM></P>
<LI>
<P><EM>Find the Microsoft.NET Framework 3.0 node, select and expand it.</EM></P>
<LI>
<P><EM>Select the <B>WCF Non-Http Activation Components</B> box and save the setting.</EM></P></LI></OL></BLOCKQUOTE>
<P>A few configuration changes were required in the host web.config. Because I wanted to use the same svc endpoint, I had to provide specific addresses in the configuration for each of the bindings because they have different base addresses.</P>
<P>Next I added the net.tcp binding to the site in IIS. This is done through the "Site Bindings" dialog of the site. So what is the format of the "Binding Information" field? I had no idea off the top of my head and unfortunately the help documentation only describes http/https binding configurations. After a quick web search, <A href="http://blogs.msdn.com/drnick/archive/2006/11/29/format-for-configuring-http-and-tcp-activation.aspx" target=_blank mce_href="http://blogs.msdn.com/drnick/archive/2006/11/29/format-for-configuring-http-and-tcp-activation.aspx">Nicholas Allen</A> to the rescue:</P>
<BLOCKQUOTE>
<P><EM>The format for TCP was picked to be as similar as possible to what already existed for HTTP. We have two parts to configure, again separated by colons. There is no equivalent for the list of addresses. The two parts are </EM>
<OL>
<LI><EM>Port number </EM>
<LI><A href="http://blogs.msdn.com/drnick/archive/2006/10/19/how-hostnamecomparisonmode-works.aspx" target=_blank mce_href="http://blogs.msdn.com/drnick/archive/2006/10/19/how-hostnamecomparisonmode-works.aspx"><EM>Host name comparison mode</EM></A><EM> (blank or a wildcard symbol)</EM> </LI></OL></BLOCKQUOTE>
<P>I chose a port number and entered 8081:* as my binding information. Now my site bindings look like this:</P>
<P><A href="/photos/blog_images/picture2567.aspx" target=_blank mce_href="/photos/blog_images/picture2567.aspx"><IMG src="/photos/blog_images/images/2567/original.aspx" border=0 mce_src="/photos/blog_images/images/2567/original.aspx"></A></P>
<P>Running my remote client using the new address and binding didn't work. No connection. Windows Firewall gets me every time. To test if the firewall is the issue, I usually turn off the firewall temporarily and run the client. In this case, the connection was successful. I turned the firewall back on (very important!) and added the TCP port 8081 as an exception in the firewall rules. I also added 8080 as I moved away from the default port 80 for my http binding.</P>
<P>Now I have a connection, but I get an error saying that the protocol net.tcp is not supported. After a quick web search, I found this <A href="http://msdn2.microsoft.com/en-us/library/ms731053.aspx" target=_blank mce_href="http://msdn2.microsoft.com/en-us/library/ms731053.aspx">MSDN article</A> (same as above). It indicates some commands that need to be run. The one that stood out to me is:</P>
<BLOCKQUOTE>
<P><EM>%windir%\system32\inetsrv\appcmd.exe set app "Default Web Site/&lt;WCF Application&gt;" /enabledProtocols:http,net.tcp</EM></P></BLOCKQUOTE>
<P>One of the things I am trying to do is to avoid command executions. So with the hint of "enabledProtocols", I go searching through the IIS admin UI. I stumble across the "Advanced Settings" dialog for the site. I added net.tcp to the enabled protocols.</P>
<P><A href="/photos/blog_images/picture2566.aspx" target=_blank mce_href="/photos/blog_images/picture2566.aspx"><IMG src="/photos/blog_images/images/2566/original.aspx" border=0 mce_src="/photos/blog_images/images/2566/original.aspx"></A></P>
<P>Job done, service up and running over both http and net.tcp.</P>
