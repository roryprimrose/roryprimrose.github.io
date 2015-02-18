---
title: Code generation rules
categories : .Net
date: 2007-05-08 08:58:47 +10:00
---

[Paul Stovell][0] has [posted some great points][1] about the usage of code generation. This is of great interest to me because I am using code generation to produce a base service implementation as I develop an [SOA][2] application. I have a couple of thoughts to add to his list:

> 7. Code generation is not a license to avoid unit testing.

> There is a tendency to not test generated code because the original unit testing of the templates was successful. Once you have generated code, it should now be considered alive in that it will be run in an environment, interact with other systems and will be required to work with data that didn't exist in the code generation templates or their original test scenarios. Anything could cause problems because specific circumstances weren't considered as the template was developed.

> 8. Code generation does not produce a product.

> This point aligns with Paul's 5th and 6th points. Code generation being a tool, shouldn't produce a product. Use code generation to output common base level code that can then be customized and built upon. Code generation output can be the basis of a product, but shouldn't be the entire product. If you have done this, then either you have wasted too much time on your templates, or your product design needs some serious [> CPR][3]> .

On a side note, the code generator I decided to use [SmartCode][4] because it is the easiest to use and understand in the freeware market. It is also open source so I can deal with any issues or restrictions that I come across. I have since also started to contribute my changes to the project.

[0]: http://www.paulstovell.net/blog
[1]: http://www.paulstovell.net/blog/index.php/six-things-to-watch-out-for-in-code-generators/
[2]: http://en.wikipedia.org/wiki/Service-oriented_architecture
[3]: http://en.wikipedia.org/wiki/Cpr
[4]: http://www.codeplex.com/smartcode
