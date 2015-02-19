---
title: Publishing embedded resources in ASP.Net 2.0
categories : .Net
date: 2005-10-11 11:47:00 +10:00
---

I put up [a post a while back][0] about my issues with publishing embedded resources from an assembly with ASP.Net 2.0. Ramon wanted a sample, so here it is.

There are a couple of things to note about getting this to work:

1. The resource must be marked as Build Action = Embedded Resource
1. Line 18 in ResTest\ResTestLib\My Project\AssemblyInfo.vb must include the full namespace of the assembly, not just the filename of the embedded resource
1. As above, Line 21 in ResTest\ResTestLib\ResTestControl.vb must also include the full namespace of the assemlby, not just the filename of the embedded resource

[0]: /2005/08/14/when-a-bug-isn-t-a-bug-but-still-requires-a-workaround/