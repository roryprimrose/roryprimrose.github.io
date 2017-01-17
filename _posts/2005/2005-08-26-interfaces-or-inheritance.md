---
title: Interfaces or Inheritance
categories: .Net
date: 2005-08-26 01:12:00 +10:00
---

I have been thinking about this a bit recently. When is it the most appropriate to use an interface or inheritance? There are so many advantages and disadvantages with both. 

Not that I can go to his talk, but I liked a few of [Doug's comments][0] on the matter.

> _Interfaces are better for those places where extensibility is the highest requirement. Inheritance is better for those places where reusability is the highest requirement._

> _You can screw up your software by doing too much of either one, or by neglecting either one._

A couple of weeks ago I was building a queueing system using generics. The main problem I had was that I wanted a little bit of functionality in the queued item, so I wrote a base class for it. That was all well and good, but then I quickly realised that most of my objects already have a base class. As you can't have multiple inheritance, there goes that idea. 

To get around this, I have to change the implementation to use an interface instead. This isn't a bad thing, but to get the same functionality there will probably be a performance hit. This is because the queue would need to constantly check with eached queued item for its progress status rather than the queued item notifying the queue when its progress status changes. 

Other than the performance consideration, I don't like the interface solution so much because the queue will have to assume that the queued item correctly uses the interface. An interface will ensure that an object has the right signatures, but not whether the code in the implemented interface of the object does what is intended.

Any thoughts?

[0]: http://blogs.msdn.com/dougturn/archive/2005/08/24/455816.aspx
