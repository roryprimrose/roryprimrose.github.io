---
title: Coding standards - String vs string etc
categories : .Net
date: 2007-08-03 11:09:00 +10:00
---

 I recently [posted][0] about some coding standards I have been reviewing. There is another standard that has plagued me where my research doesn&#39;t really offer any solid guidance. 

 It is the old argument of: 

* string vs String
* bool vs Boolean
* int vs Int32
* long vs Int64

 The reason I like the String/Boolean option is because the colour coding in the IDE clearly identifies the text as a type rather than a C# keyword. That being said, I still tend to use int and long, but then revert to String and Boolean. One reason that Int32/Int64 is better would be that the meaning of int/long may change with the platform (I think this is right, someone please confirm) whereas Int32 and Int64 have static meanings. 

 Any thoughts? 

[0]: /archive/2007/07/11/coding-standards-member-level-variables.aspx
