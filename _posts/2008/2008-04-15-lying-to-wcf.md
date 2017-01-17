---
title: Lying to WCF
categories: .Net, IT Related
tags: Useful Links, WCF
date: 2008-04-15 20:16:00 +10:00
---

There are cases when you need to transmit username/password credentials to WCF without transport security. The times that you should do this are rare because of the obvious security implications of sending credentials over the wire without encryption. One case where this is required is where hardware acceleration is used in a load balancer. The traffic between the load balancer and the client is encrypted, but the traffic between the load balancer and the service host is not. The issue here is that the service still needs the credentials passed to the load balancer from the client.

Drew Marsh has a [great write up][0] about how to lie to WCF about the security of the binding that it is using for a service. Nicholas Allen has also posted on the topic [here][1] and [here][2].

[0]: http://blog.hackedbrain.com/archive/2006/09/26/5281.aspx
[1]: http://blogs.msdn.com/drnick/archive/2008/03/28/overriding-protection-for-ipsec.aspx
[2]: http://blogs.msdn.com/drnick/archive/2007/01/17/faking-channel-security.aspx
