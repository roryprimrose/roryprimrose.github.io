---
title: Dynamic searching in TSQL
categories: IT Related
tags: Useful Links
date: 2007-06-19 20:05:52 +10:00
---

As I have been writing some SQL Server based software recently, I want to expose a standard way of providing searching capabilities for tables and views. The initial implementation started with a set of WHERE conditions like the following:

> WHERE (FieldA = @FieldAValue OR @FieldAValue IS NULL)  
>  (FieldB = @FieldBValue OR @FieldBValue IS NULL)

I now want to extend this functionality to include sorting and perhaps paging as well. I came across [this article][0] by [Erland Sommarskog][1] (SQL Server MVP) which address a ton of sorting variations.

[0]: http://www.sommarskog.se/dyn-search.html
[1]: http://www.sommarskog.se/index.html
