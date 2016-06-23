---
title: WCF and large messages
categories: .Net, IT Related
tags: WCF
date: 2006-12-05 11:11:00 +10:00
---

I am getting some interesting results when messages are returned from a WCF service. Small responses come through fine. When the responses start to get large, I run into the [QuotaExceededException][0] exception. This is fine because you can increase the [MaxReceivedMessageSize][1] configuration value on the client endpoint. This starts to fail when the size of the data continues to increase and eventually I get the exception _WebException: The underlying connection was closed: The connection was closed unexpectedly__._

The service call is still being made (I can debug it), but it seems that the error is coming back quick enough that it is a problem with the server endpoint, rather than client endpoint or configuration on the client.

Anyone come across this before???

[0]: http://msdn2.microsoft.com/en-us/system.servicemodel.quotaexceededexception.aspx
[1]: http://msdn2.microsoft.com/en-us/system.servicemodel.wshttpbindingbase.maxreceivedmessagesize.aspx
