---
title: WCF, SSL and localhost
categories : .Net
tags : WCF
date: 2008-02-28 22:05:29 +10:00
---

We encountered an interesting issue at work over the last week. We are writing WCF services and implementing transport security to communicate with IIS. For local development, we had certificates created by a certificate server and configured onto the local IIS. As you would expect, the certificates used the machine name for the common name of the certificate. This is after all the standard procedure. 

The problem encountered is that the web application project in Visual Studio stores the url of the project in the csproj file rather than the user settings file. This is the url of the application that Visual Studio will attach to in order to debug in IIS. The url has to be a reference to the local machine. As the project is under source control, the url stored as a project setting is now put onto the machines of other developers. So if I configure the project to point to my machine using https, check in the file and another developer gets it, then their version of the project now points to my machine rather than theirs. Bit of a problem.

There were two ideas that we quickly came up with:

1. Modifying the host name file on the local machine (don't know where this one was going, but it was on the table)
1. Adding a WCF hack that hooks into the [System.Net.ServicePointManager.ServerCertificateValidationCallback event][0] to ignore certificate errors when the certificate uses the machine name as the common name but the url is localhost
The first solution if it was viable was an issue because it required customisation of every developers machine. Using localhost as a name is the best direction as all machines are already configured with that host name as a loopback. The second idea was probably more viable, but was considered a potential security risk if that code landed in production.

The answer was actually quite simple. Create a certificate with the common name as localhost rather than the specific machine name. This would satisfy the csproj setting pointing to localhost which would be a valid address on all developer machines. Given that all the developer machines needed a certificate anyway, we may as well use localhost as a common name.

The first attempt at this solution actually failed. This is because we created just one localhost certificate and tried to use that on multiple developer machines. The problems seemed to be that when IIS creates a certificate request, a private key is generated such that the resulting issued certificate is only valid for that specific machine, even though localhost was the common name used. Unfortunate, but certainly not a major problem. The answer is that each developer machine needs to create their own IIS certificate request using localhost as the common name. If the url in the csproj points to that address ([https://localhost/yourproject/yourservice.svc][1] for example), then this address will work on all developer machines with their own valid certificate.

[0]: http://msdn2.microsoft.com/en-us/library/system.net.security.remotecertificatevalidationcallback(VS.80).aspx
[1]: https://localhost/yourproject/yourservice.svc
