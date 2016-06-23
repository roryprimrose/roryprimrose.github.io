---
title: Recommended reading for developers
categories: .Net, Applications, IT Related, Software Design
tags: AOP, Dependency Injection, Performance, PostSharp, ReSharper, StyleCop, Unit Testing, Unity, Useful Links
date: 2010-02-17 12:27:32 +10:00
---

I was reminded this morning of an email that I sent to the junior developers on my team when they joined us. It is an overview of some of the development practices, patterns and products that they would get exposed to on our project. I have included it here as a reference collection for others.

**Principles and Patterns**

These are things that I often use and are still learning to use.

<!--more-->

* SOLID – Originally put together by Bob Martin ([http://butunclebob.com/ArticleS.UncleBob.PrinciplesOfOod][0]). A good overview to read is at [http://www.davesquared.net/2009/01/introduction-to-solid-principles-of-oo.html][1].
* LoD - Law of Demeter (or Principle of Least Knowledge) ([http://en.wikipedia.org/wiki/Law_of_Demeter)][2]. Often referred to as loose coupling.
* DRY – Do not Repeat Yourself ([http://en.wikipedia.org/wiki/Don't_repeat_yourself][3]). Many say that this principle should not apply to unit testing.
* DIP – Dependency Injection Pattern which is the D in SOLID. Jeremy Miller has a good overview at [http://codebetter.com/blogs/jeremy.miller/archive/2005/10/06/132825.aspx][4]. DI frameworks (Unity, Castle Windsor, Spring.Net etc) often use a Service Locator pattern for their implementation. Like Jeremy, my preference is constructor injection because it defines a contract of how to use a class whereas setter (or property) injection doesn’t. Setter injection should only be used for optional dependencies and usually requires additional defensive coding.
* TDD – Test Driven Development ([http://en.wikipedia.org/wiki/Test-driven_development][5])
* DDD – Domain Driven Design is gaining popularity but still seems to be not well understood or widely used (including by me). See [http://en.wikipedia.org/wiki/Domain-driven_design][6] and MSDN Mag introduction article at [http://msdn.microsoft.com/en-us/magazine/dd419654.aspx][7].
* AOP – Aspect Orientated Programming ([http://en.wikipedia.org/wiki/Aspect-oriented_programming][8])
* Service Locator – See [http://en.wikipedia.org/wiki/Service_locator_pattern][9] and [http://msdn.microsoft.com/en-us/library/cc707905.aspx][10]. Keep in mind that this is starting to be considered as an anti-pattern and should probably be avoided in favour of DI.
* GOF - Gang of Four patterns ([http://www.dofactory.com/Patterns/Patterns.aspx][11]). I tend to use the following in GoF: 
  * Factory Method- See [http://www.dofactory.com/Patterns/PatternFactory.aspx][12]
  * Facade - See [http://www.dofactory.com/Patterns/PatternFacade.aspx][13]
  * Proxy – See [http://www.dofactory.com/Patterns/PatternProxy.aspx][14]
  * Chain of Responsibility – See [http://www.dofactory.com/Patterns/PatternChain.aspx][15]. While I haven’t used this one, it would be very helpful for processing items in a queue.
* Testing - [http://en.wikipedia.org/wiki/Software_testing][16]. Lots of areas to cover such as white box, black box, grey box, regression, non-functional (performance, load, globalisation), unit vs. integration testing, code coverage etc. 
  * Mocking – Used to provide mock dependencies for unit testing. See [http://en.wikipedia.org/wiki/Mock_object][17], [http://martinfowler.com/articles/mocksArentStubs.html][18] and [http://msdn.microsoft.com/en-us/magazine/cc163904.aspx][19].
  * Stubbing – Used to provide a dependency for unit testing with predetermined answers and behaviour. See [http://en.wikipedia.org/wiki/Test_stubs][20].

Jeremy Miller also writes a series called Patterns in Practice for MSDN Magazine (see list at [http://msdn.microsoft.com/en-au/magazine/cc720886.aspx][21]). They are all a good read, especially:

* Cohesion and Coupling - [http://msdn.microsoft.com/en-au/magazine/cc947917.aspx][22]
* Design for Testability - [http://msdn.microsoft.com/en-au/magazine/dd263069.aspx][23]

**Tools**

These are tools that are good to get experience using. Most of them relate to the patterns above. There are other tools used like ReSharper, dotTrace, StyleCop, WinMerge etc, but they don’t have the up skill requirement that the following tools do.

* Reflector - [http://www.red-gate.com/products/reflector/][24]. This is a .Net assembly disassembler that has really good search and analysis tools. This is the most important development tool for me. It is as critical to my development as Visual Studio itself.
* Rhino Mocks - [http://ayende.com/projects/rhino-mocks.aspx][25]. Mocking framework for testing.
* Unity - [http://www.codeplex.com/unity][26]. Dependency Injection framework included in Microsoft Patterns and Practices Enterprise Library.
* PostSharp - [http://www.postsharp.org/][27]. Used for AOP.
* MSTest – Unit testing integrated into Visual Studio.

Probably the most important tool to start using is Rhino Mocks. The only way to get experience with it is to start writing unit tests in VS. See the documentation at [http://ayende.com/wiki/Rhino+Mocks+Documentation.ashx][28]. 

[0]: http://butunclebob.com/ArticleS.UncleBob.PrinciplesOfOod
[1]: http://www.davesquared.net/2009/01/introduction-to-solid-principles-of-oo.html
[2]: http://en.wikipedia.org/wiki/Law_of_Demeter
[3]: http://en.wikipedia.org/wiki/Don't_repeat_yourself
[4]: http://codebetter.com/blogs/jeremy.miller/archive/2005/10/06/132825.aspx
[5]: http://en.wikipedia.org/wiki/Test-driven_development
[6]: http://en.wikipedia.org/wiki/Domain-driven_designhttp://msdn.microsoft.com/en-us/magazine/dd419654.aspx
[7]: http://msdn.microsoft.com/en-us/magazine/dd419654.aspx
[8]: http://en.wikipedia.org/wiki/Aspect-oriented_programming
[9]: http://en.wikipedia.org/wiki/Service_locator_pattern
[10]: http://msdn.microsoft.com/en-us/library/cc707905.aspx
[11]: http://www.dofactory.com/Patterns/Patterns.aspx
[12]: http://www.dofactory.com/Patterns/PatternFactory.aspx
[13]: http://www.dofactory.com/Patterns/PatternFacade.aspx
[14]: http://www.dofactory.com/Patterns/PatternProxy.aspx
[15]: http://www.dofactory.com/Patterns/PatternChain.aspx
[16]: http://en.wikipedia.org/wiki/Software_testing
[17]: http://en.wikipedia.org/wiki/Mock_object
[18]: http://martinfowler.com/articles/mocksArentStubs.html
[19]: http://msdn.microsoft.com/en-us/magazine/cc163904.aspx
[20]: http://en.wikipedia.org/wiki/Test_stubs
[21]: http://msdn.microsoft.com/en-au/magazine/cc720886.aspx
[22]: http://msdn.microsoft.com/en-au/magazine/cc947917.aspx
[23]: http://msdn.microsoft.com/en-au/magazine/dd263069.aspx
[24]: http://www.red-gate.com/products/reflector/
[25]: http://ayende.com/projects/rhino-mocks.aspx
[26]: http://www.codeplex.com/unity
[27]: http://www.postsharp.org/
[28]: http://ayende.com/wiki/Rhino+Mocks+Documentation.ashx
