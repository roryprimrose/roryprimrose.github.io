---
title: Caching should not be the source of truth
categories: Software Design
date: 2008-09-25 10:44:30 +10:00
---

I just read the [Two Reasons You're Not Using the Cache and How To Deal With Them][0] article on VSMag. Overall, I like the intention of the article which is to promote caching, but I have some issues with some of the points made. 

In the article, Peter addresses two of the common reasons that cause people to not use caching. These are data being stale and losing data. There is some good advice regarding stale data. People need to determine the volatility of their data to decide on cache expiration policies. There are also some notification services that can be used to flush the cache when data stores are updated.

The article then discusses issues about losing data. This is where I don't like the guidance provided. What I can't agree with in the article is the concept of having the cache as the source of truth of the application data. The article promotes the idea of keeping application data in the cache (usually in memory) when it is created or updated and only providing that information to the back-end data store when the item is removed from the cache.

<!--more-->

The first thing that comes to mind is that if there is a power failure, you lose your data. He mentions two things about this. Firstly, he doesn't have a solution for this (there isn't one for this design as a power failure is a power failure). Secondly, if you have a power failure, you have bigger problems. 

Well, the later might be true, but that doesn't mean you should use a design that destroys data just because you do have bigger problems like a power failure. In today's world, data has an incredibly high value and needs to be treated with more respect than that. As he discusses, web farms are also difficult in scenarios where the cache is the source of truth.

In my opinion, caching is there to assist performance. That is its only job. It shouldn't be used to hold on to data that doesn't exist in a back-end data store. Caching should be a view of data stored, not represent the data store itself (even temporarily). Appropriate expiration policies need to be applied to cache data according to business requirements and data volatility. Within the confines of those policies, if the cache has data, it should serve it. If it doesn't it can be populated with data from the back-end store until it is flushed according to the expiration policies for that data.

If this simple design of cache usage is implemented, there is no risk of losing data. It gives performance as a bonus as it is able.

[0]: http://visualstudiomagazine.com/columns/article.aspx?editorialsid=2814&amp;rss=1
