---
title: Creating a simple bootstrapper - Introduction
categories: .Net
tags: WiX
date: 2011-07-19 16:22:38 +10:00
---

Last month I was creating a WiX setup package to deploy a new system. In the past we have typically required the user to execute the MSI from the command line and provided all the MSI arguments required by the package. We have also played with transform packages to achieve the same result. 

I am not a fan of either of these methods and want to push configuration options into the installer UI as a way of minimising user error while maintaining flexibility. I came across the excellent [MsiExt project][0] on CodePlex which provided a wide variety of customisations for WiX including custom actions and dialogs. One of the issues I hit was that several of the validation options in the custom dialogs required elevated privileges. Validating service credentials against the domain and verifying account rights is an example of this.![image][1]

<!--more-->

The UI sequence of an MSI executes with standard user rights when it is opened directly from Windows Explorer. The package fails on custom actions in the UI sequence that require elevated rights. There are a couple of options for elevating the MSI for the UI sequence. These are:

* Execute the msi from an elevated command prompt using _msiexec /i “[PackagePath]”_
* Wrap the msi in a bootstrapper which elevates first

The first option is not clean and is really only for advanced users. The main problem here is that people who see an MSI expect that they can just run it as is. For me, the first option is not good enough. That leaves the bootstrapper option.

Bootstrappers normally chain many msi packages together, can run complex installation configurations and provide their own UI experience. My MSI packages are relatively simple compared to doing something like installing Visual Studio. I don’t need all the complexity that comes with bootstrapper packages like [DotNetInstaller][2]. Unfortunately my research did not provide any viable simple bootstrapper options.

I put together what I wanted out of a simple bootstrapper. The following are the design requirements I came up with.

* Seamless Visual Studio integration and compilation
* Bootstrapper must be completely self-contained
* Bootstrapper must be quiet (no UI) and simply execute the MSI package
* Automatically elevate the packaged MSI
* Support change/repair/remove actions on the package
* Flexible design to add/remove bootstrapper functionality

The next post will outline the proof of concept I put together to see if this was possible.

[0]: http://msiext.codeplex.com/
[1]: /files/image_123.png
[2]: http://dotnetinstaller.codeplex.com
