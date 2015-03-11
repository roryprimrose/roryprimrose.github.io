---
title: Getting Unity to construct an instance with the default constructor
categories : .Net
tags : Unity
date: 2010-03-26 16:51:19 +10:00
---

I wasted a lot of time this afternoon trying to get some Unity configuration to work. 

One of the issues I had was that I wanted to inject a concrete type that didn’t have an interface. This turned about to be simple as Unity natively supports it. You just don’t need to define a mapTo attribute on the type element. The issue that hit me was that Unity was not invoking the constructor that I was expecting. My type that has the following constructors:

<!--more-->

{% highlight csharp %}
public class Settings
{
    public Settings()
        : this(ConfigurationStoreFactory.Create())
    {
    }
    
    public Settings(IConfigurationStore store)
    {
    }
}
{% endhighlight %}

In my attempt to wire this up, my Unity configuration contained the following definition:

{% highlight xml %}
<type type="Neovolve.Jabiru.Server.Business.Settings, Neovolve.Jabiru.Server.Business">
</type>
{% endhighlight %}

Unity was not able to create an instance of Settings with this configuration. The reason was that Unity was calling the constructor with an interface definition rather than the default constructor. This is not what I expected given that I was not defining any constructor configuration for the type.

The following configuration must be used in order for Unity to invoke the default constructor.

{% highlight xml %}
<type type="Neovolve.Jabiru.Server.Business.Settings, Neovolve.Jabiru.Server.Business">
    <typeConfig extensionType="Microsoft.Practices.Unity.Configuration.TypeInjectionElement,
                            Microsoft.Practices.Unity.Configuration">
    <constructor>
    </constructor>
    </typeConfig>
</type>
{% endhighlight %}

This is completely counter-intuitive, but that’s life.


