---
title: Generally speaking, name collisions are bad
categories: .Net
date: 2005-09-24 01:32:00 +10:00
---

 I have been developing some Whidbey web custom controls for the last couple of days. I have been getting a little frustrated with what looked like (and probably still is) bugs in the designer. 

 I have been creating a custom tabbar control. Let's call it MyNamespace.Tabbar. I have designers, builders and converters to improve the designer support. Most of the time, the designer support just doesn't work. Sometimes I get a great error saying that MyNamespace.Tabbar can't be converted to MyNamespace.Tabbar. Given that both the assembly's namespace and System.Web.UI are imported, I probably have a naming collision on the control. 

 Parts of the framework are still able to determine that I am referring to my Tabbar class rather than the System.Web.UI.WebControls.Tabbar (given the error message rendered in the designer), but elsewhere, it can't follow through (hence the error). BTW, at runtime, none of this is a problem. Interestingly, even when I clarified the declaration with the full namespace, it still fell over in the same spots in the designer. 

 After a name change, it is all fine. 


