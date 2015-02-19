---
title: Don't trace the callstack if you don't need to
categories : .Net
tags : Performance, Tracing
date: 2009-01-08 15:04:00 +10:00
---

Here is another performance tip with tracing. In configuration, there is the opportunity to define some tracing options. These options determine the actions taken by TraceListener when it writes the footer of the trace record for a given message. One of the options is to output the Callstack.   

It takes a bit of work to calculate the callstack. If you don't need that information, then don't configure your listeners to calculate it.   

To demonstrate the difference, I created two load tests that each ran a unit test that used its own specific TraceSource. The reason for two separate load tests was to avoid a [locking issue][0] that would impact the results.   

Here is the configuration I used:

{% highlight xml linenos %}
<?xml version="1.0" encoding="utf-8" ?> 
<configuration> 
  <system.diagnostics> 
    <trace useGlobalLock="false" /> 
    <sources> 
      <source name="WithCallstack" 
              switchValue="All"> 
        <listeners> 
          <clear /> 
          <add type="System.Diagnostics.DefaultTraceListener, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" 
              name="SlimListener" 
              traceOutputOptions="Callstack" /> 
        </listeners> 
      </source> 
      <source name="Slim" 
              switchValue="All"> 
        <listeners> 
          <clear /> 
          <add type="System.Diagnostics.DefaultTraceListener, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" 
              name="SlimListener" /> 
        </listeners> 
      </source> 
    </sources> 
  </system.diagnostics> 
</configuration> 

{% endhighlight %}

And here are the unit tests: 

{% highlight csharp linenos %}
using System; 
using System.Diagnostics; 
using Microsoft.VisualStudio.TestTools.UnitTesting; 

namespace TestProject3 
{ 
    /// <summary> 
    /// Summary description for TraceTests 
    /// </summary> 
    [TestClass] 
    public class TraceTests 
    { 
        private const String Message = "this is the message to be written"; 
        private static readonly TraceSource CallstackSource = new TraceSource("WithCallstack"); 
        private static readonly TraceSource SlimSource = new TraceSource("Slim"); 

        /// <summary> 
        /// Callstacks the test. 
        /// </summary> 
        [TestMethod] 
        public void CallstackTest() 
        { 
            CallstackSource.TraceInformation(Message); 
        } 

        /// <summary> 
        /// Slims the test. 
        /// </summary> 
        [TestMethod] 
        public void SlimTest() 
        { 
            SlimSource.TraceInformation(Message); 
        } 

        /// <summary> 
        ///Gets or sets the test context which provides 
        ///information about and functionality for the current test run. 
        ///</summary> 
        public TestContext TestContext 
        { 
            get; 
            set; 
        } 
    } 
} 

{% endhighlight %}

**With Callstack** 

The load test that included writing the callstack to the trace record had the following output: 

[![Callstack included][2]][1]

With a total of 92,608 tests executed, the average execution time was 200 milliseconds per test. 

**Without Callstack** 

The load test that didn't write the callstack to the trace record had the following output: 

[![Without callstack][4]][3]

With a total of 86,652 tests executed, the average execution time was 59 milliseconds per test. 

Including the callstack in a trace record appears to take around four times longer to write a trace record. 

[0]: /2009/01/08/Disable-Trace-UseGlobalLock-For-Better-Tracing-Performance/
[1]: /files/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_8.png
[2]: /files/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_thumb_3.png
[3]: /files/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_6.png
[4]: /files/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_thumb_2.png
