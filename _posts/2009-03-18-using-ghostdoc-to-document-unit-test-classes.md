---
title: Using GhostDoc to document unit test classes
categories: .Net, Applications
date: 2009-03-18 11:47:00 +10:00
---

To follow up on my last post about [documenting unit test methods with GhostDoc][0], a similar concept can be applied to documenting unit test classes.

Add a custom rule under the Classes section in the GhostDoc configuration dialog.

I have used the name "_Matches unit test class_" and identified type names that end with "_Tests_". I have entered the summary documentation with the following value:

> _The $(TypeName.ShortNameAsSee)   
> class is used to test the &lt;see cref="$(TypeName.Words.Verbatim.ExceptLast)" /&gt; class.$(End)_

![Edit Rule dialog][1]

This produces much nicer documentation that the default documentation provided by Visual Studio.

[0]: /2009/03/18/using-ghostdoc-to-document-unit-test-methods/
[1]: /files/WindowsLiveWriter/UsingGhostDoctodocumentunittestclasses_A4F9/image_3.png
