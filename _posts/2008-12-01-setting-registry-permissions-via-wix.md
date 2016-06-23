---
title: Setting registry permissions via WiX
categories: .Net
tags: WiX
date: 2008-12-01 16:33:00 +10:00
---

I [posted previously][0] about creating EventLog sources without administrative rights. Part of this solution requires that account running the application has rights to create subkeys and write values to the EventLog in the registry. WiX is being used as the installation product so the answer is something like this for the registry key:

<!--more-->

{% highlight xml %}
<Permission User="[APP_POOL_USER_NAME]" CreateSubkeys="yes" Write="yes"/> 
{% endhighlight %}

I found that this didn't work and it failed with the message: 

> _ExecSecureObjects:  Error 0x80070534: failed to get sid for_ account 

The answer to this was that I was not defining the domain for the account. By default, I think it attempts to find the user on the local machine. 

Unfortunately it still didn't work. Setting permissions using the alternative element did seem to work successfully.

{% highlight xml %}
<util:PermissionEx Domain="[APP_POOL_USER_DOMAIN]" User="[APP_POOL_USER_NAME]" CreateSubkeys="yes" Write="yes" />
{% endhighlight %}

[0]: /2008/11/12/creating-event-log-sources-without-administrative-rights/
