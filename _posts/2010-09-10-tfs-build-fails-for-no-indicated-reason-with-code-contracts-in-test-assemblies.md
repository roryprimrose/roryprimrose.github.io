---
title: TFS Build fails for no indicated reason with code contracts in test assemblies
categories : .Net
tags : TFS
date: 2010-09-10 17:02:08 +10:00
---

This has been a curly one for a few months and I’ve finally had some time to resolve the issue. My team has been running TFS Build 2010 with gated check-ins where the build does MSI deploys then runs unit and integration tests.  

All of a sudden the builds started failing with no indication as to why. The build activity log just stops and does not contain errors. The MS build log file also does not contain any errors. The event log on the build server does shed some light on the situation however.  

{% highlight text %}
(MSTest.exe, PID 1868, Thread 1) Exception thrown when enumerating assembly: System.IO.FileLoadException: Could not load file or assembly 'MyProject.Services.Business.UnitTests.Contracts, Version=1.0.449.0, Culture=neutral, PublicKeyToken=0e60eebea588ffe8' or one of its dependencies. Strong name validation failed. (Exception from HRESULT: 0x8013141A)
File name: 'MyProject.Services.Business.UnitTests.Contracts, Version=1.0.449.0, Culture=neutral, PublicKeyToken=0e60eebea588ffe8' ---> System.Security.SecurityException: Strong name validation failed. (Exception from HRESULT: 0x8013141A)

The Zone of the assembly that failed was:
MyComputer
   at System.Reflection.RuntimeAssembly._nLoad(AssemblyName fileName, String codeBase, Evidence assemblySecurity, RuntimeAssembly locationHint, StackCrawlMark& stackMark, Boolean throwOnFileNotFound, Boolean forIntrospection, Boolean suppressSecurityChecks)
   at System.Reflection.RuntimeAssembly.nLoad(AssemblyName fileName, String codeBase, Evidence assemblySecurity, RuntimeAssembly locationHint, StackCrawlMark& stackMark, Boolean throwOnFileNotFound, Boolean forIntrospection, Boolean suppressSecurityChecks)
   at System.Reflection.RuntimeAssembly.InternalLoadAssemblyName(AssemblyName assemblyRef, Evidence assemblySecurity, StackCrawlMark& stackMark, Boolean forIntrospection, Boolean suppressSecurityChecks)
   at System.Reflection.RuntimeAssembly.InternalLoadFrom(String assemblyFile, Evidence securityEvidence, Byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm, Boolean forIntrospection, Boolean suppressSecurityChecks, StackCrawlMark& stackMark)
   at System.Reflection.Assembly.LoadFrom(String assemblyFile)
   at Microsoft.VisualStudio.TestTools.TestTypes.Unit.AssemblyEnumerator.EnumerateAssembly(IWarningHandler warningHandler, String location, ProjectData projectData, ObjectHandle assemblyResolverWrapper)
   at Microsoft.VisualStudio.TestTools.TestTypes.Unit.AssemblyEnumerator.EnumerateAssembly(IWarningHandler warningHandler, String location, ProjectData projectData, ObjectHandle assemblyResolverWrapper)
   at Microsoft.VisualStudio.TestTools.TestTypes.Unit.UnitTestAttributeEnumerator.Read(ITestTypeExtensionClientSidesProvider provider, IWarningHandler warningHandler, String assemblyFileName, ProjectData projectData, TestRunConfiguration testRunConfiguration)
{% endhighlight %}

This didn't make any sense. The initial workaround was to determine whether contracts where actually being used in the assembly for which the contract assembly is generated. Contracts where not being used in the first assembly that failed in this way so I disabled the contract assembly and submitted a new build. The next build fell over with the same problem on a different assembly.

I looked into the testsettings file configured for the build to see if code coverage was enabled as this also changes the assembly and re-signs them. This wasn’t the case, code coverage was disabled. I also tested the build with no testsettings configured on the build definition.

A quick search came up with [this forum entry][1] about code contracts and strong name verification failures. The Microsoft response is that code contract assemblies are not loaded. This quickly lead me to realise where the problem is. Something is loading the code contract assembly when it shouldn’t be. The most likely source of the problem is the build configuration where it identifies test assemblies to run.

The default configuration for new build definitions is to resolve test assemblies with the file spec of \*\*\\*test\*.dll. This file spec does a recursive directory search for any dll that contains the word test.

The naming of my test projects is like the following:

* *.UnitTests.dll
* *.IntegrationTests.dll
* *.LoadTests.dll

This allows for build definitions to run specific types of tests by searching against these types of file masks. The default build definition will pick up all of these.

The strong name verification issues comes into play when there is a test assembly that compiles a code contract assembly. The default build definition file mask will pick up these contract assemblies as well because 

1. the file spec is recursive and so the CodeContracts directory is searched, and
1. the contract assembly will be named *.UnitTests.Contracts.dll

So the build definition file spec is finding a match on the contract assemblies and then asks mstest.exe to run any tests found in them. This is where the contract assembly is getting loaded and then strong name verification fails.

The simple fix is to update the test assembly file spec to **\*\*\\*Tests.dll** to ensure that filenames like *\CodeContracts\*.UnitTests.Contracts.dll do not get picked up and loaded.

[0]: http://social.msdn.microsoft.com/Forums/en-US/codecontracts/thread/8cfd66b3-007f-45a7-9267-f45579c6401c