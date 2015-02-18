---
title: Don't trace the callstack if you don't need to
categories : .Net
tags : Performance, Tracing
date: 2009-01-08 15:04:00 +10:00
---

<p>Here is another performance tip with tracing. In configuration, there is the opportunity to define some tracing options. These options determine the actions taken by TraceListener when it writes the footer of the trace record for a given message. One of the options is to output the Callstack. </p>  <p>It takes a bit of work to calculate the callstack. If you don't need that information, then don't configure your listeners to calculate it. </p>  <p>To demonstrate the difference, I created two load tests that each ran a unit test that used its own specific TraceSource. The reason for two separate load tests was to avoid a <a href="/post/2009/01/08/Disable-Trace-UseGlobalLock-For-Better-Tracing-Performance.aspx">locking issue</a> that would impact the results. </p>  <p>Here is the configuration I used: </p>  <div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:e03edb2e-c075-4b9e-835e-8befff110862" class="wlWriterEditableSmartContent">{% highlight xml linenos %}&lt;?xml version="1.0" encoding="utf-8" ?&gt; 
&lt;configuration&gt; 
  &lt;system.diagnostics&gt; 
    &lt;trace useGlobalLock="false" /&gt; 
    &lt;sources&gt; 
      &lt;source name="WithCallstack" 
              switchValue="All"&gt; 
        &lt;listeners&gt; 
          &lt;clear /&gt; 
          &lt;add type="System.Diagnostics.DefaultTraceListener, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" 
              name="SlimListener" 
              traceOutputOptions="Callstack" /&gt; 
        &lt;/listeners&gt; 
      &lt;/source&gt; 
      &lt;source name="Slim" 
              switchValue="All"&gt; 
        &lt;listeners&gt; 
          &lt;clear /&gt; 
          &lt;add type="System.Diagnostics.DefaultTraceListener, System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" 
              name="SlimListener" /&gt; 
        &lt;/listeners&gt; 
      &lt;/source&gt; 
    &lt;/sources&gt; 
  &lt;/system.diagnostics&gt; 
&lt;/configuration&gt; 
{% endhighlight %}</div>

<p>And here are the unit tests: </p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:0c94089a-45eb-4922-8a7c-c5d08f6b5e48" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}using System; 
using System.Diagnostics; 
using Microsoft.VisualStudio.TestTools.UnitTesting; 

namespace TestProject3 
{ 
    /// &lt;summary&gt; 
    /// Summary description for TraceTests 
    /// &lt;/summary&gt; 
    [TestClass] 
    public class TraceTests 
    { 
        private const String Message = "this is the message to be written"; 
        private static readonly TraceSource CallstackSource = new TraceSource("WithCallstack"); 
        private static readonly TraceSource SlimSource = new TraceSource("Slim"); 

        /// &lt;summary&gt; 
        /// Callstacks the test. 
        /// &lt;/summary&gt; 
        [TestMethod] 
        public void CallstackTest() 
        { 
            CallstackSource.TraceInformation(Message); 
        } 

        /// &lt;summary&gt; 
        /// Slims the test. 
        /// &lt;/summary&gt; 
        [TestMethod] 
        public void SlimTest() 
        { 
            SlimSource.TraceInformation(Message); 
        } 

        /// &lt;summary&gt; 
        ///Gets or sets the test context which provides 
        ///information about and functionality for the current test run. 
        ///&lt;/summary&gt; 
        public TestContext TestContext 
        { 
            get; 
            set; 
        } 
    } 
} 
{% endhighlight %}</div>

<p><strong>With Callstack</strong> </p>

<p>The load test that included writing the callstack to the trace record had the following output: </p>

<p><a href="//blogfiles/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_8.png"><img style="border-right-width: 0px; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px" border="0" alt="Callstack included" src="//blogfiles/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_thumb_3.png" width="636" height="484" /></a> </p>

<p>With a total of 92,608 tests executed, the average execution time was 200 milliseconds per test. </p>

<p><strong>Without Callstack</strong> </p>

<p>The load test that didn't write the callstack to the trace record had the following output: </p>

<p><a href="//blogfiles/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_6.png"><img style="border-right-width: 0px; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px" border="0" alt="Without callstack" src="//blogfiles/WindowsLiveWriter/Donttracethecallstackifyoudontneedto_D15F/image_thumb_2.png" width="638" height="484" /></a> </p>

<p>With a total of 86,652 tests executed, the average execution time was 59 milliseconds per test. </p>

<p>Including the callstack in a trace record appears to take around four times longer to write a trace record. </p>
