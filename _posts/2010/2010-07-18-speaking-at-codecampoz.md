---
title: Speaking at CodeCampOz
categories: .Net, Applications, IT Related
tags: ASP.Net, WCF
date: 2010-07-18 09:19:40 +10:00
---

Mitch has just posted the [agenda][0] for CodeCampOz that is running in November. Looks like it will be a really good mix of information being presented this year. 

I’ll be running a session on Windows Identity Framework and how to use it without federation. Here is the abstract for my session.

> **Not a WIF of federation**

> The Windows Identify Framework (WIF) provides the latest Microsoft implementation for working in the claims-based identity space. WIF has particular strengths in providing federated security for systems that target users across multiple security domains, multiple credential types and multiple credential stores. 

> Unfortunately the available WIF documentation and samples almost completely deal with federated security scenarios. The information provided continues to use federated security architectures (Security Token Services, Issuing Authorities, Relying Parties etc.) even when federation is not used.

> Developers of small systems may find it difficult to understand how WIF fits into their system designs. Small systems in this context tend to have their own security store, do not cross security domains and may not even run within an Active Directory managed domain. 

> There are clear benefits with using claims based security in both large and small systems. How do developers leverage claims-based security without being tied to federated security architectures? 

> This session will briefly cover the benefits of claims-based security and then look at how to implement WIF in ASP.Net and WCF applications without federation dependencies.

[0]: http://codecampoz.com/agenda/
