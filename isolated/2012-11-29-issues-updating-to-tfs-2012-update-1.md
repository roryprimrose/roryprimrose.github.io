---
title: Issues updating to TFS 2012 Update 1
tags : TFS
date: 2012-11-29 11:56:36 +10:00
---
We encountered a couple of issues yesterday when updating TFS 2012 from RTM to Update 1. The installation went well on the application tier and the data tier. The upgrade for the build service hit some problems.   ![image][0]The event log on the application tier contains the following error:  
> _Operand type clash: dbo.typ_BuildControllerTableV2 is incompatible with dbo.typ_BuildControllerTable (type SqlException)_
With some quick escalation by [Grant][1], our issues were resolved this morning. Grant has posted about it [here][2].
The only other issue we seem to have hit is that exposing TFS over HTTPS was broken after the install. [Brian Harry indicated][3] this would be the case but did not elaborate on the details. It seems like the installation package for Update 1 has reset the IIS configuration back to its default TFS install. The affect of this is that the https binding was removed from the site. Simply adding this back in has restored our external TFS connectivity.

[0]: /blogfiles/clip_image002%5B4%5D.jpg
[1]: http://blogs.msdn.com/b/granth
[2]: http://stackoverflow.com/questions/13615812/error-when-installing-tfs-2012-update-1-dbo-typ-buildcontrollertablev2-is-inco
[3]: http://blogs.msdn.com/b/bharry/archive/2012/11/26/visual-studio-2012-update-1-is-available.aspx