---
title: Code Metrics fail to calculate
categories : .Net
date: 2007-12-06 12:25:12 +10:00
---

This is an interesting one. I have a solution for a WCF service that contains the business and data access layers. When I run code metrics for the solution, two of the projects fail. One is the IIS service host project, and the other one is the business layer implementation project. 

Both projects fail because they can't read another module. The modules that they can't read are direct project reference. The reason provided is:

> _Could not resolve type reference: [mscorlib]_

There is no information about this on the net that I can find. Is anyone else finding this problem?


