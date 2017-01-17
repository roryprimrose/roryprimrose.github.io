---
title: Executing build tasks without a build server – Design
categories: .Net, Personal
tags: MEF, Sandcastle, TFS, WiX
date: 2011-07-01 13:15:00 +10:00
---

At work we run TFS2010 with build controllers, build agents and lab management. I have customised the build workflow so that we can get a lot more functionality out of the automated build process. Some of these customisations are things like:

* Finding the product version number
* Incrementing the product version number
* Updating the build name with the new product version
* Updating files prior to compilation
* Updating files after compilation
* Building Sandcastle documentation
* Deploying MSIs to remove servers
* Filtering drop folder output (no source, just build output like configuration files, documentation and MSIs)

<!--more-->

I use Codeplex at home for my personal projects. This means that I don't get all the goodness that comes with TFSBuild in 2010. I still want the automatic version management functionality listed above however. I have the following functionality requirements to make this happen:

* Determine the current product version number
* Increment the product version number 
  * Must be done prior to any project compilation
* Sync Wix project versioning with product version number 
  * Needs to happen before wix project compilation
  * Needs to cater for wix variables being used for version information
* Allow TFS checkout for files under source control 
  * This is important so that incremented version numbers continue to increment from previous version under source control
  * Must also cater for when solution is loaded without TFS availability (broken source control bindings etc)
* Push product/wix version into wix output name
* Failure to execute these actions successful will fail the solution/project build
* No installation required
* No configuration required

  * All customisation of tasks is done using command line arguments

* Extensible using MEF

  * Again, no configuration required to add new tasks

The next post will outline how I was able to make this happen using an extensible MEF task based application.


