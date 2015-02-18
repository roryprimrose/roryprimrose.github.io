---
title: ConfigurationManager.OpenExeConfiguration fails in IIS
categories : .Net
tags : WCF
date: 2009-02-19 16:10:50 +10:00
---

Opening an application configuration is useful when you need access to information that is not available via an API. I have done this a few times for reading WCF configuration. 

The following code is how I have previously achieved this.

{% highlight csharp linenos %}
Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
ServiceModelSectionGroup serviceModel = ServiceModelSectionGroup.GetSectionGroup(appConfig);
{% endhighlight %}

This works fine while the application is an exe. Unfortunately it fails when the code is hosted in IIS. You will get an error in the event log like the following:

> A Webhost unhandled exception occurred. 
            
> Exception: System.ServiceModel.Diagnostics.CallbackException: A user callback threw an exception.  Check the exception stack and inner exception to determine the callback that failed. ---> System.ArgumentException: exePath must be specified when not running inside a stand alone exe. 
    
            
>    at System.Configuration.ConfigurationManager.OpenExeConfigurationImpl(ConfigurationFileMap fileMap, Boolean isMachine, ConfigurationUserLevel userLevel, String exePath) 
    
            
>    at System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel userLevel)

The issue here is that IIS doesn't have an exe path for the configuration manager to load from. The simple workaround for this is to access the required configuration section (rather than configuration group) directly using the ConfigurationManager and a wicked cool syntax. The previous example can now be rewritten like this:

{% highlight csharp linenos %}
        ClientSection client = ConfigurationManager.GetSection("system.serviceModel/client") as ClientSection;
{% endhighlight %}

In this case I was after the client configuration for WCF services.


