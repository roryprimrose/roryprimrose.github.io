---
title: Calling a workflow service operation multiple times
tags : WCF, WF
date: 2011-03-10 14:23:43 +10:00
---

Ron Jacobs has just answered an [interesting question][0] over on his blog. The question is about whether a workflow service operation can be invoked multiple times. Ron does not provide the details of the question but the example he provides implies that the implementation of the two invocations of the same service operation may be different as the same operation name is implemented twice in the workflow. This seems like a design issue as far as the service goes but the question itself is still interesting.  

If we assume that the implementation of the service operation is the same (as it should be), how do we keep the service alive so that we can invoke the same method multiple times?   

The answer is by using some kind of service session using WF correlation. Content correlation is my preferred option because it is independent of infrastructure concerns and does not restrict the WCF bindings available to you. I have [previously posted][1] about how to get a workflow service to create a session using content correlation.  

<!--more-->

With respect to the question put to Ron, you would not be able to achieve this result with just one service operation on the service. Correlation requires the client to provide the correlation value to the service operation. The correlation value must then map to an existing WF instance. This means that the first service operation cannot be the service operation invoked multiple times. You will need a service operation that creates the service session by returning a session identifier that can then be used for content correlation on subsequent service operations. This first operation has the CanCreateInstance set to true and will be the entry point into the service. A DoWhile activity can then allow a service operation to be invoked multiple times within that session. The WF instance will remain alive (or persisted) until the workflow exists. The DoWhile activity prevents this from happening until some kind of exit condition is met.  

I have implemented this design in a DataExchange service in my [Jabiru project][2] on CodePlex. The StartSession operation generates a Guid and returns it with some other service context information. The DoWhile then has a check for whether the session is completed. The session will be completed by one of the following conditions:

* a timeout
* CancelSession is called
* FinishSession is called

This service design can be seen in the following screenshot (full image is linked).  

[![][4]][3]

The timeout case is handled in the first pick branch by using a Delay activity and then a check of the current time against when the service was last hit.  

![][5]

Each other pick branch has a service operation in it. The first action taken by each of these service operations is to set LastActivity = DateTime.Now() in order to prevent the timeout case. Each of these service operations (such as the ReadSegment operation) within the DoWhile can be invoked multiple times using the same session identifier while the SessionCompleted flag is False.  

![][6]

The CancelSession operation simply assigns SessionCompleted flag as True. This will then allow the DoWhile activity to exit and the service session will be finished as far as the client is concerned.  

![][7]

Similarly, the FinishSession operation sets the SessionCompleted flag as True and then does some other work relating to actioning the session outcome.  

![][8]

Note the Delay activity directly after the Reply activity. This allows the workflow to push the response back to the client and then process more work asynchronously. The same technique is used after the DoWhile so that session related resources on the server can be cleaned up asynchronously after the client has finished with the service.  

We have seen here that a service operation can be invoked multiple times by using WF correlation and a DoWhile activity.

[0]: http://blogs.msdn.com/b/rjacobs/archive/2011/03/09/wf4-workflow-services-can-you-use-the-same-operation-more-than-once.aspx
[1]: /2010/11/08/hosted-workflow-service-with-content-correlation/
[2]: http://jabiru.codeplex.com
[3]: /files/image_85.png
[4]: /files/image_thumb.png
[5]: /files/image_86.png
[6]: /files/image_87.png
[7]: /files/image_88.png
[8]: /files/image_89.png