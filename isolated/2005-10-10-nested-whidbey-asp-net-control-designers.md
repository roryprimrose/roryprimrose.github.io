---
title: Nested Whidbey ASP.Net control designers
categories : .Net
date: 2005-10-10 13:54:00 +10:00
---

I have been designing and building a nested set of web controls over the last couple of weeks. By nested, I mean that there is a parent control that contains a set of child controls of a specific type.

I like to have default behaviours of my web controls in the designer when they are empty. For example, the parent control would add 3 child controls if it was empty and the child control would display something if it was empty, like how the label displays "[ID]" when the text property is empty.

One thing that surprised me was that the designers for the child control don't get called when rendering the parent control in the designer. This means that the behaviour of the parent's designer must also do the designer work of the child control. 

I am sure this is not right. One of the reasons I think I am missing something here is that if I take the panel control (which has a designer), and I put my own control in it that has a designer, my controls designer gets called. Why would it be different with two of my own nested controls?

I hope I am missing something because the nested controls should be calling their own designers to help with design-time rendering rather than relying on parent controls which would typically be completely unrelated (the panel isn't going to understand how to handle an empty label control).

I will have to check my code again.


