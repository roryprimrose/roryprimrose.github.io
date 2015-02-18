---
title: Public Fields vs. Public Properties
categories : .Net, IT Related
date: 2006-10-06 09:07:22 +10:00
---

Just came across [this post][0] in my feeds this morning. Interesting read.

I think there is a lack of consistency in implementations (event handlers being the classic case), but the contract/interface argument leans me towards always wrapping fields in properties. Using properties also means that you can change the code without having to change the structure of the class.

Any thoughts? Is [Ian Cooper][1] a witch? Should he be burnt at the stake? Is there a [Monty Python][2] take on this?

[0]: http://iancooper.spaces.live.com/Blog/cns!844BD2811F9ABE9C!251.entry
[1]: http://iancooper.spaces.live.com/blog/
[2]: http://www.mwscomp.com/movies/grail/grail-05.htm
