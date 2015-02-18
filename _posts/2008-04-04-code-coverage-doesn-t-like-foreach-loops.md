---
title: Code coverage doesn't like foreach loops
categories : .Net
tags : Performance
date: 2008-04-04 17:03:00 +10:00
---

I have an interesting scenario that I have just come across in my code. I have a foreach loop that is not getting 100% code coverage in unit tests. Prior to this, I really liked foreach for its ease of use and readability even though there is a minor performance penalty compared to using a for loop.

Here is the situation. I have a flush method that looks like this:

    {% highlight csharp linenos %}
    public void Flush()
    {
        // Loop through each listener
        foreach (TraceListener listener in Source.Listeners)
        {
            // Flush the listener
            listener.Flush();
        }
    }
    {% endhighlight %}

Code coverage for this method says that 2 blocks not covered, 12.5% not covered, 14 blocks covered, 87.5% covered. Code metrics for this method are maintainability index is 80, cyclomatic complexity is 3, class coupling is 5 and lines of code is 2.

The IL for this method is:

.method public hidebysig instance void Flush() cil managed

{

&#160;&#160;&#160; .maxstack 2

&#160;&#160;&#160; .locals init (

&#160;&#160;&#160;&#160;&#160;&#160;&#160; [0] class [System]System.Diagnostics.TraceListener listener,

&#160;&#160;&#160;&#160;&#160;&#160;&#160; [1] class [mscorlib]System.Collections.IEnumerator CS$5$0000,

&#160;&#160;&#160;&#160;&#160;&#160;&#160; [2] bool CS$4$0001,

&#160;&#160;&#160;&#160;&#160;&#160;&#160; [3] class [mscorlib]System.IDisposable CS$0$0002)

&#160;&#160;&#160; L_0000: nop 

&#160;&#160;&#160; L_0001: nop

&#160;&#160;&#160; L_0002: ldarg.0

&#160;&#160;&#160; L_0003: call instance class [System]System.Diagnostics.TraceSource MyNamespace.MyClass::get_Source()

&#160;&#160;&#160; L_0008: callvirt instance class [System]System.Diagnostics.TraceListenerCollection [System]System.Diagnostics.TraceSource::get_Listeners()

&#160;&#160;&#160; L_000d: callvirt instance class [mscorlib]System.Collections.IEnumerator [System]System.Diagnostics.TraceListenerCollection::GetEnumerator()

&#160;&#160;&#160; L_0012: stloc.1 

&#160;&#160;&#160; L_0013: br.s L_002a

&#160;&#160;&#160; L_0015: ldloc.1 

&#160;&#160;&#160; L_0016: callvirt instance object [mscorlib]System.Collections.IEnumerator::get_Current()

&#160;&#160;&#160; L_001b: castclass [System]System.Diagnostics.TraceListener

&#160;&#160;&#160; L_0020: stloc.0 

&#160;&#160;&#160; L_0021: nop 

&#160;&#160;&#160; L_0022: ldloc.0 

&#160;&#160;&#160; L_0023: callvirt instance void [System]System.Diagnostics.TraceListener::Flush()

&#160;&#160;&#160; L_0028: nop 

&#160;&#160;&#160; L_0029: nop 

&#160;&#160;&#160; L_002a: ldloc.1 

&#160;&#160;&#160; L_002b: callvirt instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()

&#160;&#160;&#160; L_0030: stloc.2 

&#160;&#160;&#160; L_0031: ldloc.2 

&#160;&#160;&#160; L_0032: brtrue.s L_0015

&#160;&#160;&#160; L_0034: leave.s L_004d

&#160;&#160;&#160; L_0036: ldloc.1 

&#160;&#160;&#160; L_0037: isinst [mscorlib]System.IDisposable

&#160;&#160;&#160; L_003c: stloc.3 

&#160;&#160;&#160; L_003d: ldloc.3 

&#160;&#160;&#160; L_003e: ldnull 

&#160;&#160;&#160; L_003f: ceq 

&#160;&#160;&#160; L_0041: stloc.2 

&#160;&#160;&#160; L_0042: ldloc.2 

&#160;&#160;&#160; L_0043: brtrue.s L_004c

&#160;&#160;&#160; L_0045: ldloc.3 

&#160;&#160;&#160; L_0046: callvirt instance void [mscorlib]System.IDisposable::Dispose()

&#160;&#160;&#160; L_004b: nop 

&#160;&#160;&#160; L_004c: endfinally 

&#160;&#160;&#160; L_004d: nop 

&#160;&#160;&#160; L_004e: ret 

&#160;&#160;&#160; .try L_0013 to L_0036 finally handler L_0036 to L_004d

}

