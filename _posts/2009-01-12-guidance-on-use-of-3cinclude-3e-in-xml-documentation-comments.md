---
title: Guidance on use of include in XML documentation comments
categories: .Net, Applications
tags: ReSharper, StyleCop
date: 2009-01-12 13:32:00 +10:00
---

I have [previously posted][0] about using the [include element][1] for my XML documentation in cases where there is duplicated content. After a recent code review, the reviewer commented that the include element made it difficult to read the documentation because large parts of the XML documentation were abstracted out to an XML file. This made me look at ways around this and the affect of the include tag had on the tooling support.

As a quick overview, the include element in XML documentation tells the compiler to go to the specified XML file and run an XPath query (recursively). It pulls in the result of that XPath query and injects the content into the XML documentation being generated for a code element.

<!--more-->

**Advantages**

* Documentation reuse
* Removes noise in a code file if the bulk of the documentation points to an included reference

**Disadvantages**

* Hard to read the documentation when looking at the source
* [Current ReSharper 4.1 doesn't correctly walk the include path references to validate using directives][2]
* [ReSharper 4.1 doesn't walk the include path references for intellisense (summary and param elements and their content)][3]
* Visual Studio doesn't walk the include path references for intellisense (summary and param elements and their content and exception elements - Visual Studio ignores exception element content)
* [Current StyleCop 4.3 doesn't correctly walk the include path references to validate documentation structure and content][4]

**Guidance**

There is a tug of war between the reducing noise advantage and the readability disadvantage so the decision between the two must be made with respect to the code and its readers/consumers.

The disadvantages listed are primarily regarding the tooling support. With the issues stated for Visual Studio and R#, they are only issues for when the interpretation of the XML documentation is done from the source directly (meaning intellisense within the same solution), rather than the compiler generated XML documentation file. The SC issue is a bigger disadvantage especially if you are using the tool for strict validation as part of a continuous integration process.

To satisfy all but the readability disadvantage, I suggest that you avoid using an include to:

1. Define XML documentation structure required by SC (summary, param and returns elements)
1. Define content that affect SC validation (summary, param and returns elements)
1. Define XML documentation structure parsed by Visual Studio intellisense (exception element)
1. Define content that affect Visual Studio and R# intellisense (summary, param and returns elements)

The include element is very useful for moving remarks and code examples out of the code file to reduce noise. When reading the code file, these items are not normally required for the readability of the file and don't affect intellisense.

**Note:** The SC issues should be fixed in the next version. There is no indication about intellisense fixes to Visual Studio or R#. The use of the include element may change as these issues are fixed.

[0]: /2008/03/26/xml-comments-and-the-include-element/
[1]: http://msdn.microsoft.com/en-us/library/9h8dy30z.aspx
[2]: http://www.jetbrains.net/jira/browse/RSRP-62567
[3]: http://www.jetbrains.net/jira/browse/RSRP-90953
[4]: http://code.msdn.microsoft.com/sourceanalysis/WorkItem/View.aspx?WorkItemId=132
