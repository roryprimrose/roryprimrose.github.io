---
title: Tricky paging - Learning something new everyday
date: 2005-11-12 02:08:00 +10:00
---

 My team lead sent us an email with a link to [http://smh.com.au/articles/2005/11/02/1130823242025.html][0]. He stumbled across a cool bit of functionality when he copied the contents of this five page article. He found that if you select the entire page, copy it to the clipboard then paste it into a program (like notepad), then all five pages of the article are pasted into the document (although this doesn't seem to work when pasting into Word though). 

 How does this work? 

 Firstly, the contents of all five pages are in the source code for each page downloaded. The contents for each page is surrounded in a DIV that has a class and an ID that identifies the page number. There is a script in the page that looks for a querystring that indicates the page number. It loops through each page content DIV and shows or hides it as appropriate. There is a stylesheet reference in the page defined as media="print" that includes a reference to the css class. This class ensures that when the document is printed, all the page content DIVs are rendered. 

 So that covers displaying the correct page in the browser, and also printing all pages. What about copying it to the clipboard? 

 I originally thought that a copy operation was using the media="print" stylesheet. With a little testing, it seems that this is not the case. A copy seems to ignore the display style and goes off the HTML defined instead. In the case of this page, a copy has the same effect as the media="print" stylesheet in that all page content DIVs are included. 

 Whether the copy behavior was understood by the developers or not, it is a very cool side-effect. 

[0]: http://smh.com.au/articles/2005/11/02/1130823242025.html
