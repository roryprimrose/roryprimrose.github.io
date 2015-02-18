---
title: Stardust theme update instructions for BlogEngine.Net 2.0
categories : .Net
tags : BlogEngine.Net
date: 2011-01-03 11:54:56 +10:00
---

I had a couple of issues with the excellent [Stardust theme][0] I use on this site after upgrading to [BE] 2.0. The following are the changes I made to make the theme compatible.

1. Post.IsCommentEnabled has been changed to Post.HasCommentEnabled. This appears in themes/stardust/PostView.ascx.
1. Login.aspx has been moved into the Account folder. Modify the reference to this in themes/stardust/site.master in the code MenuClass(&quot;account/login.aspx&quot;)
1. Similarly to #2, modify the reference to login.aspx in themes/stardust/site.master.cs in the two references to aLogin.HRef

All these three references need to be changed from login.aspx to account/login.aspx.

[0]: http://www.onesoft.dk/post/Stardust-theme-ready-for-BlogEngine.aspx
