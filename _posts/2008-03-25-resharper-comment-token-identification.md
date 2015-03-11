---
title: ReSharper comment token identification
categories : Applications
tags : ReSharper
date: 2008-03-25 14:33:23 +10:00
---

ReSharper has a handy feature where it will identify tokens in your comments such as todo, note, bug etc. The Visual Studio IDE has a similar feature for TODO where it will identify those comments in the Task List window.

My issue with the ReSharper implementation is that it will identify these tokens even if they are in the middle of the comment rather than just at the start of the comment line. I originally [posted an issue][0] into the JetBrains Jira system thinking that this behaviour was not configurable, but then found that ReSharper identifies these tokens with regular expressions. If you open up ReSharper -&gt; Options -&gt; To-do Items, you will see a set of patterns identified. 

The patterns defined are Todo, Note and Bug (I added Hack as a duplicate of Bug to support the default Visual Studio HACK token). I modified these regular expressions to ignore words beginning before the token. Instead, I just look for the beginning of the line. For example, the Note pattern was:

<!--more-->

{% highlight text %}
(\W|^)(?<TAG>NOTE)(\W|$)(.*)
{% endhighlight %}

I have modified this to:

{% highlight text %}
^(?<TAG>NOTE)(\W|$)(.*)
{% endhighlight %}

This will now only identify note comments if the NOTE token exists at the beginning of the comment only.

[0]: http://www.jetbrains.net/jira/browse/RSRP-62418
