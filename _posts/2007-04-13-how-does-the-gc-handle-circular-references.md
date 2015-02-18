---
title: How does the GC handle circular references?
categories : .Net
date: 2007-04-13 15:35:30 +10:00
---

I am a little curious about how the garbage collector cleans up CLR objects. From what I understand, it will wait until objects are de-referenced and then will go through a couple of generations (as required) to release objects from memory. 

The following is a simple scenario of GC:

> Object A that has a reference to B that has a reference to C. When A goes out of scope, the GC can collect it. When it removes A, B is has a reference count of 0 and can also be removed and then likewise for C as B is removed. 

Side question: does this happen in the same GC cycle or is this over three GC cycles?

Ok, simple enough. But how does the GC handle the following scenario:

> Object A has a reference to B which has a reference to C. C has a reference back to B. 

When A goes out of scope, is B available for the GC as it is still referenced by C? How does the GC handle these circular references?


