---
title: Fix the BASE tag, break me
date: 2005-08-31 04:48:00 +10:00
---

The IEBlog has just [posted an entry][0] about the behaviour of the BASE tag in IE7. The standard (back to HTML 3.2) says that the base tag should occur once in the document and it should be located in the HEAD tag. The IE implementation for the last few versions has been that references and links will be relative to the last BASE tag specified. IE7 will remove this behaviour to be more standards compliant.

While I agree with standards compliance, this one will hurt. I have built a feed reader application that displays entries that have been obtained from different sites. It is quite common that when people write their blog entries, they include relative references to images and hyperlinks. 

I have used the IE BASE tag behaviour to get around the broken link problem that my feed reader would otherwise have. For each entry that I render from XML to HTML, I prefix it with a BASE tag that specifies the base href being the url of the web version of the post. As any relative image and anchor urls would be relative to that page, my feed reader was causing IE to correctly identify all the resource for the entries regardless of the site they came from.

How do I get around this? Any ideas?

Without any nice implementation provided already, I will have to parse the contents of each entry, determine the links that are relative and prefix them with the web address for the entry. Quite ugly.

I don't mind if they revert the BASE tag implementation to be more standards compliant. It would have been nice if the IE team provided another way to declare a section of HTML as relative to a base url though.

[0]: http://blogs.msdn.com/ie/archive/2005/08/29/457667.aspx
