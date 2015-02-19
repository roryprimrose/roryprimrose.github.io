---
title: Carrying tracing weight you didn't know you had
categories : .Net
tags : Performance, Tracing
date: 2009-01-08 13:14:00 +10:00
---

[Continuing on my performance testing][0] of tracing components, there is another factor I realised that may be impacting the performance I get out of my code. 

When the TraceSource.Listeners property is referenced, the collection is initialised using the application configuration. Regardless of whether there is a TraceSource configured for the provided name or what listeners are defined, there is always a default listener that is added to the configured collection of listeners. This is the [System.Diagnostics.DefaultTraceListener][1]. 

All the tracing methods of this listener implementation call down to an internalWrite method that has the following implementation: 

{% highlight csharp linenos %}
private void internalWrite(string message)
{
    if (Debugger.IsLogging())
    {
        Debugger.Log(0, null, message);
    }
    else if (message == null)
    {
        SafeNativeMethods.OutputDebugString(string.Empty);
    }
    else
    {
        SafeNativeMethods.OutputDebugString(message);
    }
}    
{% endhighlight %}

This is extra baggage that you probably didn't know you had. If you want lean tracing performance and don't need this debug trace support, the best option is to remove this listener in configuration. All you have to do is add a _&lt;clear /&gt;_ element as the first item in the listeners element for each source you have defined in your configuration. 

{% highlight xml linenos %}
<?xml version="1.0" encoding="utf-8" ?> 
<configuration> 
    <system.diagnostics>
    <trace useGlobalLock="false" /> 
    <sources> 
        <source name="MySource" 
                switchValue="All"> 
        <listeners>
    
            <clear />
    
            <add name="ListenerName" 
                type="MyApplication.LoadTests.LoadTestTraceListener, MyApplication.LoadTests" /> 
        </listeners> 
        </source> 
    </sources>    
    </system.diagnostics> 
</configuration>     
{% endhighlight %}

[0]: /2009/01/08/disable-trace-usegloballock-for-better-tracing-performance/
[1]: http://msdn.microsoft.com/en-us/library/system.diagnostics.defaulttracelistener.aspx
