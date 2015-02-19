---
title: WF content correlation and security
tags : WCF, WF, WIF
date: 2011-03-10 22:14:35 +10:00
---

I have [posted previously][0] about using content correlation in WF services to implement a service session. One issue that must be highlighted regarding content correlation is about the security of the session in relation to hijack attacks.

I am writing a workflow service that is a combination of IIS, WCF, WF, WIF and AppFabric. WIF is used to secure the WCF service to ensure that only authenticated users can hit the endpoint. WIF then handles claim demands raised depending on the actions taken within the service by the authenticated user. A session hijack can occur with content correlation where authenticated UserA starts the service and then authenticated UserB takes the content used for correlation and makes their own call against the service. In this case UserB is authenticated and passes through the initial WIF authentication. UserB could then potentially take actions or obtain data from the service related to UserA.

The way to protect the service against this session hijack attack is to hold on to the identity of the user that started the session. Each service call within the session should then validate the identity of the caller against the original identity. The service execution can continue if the identities match, otherwise a SecurityException should be thrown.

In my application, the StartSession service operation does this first part. ![image][1]

The StartSession service operation is the first for the session and (among other things) configures the service for content correlation. It uses my [ReceiveIdentityInspector activity][2] to obtain the identity of the user that is invoking the service. It then stores this identity in a workflow variable that is scoped in such a way that it is available to the entire lifecycle of the workflow.

Each other service operation then uses the same ReceiveIdentityInspector to get the identity of the user invoking those operations.![image][3]

All these other service operations can then compare the two identities to protect the service against a hijack attack. The following condition is set against the Invalid Identity activity above:

> _ReadSegmentIdentity Is Nothing OrElse ReadSegmentIdentity.IsAuthenticated = False OrElse ReadSegmentIdentity.Name &gt;&gt; SessionIdentity.Name_

A SecurityException is thrown if this condition is evaluated as True. An authenticated user is now unable to hijack the service of another user even if they can obtain the value used for content correlation. 

Another security measure to protect the content correlation value (and all the data of the service) to use ensure that SSL is used to encrypt the traffic for the service. This should not however remove the requirement for the above security check. Additionally, you should also write integration tests that verify that this potential security hole is successfully managed by your service application.

[0]: /post/2010/11/08/Hosted-workflow-service-with-content-correlation.aspx
[1]: //files/image_90.png
[2]: /post/2011/02/21/Extract-WCF-identity-into-a-WorkflowServiceHost-activity.aspx
[3]: //files/image_91.png
