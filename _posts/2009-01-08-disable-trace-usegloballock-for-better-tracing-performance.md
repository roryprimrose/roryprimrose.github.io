---
title: Disable Trace UseGlobalLock For Better Tracing Performance
categories : .Net
tags : Performance, Tracing
date: 2009-01-08 12:58:00 +10:00
---

I have had a performance bottleneck in a load test that I have been running. The load test run against some tracing components which ultimately invoke TraceSource and TraceListener methods. I have been wondering why performance drops through the floor as more and more users come online and the call count increases. I have used Reflector a lot of times to review the implementation of TraceSource and TraceListener to get a feel for what they do. I remembered that global locking may be a problem. 

The methods on TraceSource check TraceInternal.UseGlobalLock (also referenced by Trace.UseGlobalLock) which is determined by the _system.diagnostics/trace/useGlobalLock_ configuration value: 

{% highlight xml %}
<?xml version="1.0" encoding="utf-8" ?> 
<configuration> 
    <system.diagnostics> 
    <trace useGlobalLock="false" /> 
    <sources> 
        <source name="MySource" 
                switchValue="All"> 
            <listeners> 
                <add name="ListenerName" 
                    type="MyApplication.LoadTests.LoadTestTraceListener, MyApplication.LoadTests" /> 
            </listeners> 
        </source> 
    </sources>    
    </system.diagnostics> 
</configuration> 
{% endhighlight %}

TraceSource checks whether a global lock should be used when a trace message is written. 

If global locking is enabled (the default value), then a _lock (TraceInternal.critSec)_ call is made before looping through each listener defined for the source to write the message to the listener. With multiple threads invoking trace methods, each invocation is blocking all the other threads while it is writing its trace message to the collection of trace listeners. This is regardless of where each trace invocation occurs in the application code base. 

If global locking is disabled, TraceSource loops through the trace listeners without a lock. It then checks each listener for whether it is thread safe ([TraceListener.IsThreadSafe][0], false by default). If the listener is thread safe, the message is written directly to the listener. Otherwise, a lock is obtained on the listener itself before writing the message. If the application is configured to a single source and listener, then this will actually have the same affect as a global lock. However, if multiple sources and listeners are used, locking on each listener has the advantage that tracing a message in one part of the application will not lock up other threads that write trace messages in another part of the application using a different listener. 

Ideally you would use trace listeners that are thread safe but most of the ones out of the box are not. [System.Diagnostics.EventSchemaTraceListener][1] and [System.Diagnostics.EventProviderTraceListener][2] are the only two provided with the .Net framework that are thread safe. 

I find that [System.Diagnostics.XmlWriterTraceListener][3] is the best listener for my purposes so this limits the performance that I can get out of tracing. However, I tend to use a unique source and listener per assembly in a solution. While this improves locking performance encountered, this also makes for a good segregation of tracing data that can be merged together if required using [SvcTraceView.exe][4]. 

On a side note, I created a custom listener to use in my load test because I wanted to be able to test the performance of my tracing components while minimizing the performance &quot;noise&quot; of the .Net framework part of the tracing execution. The listener looks something like this: 

{% highlight csharp %}
using System; 
using System.Diagnostics; 
    
namespace MyApplication.LoadTests 
{ 
    /// <summary> 
    /// The <see cref="LoadTestTraceListener"/> 
    /// class is used to help run load tests against the tracing components. 
    /// </summary> 
    public class LoadTestTraceListener : TraceListener 
    { 
        /// <summary> 
        /// Writes the provided message to the listener you create in the derived class. 
        /// </summary> 
        /// <param name="message">A message to write.</param> 
        public override void Write(String message) 
        { 
            Debug.WriteLine(message); 
        } 
    
        /// <summary> 
        /// Writes a message to the listener you create in the derived class, followed by a line terminator. 
        /// </summary> 
        /// <param name="message">A message to write.</param> 
        public override void WriteLine(String message) 
        { 
            Debug.WriteLine(message); 
        }
    
        /// <summary> 
        /// Gets a value indicating whether the trace listener is thread safe. 
        /// </summary> 
        /// <value></value> 
        /// <returns>true if the trace listener is thread safe; otherwise, false. The default is false. 
        /// </returns> 
        public override Boolean IsThreadSafe 
        { 
            get 
            { 
                return true; 
            } 
        } 
    } 
} 
{% endhighlight %}

This listener will output the trace messages in debug mode, but the compiler will not include the debug statements under a release build. It also indicates that the listener is thread safe to improve performance of TraceSource implementations that use this listener when global locking is disabled. 

[0]: http://msdn.microsoft.com/en-us/library/system.diagnostics.tracelistener.isthreadsafe.aspx
[1]: http://msdn.microsoft.com/en-us/library/system.diagnostics.eventschematracelistener.aspx
[2]: http://msdn.microsoft.com/en-us/library/system.diagnostics.eventing.eventprovidertracelistener.aspx
[3]: http://msdn.microsoft.com/en-us/library/system.diagnostics.xmlwritertracelistener.aspx
[4]: http://www.google.com/url?q=http://msdn.microsoft.com/en-us/library/ms732023.aspx&amp;sa=X&amp;oi=revisions_result&amp;resnum=1&amp;ct=result&amp;cd=1&amp;usg=AFQjCNHXG4w7CobT9-Fxn7mw8FNj-Ppwvg
