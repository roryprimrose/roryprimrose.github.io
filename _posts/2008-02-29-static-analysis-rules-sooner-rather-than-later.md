---
title: Static Analysis Rules - Sooner rather than later
categories : .Net, Applications
tags : TFS, WCF
date: 2008-02-29 00:16:00 +10:00
---

 I [posted the other day][0] that I wanted to create some static analysis rules for Visual Studio. I have some great ideas for several rules that I want to write in order to do two things. Firstly, I want rules to pick up common mistakes made in coding. Secondly, the rules can be used to enforce coding standards via the Code Analysis TFS checkin policy. 

 To enforce coding standards, I have previously written xpath and regex TFS checkin policy implementations that work off a set of defined rules. I found that the xpath policy worked really well for files like csproj and sln files. The xml in these files is reasonably simple and the xpath queries are able to easily identify data that is missing or data that shouldn&#39;t be included. I found it was a different story with the regex policy though. It became very difficult to be able to satisfy coding standards using regex on code files. It also only allowed for analysis of content within a single file, not what outside code calls into that file or other files that the code calls out to. 

 Enter the static analysis rules. It turned out that these are really easy to write. In about an hour, I have created four static analysis rules and tested them. The rules so far are: 

* ND1001 - Mark DataContract properties with DataMember
* ND1002 - Mark class with DataContract where properties with DataMember exist
* ND1003 - Scope properties marked with a DataMember attribute as public
* ND1004 - Scope classes marked with a DataContract attribute as public

 See [this post][1] and [this post][2] for some pointers about writing your own rules. Given the changes to the Visual Studio 2008 for Static Analysis Rules, some of the information around is not completely correct. I found that using [Reflector] on the files in _C:\Program Files\Microsoft Visual Studio 9.0\Team Tools\Static Analysis Tools\FxCop\Rules_ gave a great insight about how to achieve some things. 

 You can download the code for the above rules and included Release build from [here][3]. If you want to use these rules, just drop the _Neovolve.StaticAnalysis.Rules.dll_ into _C:\Program Files\Microsoft Visual Studio 9.0\Team Tools\Static Analysis Tools\FxCop\Rules_, start up Visual Studio and run Code Analysis on a WCF service contract project. 

 On a side note, static analysis rules are great if you use CodePlex. They will allow you to do some of the things that a checkin policy might do. Given that CodePlex doesn&#39;t support checkin policies, this is a great alternative as static analysis rules are just dll&#39;s on the client machine (no registration or server configuration!). 

[0]: /archive/2008/02/25/writing-your-own-fxcop-rules.aspx
[1]: http://blogs.msdn.com/fxcop/archive/2008/01/18/tutorial-on-writing-your-own-code-analysis-rule.aspx
[2]: http://blogs.msdn.com/fxcop/archive/2006/05/31/faq-can-i-create-custom-rules-that-target-both-visual-studio-and-fxcop-david-kean.aspx
[3]: http://www.codeplex.com/CSAR
