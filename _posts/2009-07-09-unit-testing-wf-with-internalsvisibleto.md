---
title: Unit testing WF with InternalsVisibleTo
categories : .Net
tags : Unit Testing, WF
date: 2009-07-09 16:16:00 +10:00
---

I hit an interesting situation the other day. I had a WF library that I wanted to unit test and I had to add workflows to the unit test assembly to be able to test some of the activities. The unit test project template doesn’t support WF but this is easily fixed by [hacking the project file][0]. The assemblies were strong named and there were some internal classes that I was also testing using the InternalsVisibleTo attribute. 

All of a sudden the compiler complains that the unit test assembly isn’t signed and the InternalsVisibleTo attribute is not valid. The compiler message clearly indicates that the unit test assembly has not been signed and has null public token. The compiler error looks like this:

> _Friend access was granted to 'MyAssembly.UnitTests, PublicKey=0024000004800000940000000945000000240000525341310004000001000100E5D06B6A34E0EBE7386CA8C177B3EEDA66802357F74D8F5D419BF3623C9CE4F7EF2D9081418529E63A6B4C287C3941E1113543C7AF93E1ABE96A1511B3ED3B93F36DB193146BDC932EDB2A03C3CF511C1A798FF3130AD9ABB5044E1F67049878D3CE4686A2E3E5EEA3A098B71778CD8B73651CD5AFC320CDC4F315F7666659B5', but the output assembly is named 'MyAssembly.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'. Try adding a reference to 'MyAssembly.UnitTests, PublicKey=0024000004800000940000000945000000240000525341310004000001000100E5D06B6A34E0EBE7386CA8C177B3EEDA66802357F74D8F5D419BF3623C9CE4F7EF2D9081418529E63A6B4C287C3941E1113543C7AF93E1ABE96A1511B3ED3B93F36DB193146BDC932EDB2A03C3CF511C1A798FF3130AD9ABB5044E1F67049878D3CE4686A2E3E5EEA3A098B71778CD8B73651CD5AFC320CDC4F315F7666659B5' or changing the output assembly name to match._

I checked and rechecked the signing configuration for the unit test assembly and tried various other actions to try and get a successful compilation. Nothing would work. I figured this was some weird incompatibility with hacking the unit test assembly to also support WF (which is actually partially correct). I had to split my source and unit test projects into projects that contain WF and projects that don’t.

I just hit the same problem again today at work. After a bit of Googling I came up with [this][1] Connect issue. What it boils down to is that Microsoft don’t adequately handle signing assemblies when compiling WF assemblies such that the InternalsVisibleTo attribute fails. The workaround suggested is to the use old AssemblyKeyFile attribute and uncheck “Sign the assembly” in the project properties.

I also configure my projects to treat warnings as errors. The compiler then complained with the following error:

> _Warning as Error: Use command line option '/keyfile' or appropriate project settings instead of 'AssemblyKeyFile' [AssemblyPath]\Properties\AssemblyInfo.cs_

The AssemblyKeyFile attribute is essentially depreciated and its use throws a compiler warning. With treat warnings as errors set, this is now a compiler error. The way around this is to put a warning suppression for 1699 into the project settings. This needs to be done for all build configurations.

Project settings now look this this.![image][2]

Problem solved.

[0]: /post/2006/12/13/adding-workflows-to-a-non-wf-project.aspx
[1]: https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=338293
[2]: //files/image_1.png
