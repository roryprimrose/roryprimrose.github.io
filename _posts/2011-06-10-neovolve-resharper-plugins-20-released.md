---
title: Neovolve ReSharper Plugins 2.0 released
categories : .Net, My Software
tags : ReSharper, StyleCop
date: 2011-06-10 14:26:02 +10:00
---

It has been a few years since I have updated my ReSharper plugin project that was originally released [three years ago][0]. The reason for the project was that ReSharper natively converts CLR types (System.Int32) to its alias (int). StyleCop also adds this support in its ReSharper plugin. Unfortunately nothing in the marketplace provides a conversion from an alias type to its CLR type. 

I always prefer the CLR types over the alias type names. This allows me to easily identify what is a C# keyword and what is a type reference.

The original 1.0 (and subsequent 1.1) version provided the ability to switch between C# alias types and their CLR type equivalents using ReSharper’s code cleanup profile support. This version adds code inspection and QuickFix support. It also adds better support for type conversions in xmldoc comments.

**Code Cleanup**

The code cleanup profile settings for this plugin are now found under the Neovolve category. This is the only change to the settings used for code cleanup.![image][1]

**Code Inspection**

ReSharper code inspection allows for plugins to notify you when your code does not match defined inspection rules. This release of the plugin adds code inspection to detect when a value type is written in a way that is not desired according to the your ReSharper settings. 

The code inspection settings for this plugin can be found under the Neovolve category.![image][2]

Code highlights will then notify you when your code does not match the configured rules. For example, the settings in the above screenshot identify that type alias definitions show up as a suggestion to be converted to their CLR types. This can be seen below.![image][3]

**QuickFix**

ReSharper has had quick fix support for several years. This version now supports QuickFix bulbs based on the information identified by the code inspection rules. Giving focus to the suggestion will then allow for a quick fix action using Alt + Enter.![image][4]

This version adds better support for type identification in xmldoc comments. Code inspection and QuickFix support also come into play here as well.![image][5]

You can grab this release from the CodePlex project [here][6].

[0]: /post/2008/07/06/neovolve-resharper-plugins-1-0-released.aspx
[1]: //blogfiles/image_102.png
[2]: //blogfiles/image_103.png
[3]: //blogfiles/image_104.png
[4]: //blogfiles/image_105.png
[5]: //blogfiles/image_106.png
[6]: http://neovolvex.codeplex.com/releases/view/45597
