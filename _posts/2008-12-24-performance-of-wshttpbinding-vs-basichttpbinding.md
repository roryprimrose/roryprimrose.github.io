---
title: Performance of wsHttpBinding vs basicHttpBinding
categories: .Net
tags: Performance, WCF
date: 2008-12-24 11:15:00 +10:00
---

I've been performance testing a WCF service recently and working away at the bottlenecks in the system. After fixing a few performance issues in the components behind the service endpoint (service implementation and beyond), I was still getting really bad throughput calling the distributed service. The service in this case is hosted on a Windows Server 2003 VM. While it is not on physical hardware, I should be able to achieve better performance than the results I was getting.

After 90 seconds into a load test, the resources on the server got saturated and performance dropped through the floor. After this happened for a minute or so, timeouts and security negotiation failures occurred and test executions essentially halted for the remainder of the load test. I noticed that once service requests were no longer being processed that the server was no longer stressed (CPU dropped back down to normal).

<!--more-->

This lead me to think that the reason the host was no longer processing requests was that WCF throttling was occurring. All the test executions passed without timeouts or security negotiation failures once I increased the throttling limits on the host. The test execution time performance still had the same pattern though. The next thought was regarding the binding I was using. The service was configured for wsHttpBinding. This binding has a lot more functionality but also a lot more overhead when compared with basicHttpBinding. To test the theory, I switched the service over to basicHttpBinding.

This is the load test result using wsHttpBinding. The test starts well, then the CPU gets saturated dealing with the wsHttpBinding overhead. The test time starts to bounce around the 8 second mark and the CPU on the server is working way too hard.

![WsHttpBinding][0]

Switching over to basicHttpBinding dropped the average test time down to a consistent 28 milliseconds. That is quite an improvement over 8 seconds. This such an increase in performance, I was able to remove the custom throttling configuration and use the default values without a problem.

![BasicHttpBinding][1]

The CPU on the server is no longer peaking out, but is still working hard because it is processing over 28,000 requests rather than just a few thousand.

This post isn't intended to scare you off using wsHttpBinding. It has its place, but if you don't require its functionality, be aware of the performance implication. You may need to scale up your hardware if you do require wsHttpBinding on a service that will be hit heavily. My preference would be to use netTcpBinding in WAS, but unfortunately Windows Server 2008 is not available to me for this specific scenario.

[0]: /files/WindowsLiveWriter/PerformanceofwsHttpBindingvsbasicHttpBin_9E5E/WsHttpBinding_2.jpg
[1]: /files/WindowsLiveWriter/PerformanceofwsHttpBindingvsbasicHttpBin_9E5E/BasicHttpBinding_2.jpg