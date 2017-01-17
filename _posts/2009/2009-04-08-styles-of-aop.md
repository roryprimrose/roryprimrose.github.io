---
title: Styles of AOP
categories: .Net
tags: AOP, Performance, PostSharp
date: 2009-04-08 23:55:00 +10:00
---

 There are many AOP frameworks around and they all use one of just a few techniques to inject code into join points. [Oren][0] has previously [posted a list][1] of these types of AOP techniques with their advantages and disadvantages. This list is copied below for reference: 

<!--more-->

| # | Approach | Advantages | Disadvantages |
| --- | --- | --- | --- |
| 1 | Remoting Proxies | Easy to implement, because of the .Net framework support | Somewhat heavy weight. Can only be used on interfaces or MarshalByRefObjects |
| 2 | Deriving from ContextBoundObject | Easiest to implement. Native support for call interception | Very costly in terms of performance |
| 3 | Compile-time subclassing ( Rhino Proxy ) | Easiest to understand | Interfaces or virtual methods only |
| 4 | Runtime subclassing ( Castle Dynamic Proxy ) | Easiest to understand. Very flexible | Complex implementation (but already exists) Interfaces or virtual methods only |
| 5 | Compile time IL-weaving ( Post Sharp / Cecil ) | Very powerful. Good performance | Very hard to implement
| 6 | Runtime IL-weaving ( Post Sharp / Cecil ) | Very powerful. Good performance | Very hard to implement |
| 7 | Hooking into the profiler API ( Type Mock ) | Extremely powerful | |

## Performance
 Complex implementation (COM API, require separate runner, etc)

 What I want in an AOP framework is for it to not be limited to interfaces and virtual methods (1, 3, 4) and preferably to not require decorating types in order to support an AOP framework (1, 2). It also needs to perform well and preferably be free. 

 My requirements seem to sit in between the easy to implement but not very powerful at one end (1, 2, 3, 4) and incredible powerful but hard to implement or with performance implications on the other (7). PostSharp sits nicely with my requirements. Compile time IL weaving with PostSharp is hard to implement whereas runtime weaving (PostSharp.Laos - redirecting runtime method calls by compile time weaving) is a lot easier. 

 I will publish a post for each of the two PostSharp methods and indicate their coding differences, IL impact and license considerations. 

 On a side note, TypeMock recently released an [open AOP API][2] for open source projects. 

[0]: http://ayende.com/Blog/
[1]: http://ayende.com/Blog/archive/2007/07/02/7-Approaches-for-AOP-in-.Net.aspx
[2]: http://cthru.codeplex.com/Wiki/View.aspx?title=Typemock%20Open-AOP%20API
