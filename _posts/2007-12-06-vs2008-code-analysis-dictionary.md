---
title: VS2008 Code Analysis Dictionary
categories: .Net
date: 2007-12-06 10:28:01 +10:00
---

VS2008 Code Analysis has new features that provide spell checking. This is really great, but sometime you need to provide some additional information as to what valid words are. This is normally due to the spell checker not knowing your company name and product names. Quite understandable and thankfully it is configurable.

If you look at the help for the _Identifiers should be spelled correctly_ rule, it refers to a custom dictionary. Unfortunately, the help documentation is quite unhelpful where it indicates where the _CustomDictionary.xml_ file is stored:

> _Place the dictionary in the installation directory of the tool, the project directory, or in the directory associated with the tool under the user's profile (%USERPROFILE%\Application Data\...)._

<!--more-->

Which tool? I can only assume that it is referring to the Visual Studio IDE? Why is this not specific? The project directory reference is self explanatory, but the users profile directory is as useful as the first reference to 'the tool'.

If you click the _How to: Customize the Code Analysis Dictionary_ link at the bottom of the help page, you get some mildly more helpful help information as it says:

> _To use the custom dictionary with all projects, put the file in the Visual Studio root install folder and restart Visual Studio. For a project-specific custom dictionary, put the file in the project folder._

I say mildly, because what is the Visual Studio root install folder? I would assume that to mean _C:\Program Files\Microsoft Visual Studio 9.0_, but it could also be _C:\Program Files\Microsoft Visual Studio 9.0\Common7\IDE_ which is where _devenv.exe_ lives. 

Turns out that neither are correct. If you run a search over the root install folder for _CustomDictionary.xml_, you will probably find that the file is found in _C:\Program Files\Microsoft Visual Studio 9.0\Team Tools\Static Analysis Tools\FxCop_. Editing the xml file in the FxCop directory does the trick. 

So the tool is FxCop, not Visual Studio, and the references to the Visual Studio root install folder are just wrong.

I think this tool is absolutely fantastic, but the help documentation really lets it down.


