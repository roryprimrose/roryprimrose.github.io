---
title: Bug found in calculating Code Metrics in VS2008
categories: .Net
date: 2007-12-21 09:14:54 +10:00
---

I have found that several of my VS2008 solutions are not able to calculate code metrics for a couple of their projects. The error message returned is:

> _An error occurred while calculating code metrics for target file '&lt;File path&gt;' in project &lt;ProjectA&gt;. The following error was encountered while reading module '&lt;ProjectB&gt;': Could not resolve type reference: [mscorlib]._

After consulting David Kean and Todd King at Microsoft (FxCop team), they have found the issue. Todd's response was:

> _It looks like we have a bug in metrics where if one of your project’s reference assemblies uses an attribute that lives in an assembly that is not one of your project’s other reference assemblies metrics will fail because when it goes to try to populate all the attributes on your reference assemblies because it won’t be able to find the type information for that attribute. We are tracking this bug and should have it fixed in SP1. For now the workaround is to add a few extra references to some of your projects._

I found this issue when developing WCF services. The problem scenario was because of WCF attributes in a service contract assembly being referenced by a service host where the service host didn't reference System.ServiceModel. The service host compiles, but failed to have code metrics calculated for it. A simplified repro example is ProjectA references ProjectB which references AssemblyC. ProjectA fails to calculate code metrics because it doesn't reference AssemblyC. 

There is a second bug here as well. The error message points to mscorlib as the type reference that couldn't be resolved. This is not correct and is misleading.

Todd has said that this bug will be fixed in SP1. Until then, the workaround is to look at the references in ProjectB that are not in ProjectA. Gradually add the missing references until code metrics can be calculated.


