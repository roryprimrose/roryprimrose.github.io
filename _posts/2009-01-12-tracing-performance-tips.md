---
title: Tracing Performance Tips
categories : .Net
tags : Performance, Tracing
date: 2009-01-12 12:18:00 +10:00
---

 I have recently been working with tracing performance and have posted several tidbits of information. Here is the overview. 

1. [Use TraceSource instead of Trace][0]
1. [Disable global locking][1]
1. [Clear the default listener in configuration][2]
1. [Don&#39;t collect stacktrace information if not required][3]
1. Create TraceSource instances once per name and cache for reuse. I have encountered memory leaks from creating large numbers of instances of the same TraceSource name.
1. [Create a unique TraceSource and TraceListener for each logical part/tier/layer of the application (locking performance and data segregation)][1]
1. [Use thread safe listeners if possible][1]
1. Check TraceSource.Switch.ShouldTrace before calculating any expensive information to provide to the trace message

 On a side note, don&#39;t forget to [turn off code coverage for running load tests][4]. 

[0]: /post/2008/12/22/The-evils-of-SystemDiagnosticsTrace.aspx
[1]: /post/2009/01/08/Disable-Trace-UseGlobalLock-For-Better-Tracing-Performance.aspx
[2]: /post/2009/01/08/Carrying-tracing-weight-you-didnt-know-you-had.aspx
[3]: /post/2009/01/08/Dont-trace-the-callstack-if-you-dont-need-to.aspx
[4]: /post/2008/06/02/load-tests-don-t-like-code-coverage.aspx
