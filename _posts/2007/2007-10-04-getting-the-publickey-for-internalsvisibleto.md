---
title: Getting the PublicKey for InternalsVisibleTo
categories: .Net, Applications
tags: Great Tools
date: 2007-10-04 16:01:02 +10:00
---

Using the [InternalsVisibleTo attribute][0] is great for allowing unit tests in an external assembly to directly call types and methods marked as internal. The problem for strong named assemblies is that you need to specify the public key of the unit test assembly in the attribute declaration. I found that [David Kean][1] produced [a little utility][2] app to make life easy. Thank you very much!

[0]: http://msdn2.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.aspx
[1]: http://davidkean.net/
[2]: http://davidkean.net/archive/2005/10/06/1183.aspx
