---
title: Refreshing an expired STSTestCert WIF certificate
categories: .Net, Applications, Personal
tags: WIF
date: 2012-01-29 23:32:45 +10:00
---

I have been using WIF for the last couple of years on a few of my projects and the STSTestCert gets a bit of a workout on my development machines. This certificate is only valid for 12 months. All the applications that use this test certificate will fail to execute authentication requests once this certificate has expired. 

Here is the easiest way to renew the certificate.

1. Open up MMC and attach the Certificate Manager plugin for the local machine.
1. Navigate to Certificates (Local Computer) -&gt; Personal -&gt; Certificates.
1. Select and delete the expired STSTestCert certificate.
1. Open VS with elevated rights
1. Add a new solution
1. Add a new STS project to that solution using the Tools -&gt; Add STS Reference… menu item
1. Continue through the wizard
1. Refresh the MMC console and you should now have a fresh STSTestCert

![image][0]

[0]: /files/image_134.png
