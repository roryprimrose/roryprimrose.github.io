---
title: Object cache keys and equality
categories: .Net, IT Related
tags: Useful Links
date: 2007-05-22 10:32:08 +10:00
---

For some bizarre reason (meaning I really don't know why), I have always built cache key values as strings when the key being represented is a set of values. On Friday, my eyes were opened to the fact that it is much better to create a rich object that represents the key values. The question then come down to equality testing of the reference type to make sure that the cache keys are correctly identified. [Understanding Equality in C#][0] on CodeProject is a concise article that was helpful for getting this right.

[0]: http://www.codeproject.com/dotnet/Equality.asp?df=100&amp;forumid=205269&amp;exp=0&amp;select=1213983