The UI for code coverage indicates that each line of code is hit. My guess is that there is something to do with the IEnumerator that is called when foreach is compiled.

I changed the code to this:

    {% highlight csharp linenos %}
    public void Flush()
    {
        // Loop through each listener
        for (Int32 index = 0; index < Source.Listeners.Count; index++ )
        {
            TraceListener listener = Source.Listeners[index];
    
            // Flush the listener
            listener.Flush();
        }
    }
    {% endhighlight %}

Code coverage now says that 0 blocks not covered, 0% not covered, 11 blocks covered, 100% covered. Code metrics for this method now say that maintainability index is 75, cyclomatic complexity is2, class coupling is 3 and lines of code is 3.

The IL for this method is now:

.method public hidebysig instance void Flush() cil managed

{

&#160;&#160;&#160; .maxstack 2

&#160;&#160;&#160; .locals init (

&#160;&#160;&#160;&#160;&#160;&#160;&#160; [0] int32 index,

&#160;&#160;&#160;&#160;&#160;&#160;&#160; [1] class [System]System.Diagnostics.TraceListener listener,

&#160;&#160;&#160;&#160;&#160;&#160;&#160; [2] bool CS$4$0000)

&#160;&#160;&#160; L_0000: nop 

&#160;&#160;&#160; L_0001: ldc.i4.0 

&#160;&#160;&#160; L_0002: stloc.0 

&#160;&#160;&#160; L_0003: br.s L_0024

&#160;&#160;&#160; L_0005: nop 

&#160;&#160;&#160; L_0006: ldarg.0 

&#160;&#160;&#160; L_0007: call instance class [System]System.Diagnostics.TraceSource MyNamespace.MyClass::get_Source()

&#160;&#160;&#160; L_000c: callvirt instance class [System]System.Diagnostics.TraceListenerCollection [System]System.Diagnostics.TraceSource::get_Listeners()

&#160;&#160;&#160; L_0011: ldloc.0 

&#160;&#160;&#160; L_0012: callvirt instance class [System]System.Diagnostics.TraceListener [System]System.Diagnostics.TraceListenerCollection::get_Item(int32)

&#160;&#160;&#160; L_0017: stloc.1 

&#160;&#160;&#160; L_0018: ldloc.1 

&#160;&#160;&#160; L_0019: callvirt instance void [System]System.Diagnostics.TraceListener::Flush()

&#160;&#160;&#160; L_001e: nop 

&#160;&#160;&#160; L_001f: nop 

&#160;&#160;&#160; L_0020: ldloc.0 

&#160;&#160;&#160; L_0021: ldc.i4.1 

&#160;&#160;&#160; L_0022: add 

&#160;&#160;&#160; L_0023: stloc.0 

&#160;&#160;&#160; L_0024: ldloc.0 

&#160;&#160;&#160; L_0025: ldarg.0 

&#160;&#160;&#160; L_0026: call instance class [System]System.Diagnostics.TraceSource MyNamespace.MyClass::get_Source()

&#160;&#160;&#160; L_002b: callvirt instance class [System]System.Diagnostics.TraceListenerCollection [System]System.Diagnostics.TraceSource::get_Listeners()

&#160;&#160;&#160; L_0030: callvirt instance int32 [System]System.Diagnostics.TraceListenerCollection::get_Count()

&#160;&#160;&#160; L_0035: clt 

&#160;&#160;&#160; L_0037: stloc.2 

&#160;&#160;&#160; L_0038: ldloc.2 

&#160;&#160;&#160; L_0039: brtrue.s L_0005

&#160;&#160;&#160; L_003b: ret

}

There are several posts around that talk about the performance difference of foreach vs for, but no-one seems to have actually posted metrics to base their stance on. One post that was in interesting read was [How to Write High-Performance C# Code by Jeff Varszegi][0]. As far as performance goes, the collection in this situation is always going to be very small so it is perhaps not that much of an issue.

I think that for loops would be faster after looking at the IL and understanding what foreach does under the covers. I don't think however that the performance difference is significant in itself. However, if foreach causes issues with code coverage, perhaps both these issues combined is enough of a reason to change coding practices.

**Updated:** Reformatted the IL code to avoid PRE tags that don't wrap.

[0]: http://dotnet.sys-con.com/read/46342.htm
