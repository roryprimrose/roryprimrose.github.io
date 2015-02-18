---
title: Using GhostDoc to document unit test classes
categories : .Net, Applications
date: 2009-03-18 11:47:00 +10:00
---

To follow up on my last post about [documenting unit test methods with GhostDoc][0], a similar concept can be applied to documenting unit test classes.

Add a custom rule under the Classes section in the GhostDoc configuration dialog.

I have used the name &quot;_Matches unit test class_&quot; and identified type names that end with &quot;_Tests_&quot;. I have entered the summary documentation with the following value:

_> The $(TypeName.ShortNameAsSee)   
> class is used to test the <see cref=&quot;$(TypeName.Words.Verbatim.ExceptLast)&quot; /&gt; class.$(End)_

![Edit Rule dialog][1]

This produces much nicer documentation that the default documentation provided by Visual Studio.

[0]: /post/2009/03/18/Using-GhostDoc-to-document-unit-test-methods.aspx
[1]: //blogfiles/WindowsLiveWriter/UsingGhostDoctodocumentunittestclasses_A4F9/image_3.png
