---
title: Proxy Configuration Switching
categories : .Net, Applications
date: 2005-02-03 23:26:00 +10:00
---

Late last year I wanted to move my RSS reader (currently [Sauce Reader][0]) from my work desktop to my laptop. Doing this meant that I was able to keep up-to-date with feeds over the weekend. It did come with a hitch though. My work desktop sits behind a proxy server and I haven't yet come across a feed reader that does true auto-detection of proxy settings like IE does. 

After I moved the feed reader to my laptop, it meant that I had to put the proxy settings either in the feed readers settings or in the IE settings. I know that a lot of other applications use IE's settings so that was the best place as all the other applications would benefit from the settings. I set the feed reader to use the IE proxy settings and entered the proxy server and port number in the IE options dialog. Every time I went to work, I would have to go into the IE settings and enable the proxy setting so that IE and all the other applications would have Internet access. When I went home, I would have to disable the proxy settings again. This very quickly became a major hassle. 

Just before Christmas, I thought that if I could identify the domain that the computer was on, I would be able to automatically determine if I was plugged into work or not and change the proxy settings as appropriate. It turns out that I wasn't able to programmatically determine the current domain (anyone achieved this?), but at the start of January, I had another idea. I could get the IP address of a known computer name, in this case, my work computer.

<!--more-->

It goes like this, when the computer starts, I get it to run my little proxy switch application. It gets a computer name from the command-line parameters and attempts to find its IP address. If the IP address exists, it enables the proxy setting through the registry, otherwise, the setting is disabled. 

There are two problems with this idea. Firstly, when the IP address is determined, that value is cached. This means that if the computer falls off the network, the IP address is still returned for the computer name specified. Secondly, if I don't have the network cable plugged in at work before I boot up, it will disable the proxy settings. As long as I turn the computer off when traveling between home and work, and I always ensure that the network cable is plugged in before boot-up at work, neither of these are an issue.

Here is the code:

{% highlight vb.net %}
#Region " Imports "
     
Imports System.Net
Imports Microsoft.Win32
     
#End Region
     
Module modGlobal
     
    Public Sub Main(ByVal CmdArgs() As String)
     
        Dim objEntry As System.Net.IPHostEntry
        Dim objKey As RegistryKey = Registry.CurrentUser.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\Internet Settings", True)
        Dim bUseProxy As Boolean
     
        Try
     
            ' Check if there is at least 1 command line parameter
            If CmdArgs.Length > 0 Then
     
                ' Attempt to resolve the name to an IP address
                objEntry = System.Net.Dns.Resolve(CmdArgs(0))
     
                ' We haven't failed, the resolve was successful
                bUseProxy = True
     
            End If  ' End checking if there is at least one command line parameter
     
        Catch
     
            ' Couldn't resolve the computer name to an IP address
            bUseProxy = False
     
        Finally
     
            ' Destroy the object
            objEntry = Nothing
     
        End Try
     
        ' Check if we want to use a proxy
        If bUseProxy Then
     
            objKey.SetValue("ProxyEnable", 1)
     
        Else    ' We don't want to use a proxy
     
            objKey.SetValue("ProxyEnable", 0)
     
        End If  ' End checking if we want to use a proxy
     
    End Sub
     
End Module
{% endhighlight %}

[0]: http://www.synop.com/Products/SauceReader/
