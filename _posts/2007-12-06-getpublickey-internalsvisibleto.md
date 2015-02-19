---
title: GetPublicKey - InternalsVisibleTo
categories : .Net, Applications, My Software
tags : GetPublicKey, Great Tools, Unit Testing
date: 2007-12-06 12:50:00 +10:00
---

 I [posted previously][0] about using the InternalsVisibleTo attribute for unit testing and how I had come across [David Kean's][1] very helpful [PublicKey][2] application. I have been using this application for the last month or so and it has been great, until yesterday. 

 I changed the snk file used by my solution. This caused an interesting Catch-22 situation. AssemblyA couldn't compile because it had an InternalsVisibleTo attribute pointing to AssemblyATest, which now has the wrong PublicKey value. AssemblyATest couldn't compile because it directly references AssemblyA in order to run the tests. 

 Unfortunately, David's PublicKey application works from binaries alone. Because I can't compile the assemblies, I can't regenerate the InternalsVisibleTo attribute with the correct PublicKey value. 

 Now there are several ways around this, but I couldn't resist coding my own utility to cover this scenario. Using David's application as inspiration, I have created GetPublicKey. It will identity the PublicKey value from dll, exe, snk and pub files in order to generate an InternalsVisibleTo attribute. 

 GetPublicKey looks at command line arguments so that you can send any of the supported file types to it using Explorer, VS External Tools or VS Open With. It leverages sn.exe to extract the public key information so you may need to install the SDK. 

 I have also found GetPublicKey very helpful to get the PublicKeyToken values (found in the log data in the UI). I often use this for configuration values that define assembly qualified names of types that are in strong named assemblies. 

 Download: [GetPublicKey.exe (127.50 kb)][3]

[0]: /archive/2007/10/04/getting-the-publickey-for-internalsvisibleto.aspx
[1]: http://davidkean.net/
[2]: http://davidkean.net/archive/2005/10/06/1183.aspx
[3]: /files/2008%2f9%2fGetPublicKey.exe
