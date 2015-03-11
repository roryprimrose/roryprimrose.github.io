---
title: Styles come and go, but habits stick around much longer
categories : .Net, IT Related
date: 2006-06-07 09:31:00 +10:00
---

I have been reading up on naming standards today. It has been quite a while since I challenged by naming convention habits.

This has all come about because I was using [GhostDoc][0] today to quickly put in the bulk of comments in an assembly for me to then go through and tweak.What I found was that GhostDoc didn't like some of my parameter naming. This was the catalyst for my reading about naming standards after thinking about naming standards for quite a while.

I read [Microsoft's naming guidelines][1], but it seemed to not address the naming standards of member level variables. The closest discussion it seemed to get on the issue was [guidelines for static fields][2]. What standards are people using for member level variables? I read a [recent post][3] where it seems that everyone has a different opinion.

<!--more-->

I have come from a VB5/6 background (Hungarian notation) with some m_ C++ style thrown into the mix. I already follow Microsoft's naming guidelines for namespaces, classes, methods and properties, so it seems that it is just parameter and variable naming that has to change.

It seems like the common standard these days is to PascalCase everything apart from parameters and variables which should be camelCase. No Hungarian notation anywhere, although I think I will continue Hungarian for local variables. As for member level variables, the best standard seems to be camelCase prefixed with _ instead of m_, but no Hungarian prefix.

Any ideas or comments?

On a side note, I find it interesting that my method naming is becoming more verbose for readability now that development environments (especially C#) have much better support for minimising keystrokes than they used to. Other than for better readability (as long as the naming length is not over the top), a great side affect is much better GhostDoc comment generation.

[0]: http://www.roland-weigelt.de/ghostdoc/
[1]: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconNamingGuidelines.asp
[2]: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconstaticfieldnamingguidelines.asp
[3]: http://discuss.joelonsoftware.com/default.asp?dotnet.12.340921.18
