---
title: WorkflowApplication throws ObjectDisposedException on ActivityContext with LINQ queries
categories : .Net
tags : WF, WIF
date: 2011-01-23 13:33:00 +10:00
---

I have written a simple WF4 activity that validates a WIF IClaimsIdentity has certain claims attached to it for a specific claim value. I wrote several unit tests for this activity to validate the implementation. The basic logic is to select a subset of claims that have a specified claim value using Where(). I then fall into a flow chart that validates whether there are any claims (Any() == false) and then whether there is only one claim (First() with a Count()) of a specified claim type that will not be accepted.  

![][0]

Almost all of the tests failed with the following error:  

<!--more-->

> _Test method Neovolve.Jabiru.Server.Business.UnitTests.Activities.DemandAnyDataChangeClaimTests.DemandAnyDataChangeClaimThrowsExceptionWithManageOnlyClaimTest threw exception System.ObjectDisposedException, but exception System.Security.SecurityException was expected. Exception message: System.ObjectDisposedException: An ActivityContext can only be accessed within the scope of the function it was passed into.  
> Object name: 'System.Activities.ActivityContext'._

This seemed a little strange as the activity being tested is very simple. The only complexity is that the activity does use a few LINQ queries. Strangely one of the tests did pass and it was one that tested an identity without any claims. I then made a change to one of the other failed tests to remove it’s claim from the test identity and the test then passed. The most likely cause of the exception is the LINQ queries against the claims of the identity.  

So this clearly seemed like a bug somewhere in WF. The first net search result turned up with [this post][1] on MSDN forums which does indicate a bug with WF and LINQ.  

Andrew notes in the forum response that this is a bug that will not get fixed by RTM so it is probably the same issue I am having here. The forum makes some suggestions about how to get around this issue but I was hoping for a simple workaround.   

I guessed that perhaps the issue might be due to LINQ queries against a particular IEnumerable&lt;T&gt; type. I then tried putting a ToList() onto the original Where query. Now all the tests pass as expected. This simple workaround is just changing the data type to IList for the other LINQ operations (Any(), First() and Count()) to operate on rather than the IEnumerable returned from Where(). 

[0]: /files/image%5B2%5D.png
[1]: http://social.msdn.microsoft.com/Forums/en/wfprerelease/thread/1ecdf960-093e-47b5-a984-d32f3dd03b1e