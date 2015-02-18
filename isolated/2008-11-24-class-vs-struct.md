---
title: Class vs Struct
categories : .Net, Software Design
tags : Useful Links
date: 2008-11-24 13:09:00 +10:00
---

<p>
There is a lot of information around that discusses the differences between classes and structs. Unfortunately there isn&#39;t a lot of information available about when to use one over the other. 
</p>
<p>
MSDN has <a href="http://msdn.microsoft.com/en-us/library/ms229017.aspx" target="_blank">a good resource</a> which provides guidance on how to choose between classes and structs. It starts by describing the differences between the two and then provides the following advice. 
</p>
<blockquote>
	<div class="subsection">
	<p>
	<em><span class="label">Consider defining a structure instead of a class if instances of the type are small and commonly short-lived or are commonly embedded in other objects.</span> </em>
	</p>
	</div>
	<h4 class="subHeading"><em><!----></em></h4>
	<div class="subsection">
	<p>
	<em><span class="label">Do not define a structure unless the type has all of the following characteristics:</span> </em>
	</p>
	<ul>
		<li><em>It logically represents a single value, similar to primitive types (integer, double, and so on). </em></li>
		<li><em>It has an instance size smaller than 16 bytes. </em></li>
		<li><em>It is immutable. </em></li>
		<li><em>It will not have to be boxed frequently. </em></li>
	</ul>
	<p>
	<em>If one or more of these conditions are not met, create a reference type instead of a structure. Failure to adhere to this guideline can negatively impact performance. </em>
	</p>
	</div>
</blockquote>
<p>
The one that got me was having an instance size of 16 bytes or smaller. Several of the classes that I wanted to convert into structs defined string properties. Initially, I thought that a string would almost always be over 16 bytes making it inappropriate for a struct. 
</p>
<p>
It later occurred to me that strings are reference types not value types. Any string variable is simply a pointer to the memory location that holds the data for that reference type. This means that the size of a string property in a struct is the size of IntPtr. 
</p>
<p>
Structs are back on the menu. 
</p>
<p>
Some useful links are: 
</p>
<ul>
	<li><a href="http://msdn.microsoft.com/en-us/library/ms229036.aspx" target="_blank">MSDN - Type Design Guidelines</a> </li>
	<li><a href="http://msdn.microsoft.com/en-us/library/ms229017.aspx" target="_blank">MSDN - Choosing Between Classes And Structures</a> </li>
	<li><a href="http://msdn.microsoft.com/en-us/library/ms229031.aspx" target="_blank">MSDN - Structure Design</a>  </li>
	<li><a href="http://dotnetperls.com/Content/Struct-Examples.aspx" target="_blank">DotNetPearls - Struct Examples And Tricks</a> </li>
</ul>

