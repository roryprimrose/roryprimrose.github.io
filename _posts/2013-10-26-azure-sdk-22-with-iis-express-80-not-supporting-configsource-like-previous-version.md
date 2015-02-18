---
title: Azure SDK 2.2 with IIS Express 8.0 not supporting configSource like previous version
categories : .Net
tags : Azure
date: 2013-10-26 23:04:17 +10:00
---

I have upgraded a decent sized solution to Azure SDK 2.2, VS2013 and most of the latest bits (still MVC4 though). All of a sudden the web role is not serving any content. In fact the web role can’t even start and the Application_Start method never gets invoked. IIS Express 8.0 only renders the following content:

    The page cannot be displayed because an internal server error has occurred.{% endhighlight %}

I’ve burnt a couple of days on this problem so far. The event log is no help and only provides the following:

    The worker process for application pool '60ac083e-fc60-43ec-99f6-07c80ac3f2c6' encountered an error 'Cannot read configuration file
    ' trying to read global module configuration data from file '\\?\C:\Users\[AccountName]\AppData\Local\dftmp\Resources\f8ac7ae6-f28d-4534-8985-5afd77be4cbc\temp\temp\RoleTemp\applicationHost.config', line number '0'.  Worker process startup aborted.{% endhighlight %}

I have proved that a new Cloud solution in VS2013 is able to run. With so many artefacts in the solution it has been impossible to stab in the dark and figure out what is breaking IIS Express 8.0. I have resorted to creating a new Cloud solution as the base template, copying that into my actual solution and slowly adding back in the original artefacts until the system breaks.

It turns out that IIS Express 8.0 fails to process the following configuration in web.config.

    <system.diagnostics configSource=&quot;system.diagnostics.config&quot; /&gt;{% endhighlight %}

The web role will load correctly if the configSource attribute is not applied. This unfortunately means that the entire diagnostics configuration must be in web.config rather than in an external file. This is less than ideal and it is a breaking change from the previous combination of Azure SDK 2.1 and the previous IIS Express (7.5?). This appears to only apply for web roles in a compute emulator. A new MVC5 web project running under IIS Express 8.0 does not hit this issue.

A connect bug has been raised [here][0].

[0]: https://connect.microsoft.com/VisualStudio/feedback/details/806651/azure-sdk-2-2-with-iis-express-8-0-fails-with-web-config-using-configsource
