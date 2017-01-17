---
title: Hosted workflow service with content correlation
categories: .Net, Applications
tags: WCF, WF
date: 2010-11-08 16:33:00 +10:00
---

Content correlation in hosted WF services seems daunting but is surprisingly simple to use with the available designer support. Content correlation uses data from the workflow to link (correlate) multiple WCF service operations to the same hosted workflow instance. Content correlation is different to context correlation where WCF communication down at the binding level handles correlation. There is unfortunately a lack of detailed examples available on how to get content correlation to work. This post aims to fill that gap.

I have an account registration scenario where the first service operation is to request a new account. The request needs to provide a first name, last name, email address and a password. The register account workflow will run some validation logic and then send an email to the email address with a link back to a website for confirmation. 

<!--more-->

The confirmation of a registration requires a TransactionId and ChallengeCode. The confirmation link in the email contains the TransactionId and registration request operation returns the ChallengeCode to the user. The service never returns the TransactionId and the email never contains the ChallengeCode. 

The user must provide both the TransactionId and the ChallengeCode to the confirmation screen of the website to confirm the registration. This design provides registration security because it ensures that the registration belongs to a valid email address and that the person confirming the registration is the person who made the registration request because only they have both the TransactionId and the ChallengeCode combination. This is similar to how public/private keys work in cryptography.

The registration service workflow looks like the following.![image][0]

The registration service receives the message for RegisterAccount and runs the register account logic. This process generates the TransactionId and a ChallengeCode and sends the confirmation email. The workflow then waits for the confirmation request to come back or for a timeout value to occur. Some kind of correlation is required to link the RegisterAccount service operation to the ConfirmRegistration service operation to ensure that the same workflow instance executes the related service operations.

This registration process cannot use context correlation because there is no WCF binding available that would support this scenario. Different applications may execute the registration and confirmation operations with no knowledge between them. A rich client may make the original registration request while the website processes the confirmation. Context correlation still would not work even if the original registration request and subsequent confirmation were processed by the same website as there is no WCF session between the two website requests. This workflow must use content correlation to link the original registration request with the subsequent confirmation request. 

The first step of correlation is to define a CorrelationHandle variable on the workflow at a scope level that is available to both service operations. This variable is the reference that links the service operations using the nominated correlation data.![image][1]

There are two ways that content correlation may satisfy this registration workflow. The first method uses content references that are only available in the service contract. The second method provides a content correlation solution that only partially relies on the service contract data.

## Method 1 – Content correlation based on service contract data

This first method uses the correlation support in solely available in the SendReceive activity pairs. The value to correlate on in this case is the ChallengeCode value as it exists as part of the service contract for the RegisterAccount call (the response) and the ConfirmRegistration call (a request property). 

The CorrelationInitializers property for the RegisterAccount SendReceive activities must define the content data to correlate the two service operations. The ChallengeCode is generated as part of the RegisterAccount workflow so this value cannot be assigned in CorrelationInitializers of the _Receive RegisterAccount Request_ activity as the value has not yet been determined. It can however be assigned in the related _Send RegisterAccount Reply_ activity.![image][2]![image][3]

The CorrelationHandle variable must be entered on the left. When selected, there are several options available for correlation support. The option to select is _Query correlation initializer_. The next step is to define an XPath query to the content value in the service contract. The dialog helps out here by providing a dropdown (indicated in the screenshot). The dropdown options provide an easy way to enter the XPath Queries value based on the service contract related to the activity. In this case the RegisterAccountResult (the ChallengeCode) is selected and the XPath Query of _sm:body()/xgSc:RegisterAccountResponse/xgSc:RegisterAccountResult_ is entered automatically.

The next step is to configure the Receive activity of the ConfirmRegistration service operation. ![image][4]

The CorrelationHandle variable is defined on the activity as indicated in the above screenshot. The CorrelatesOn property now needs to be set in order to link the ChallengeCode in the ConfirmRegistration service operation with the ChallengeCode generated by RegisterAccount. This will allow the WorkflowServiceHost to identify the correct workflow instance to load to process ConfirmRegistration.![image][5]

The CorrelatesOn dialog provides similar assistance in defining the XPath query to match the content correlation data. In this case the service contract is seen in the drop-down list and the ConfirmRegistrationRequest.ChallengeCode value can be chosen. This results in the XPath Query of sm:body()/xgSc:ConfirmRegistration/xgSc:request/xg0:ChallengeCode. It is important to note that the CorrelationInitializers and CorrelatesOn property values between these activities define the same correlation key value.

Content correlation will now be able to load the correct workflow instance if the ConfirmRegistration service operation request contains the ChallengeCode value returned from a prior call to RegisterAccount (unless the timeout activity has fired first).

## Method 2 - Content correlation based on partial service contract data

Defining content correlation on the SendReceive activities in this way restricts you to data that is common between the service contracts for both service operations. This has forced the implementation to use ChallengeCode instead of TransactionId. This is not technically a problem as both values will be unique. The TransactionId value is closer to the concepts of WF instance management and persistence than the ChallengeCode and is a little more appropriate from a design point of view. The ChallengeCode is more of a business-orientated value that has nothing to do with workflows or correlation. Some minor changes to the workflow will be able to use TransactionId rather than ChallengeCode.

The first change is to remove the CorrelationInitializers configuration in _Send RegisterAccount Reply_ activity. Next, we need to drop on an InitializeCorrelation activity before _Send RegisterAccount Reply_ activity. The Correlation property is assigned the CorrelationHandle variable that has been used so far.![image][6]

The CorrelationData property is not restricted to values defined in the service contract. This allows us to define content correlation to use another variable defined in the workflow. We can now select _View…_ and enter the TransactionId value as this is stored in a workflow variable.![image][7]

The first thing to note here is that the same correlation key name is used. The second important thing to note is that content correlation must use a string value. The TransactionId value is a Guid so a ToString will cater for this.

The final change is to update the CorrelatesOn definition on the _Receive ConfirmRegistration Request_ activity. This property will now reference the TransactionId of the ConfirmRegistrationRequest object rather than ChallengeCode.![image][8]

The content correlation will still work here even though the correlation data is a string representation of the TransactionId. Presumably this is because the XPath query is run against the WCF message (as an XML structure) which will result in a string value for TransactionId. Correlation will work in the same way as when ChallengeCode was used as long as the same TransactionId value is provided to the ConfirmRegistration service operation that was generated by RegisterAccount.

In this post we have looked at two ways of getting content correlation support to link two WCF service operations in a hosted workflow service. The remaining obstacle is how to return a meaningful service fault back if the user provides an invalid TransactionId to the ConfirmRegistration operation. I hope that a viable solution will pop up in the next post.

[0]: /files/image_52.png
[1]: /files/image_53.png
[2]: /files/image_54.png
[3]: /files/image_55.png
[4]: /files/image_56.png
[5]: /files/image_57.png
[6]: /files/image_58.png
[7]: /files/image_59.png
[8]: /files/image_60.png
