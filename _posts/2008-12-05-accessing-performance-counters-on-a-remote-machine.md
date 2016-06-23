---
title: Accessing performance counters on a remote machine
categories: .Net
tags: Performance, WCF
date: 2008-12-05 14:40:00 +10:00
---

 I just encountered a curly situation with performance counters. I have added performance counters to a WCF service which has been deployed out to a host platform. When I fire up perfmon.exe on my local machine, the counter category isn't in the list of categories when I specify the remote machine. 

 All the research on the net seems to point towards a permissions problem. I am an administrator on the server however so this isn't the problem. I can also see other performance categories and counters for that machine, but not the ones I have just installed. 

 The answer to this one is unexpected. A restart of the Remote Registry service on the server is required. It seems that the remote registry service uses some kind of internal cache of the registry. After restarting that service, the performance counters I'm after are now available to my local machine. 


