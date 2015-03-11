---
title: Class vs Struct
categories : .Net, Software Design
tags : Useful Links
date: 2008-11-24 13:09:00 +10:00
---

There is a lot of information around that discusses the differences between classes and structs. Unfortunately there isn't a lot of information available about when to use one over the other. 

MSDN has [a good resource][0] which provides guidance on how to choose between classes and structs. It starts by describing the differences between the two and then provides the following advice. 

> _Consider defining a structure instead of a class if instances of the type are small and commonly short-lived or are commonly embedded in other objects._
> _Do not define a structure unless the type has all of the following characteristics:_

> * _It logically represents a single value, similar to primitive types (integer, double, and so on)._
> * _It has an instance size smaller than 16 bytes._
> * _It is immutable._
> * _It will not have to be boxed frequently._

> _If one or more of these conditions are not met, create a reference type instead of a structure. Failure to adhere to this guideline can negatively impact performance._

<!--more-->

The one that got me was having an instance size of 16 bytes or smaller. Several of the classes that I wanted to convert into structs defined string properties. Initially, I thought that a string would almost always be over 16 bytes making it inappropriate for a struct. 

It later occurred to me that strings are reference types not value types. Any string variable is simply a pointer to the memory location that holds the data for that reference type. This means that the size of a string property in a struct is the size of IntPtr. 

Structs are back on the menu. 

Some useful links are: 

* [MSDN - Type Design Guidelines][1]
* [MSDN - Choosing Between Classes And Structures][0]
* [MSDN - Structure Design][2]
* [DotNetPearls - Struct Examples And Tricks][3]

[0]: http://msdn.microsoft.com/en-us/library/ms229017.aspx
[1]: http://msdn.microsoft.com/en-us/library/ms229036.aspx
[2]: http://msdn.microsoft.com/en-us/library/ms229031.aspx
[3]: http://dotnetperls.com/Content/Struct-Examples.aspx