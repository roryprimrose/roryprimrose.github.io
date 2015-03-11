---
title: Should code contracts be tested?
categories : .Net, Software Design
date: 2012-03-20 21:26:23 +10:00
---

I have been writing unit tests for my classes that use code contracts (Contract.Requires&lt;T&gt;) just like I did with the old style guard clauses basically since code contracts were released. My reasoning for testing them has always been that the unit test code should not make any assumptions about the implementation of the SUT and ideally should have no understanding about how it is implemented. Instead it should just test the behaviour. 

If a method that takes a reference type as a parameter and a value must be supplied, then the expectation is that the method will throw an exception if null is provided. I have always believed that this behaviour should be tested regardless of whether this is implemented as a traditional guard clause, a Contract.Requires on the method implementation or a Contract.Requires on the interface or base class.

<!--more-->

I saw someone mention recently that they didn’t think that code contracts should be tested. This has lead me to question the value of the practice. A crisis of testing faith perhaps. 

My two thoughts about this so far are:

* Code contracts are a totally different beast than traditional guard clauses

  * Compiler support
  * Design time analysis
  * Compile time analysis
  * Intellisense support
  * Runtime enforcement (assuming that the contract has been written correctly and is therefore not a source of a bug itself)

* Testing all these contracts is starting to look more like infrastructure code

Regarding the last point, I am referring to infrastructure code in that it is important for running the application but is repetitive, is not the meaty part of the application and doesn’t really have much value in and of itself.

Thoughts?


