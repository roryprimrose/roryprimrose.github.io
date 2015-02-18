---
title: Executing build tasks without a build server – In action
categories : .Net, My Software
tags : TFS, WiX
date: 2011-07-06 23:02:00 +10:00
---

<p>The <a href="/post/2011/07/03/Executing-build-tasks-without-a-build-server-%E2%80%93-Implementation.aspx">previous post</a> provided the implementation of the BTE application. It described how the implementation of BTE is able to satisfy the <a href="/post/2011/07/01/Executing-build-tasks-without-a-build-server-%E2%80%93-Design.aspx">original design requirements</a> by providing an extensible task execution model.</p>
<p>The following are some examples of BTE running on the command line. I am using a batch file in order to shorten BuildTaskExecutor.exe down to bte.</p>
<p>1) Executing BTE without arguments</p>
<p><a href="//blogfiles/image%5B8%5D.png"><img style="background-image: none; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; padding-top: 0px; border-width: 0px;" title="image[8]" src="//blogfiles/image%5B8%5D_thumb.png" alt="image[8]" width="681" height="454" border="0" /></a></p>
<p>2) Executing the help task</p>
<p><a href="//blogfiles/image%5B11%5D.png"><img style="background-image: none; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; padding-top: 0px; border-width: 0px;" title="image[11]" src="//blogfiles/image%5B11%5D_thumb.png" alt="image[11]" width="681" height="454" border="0" /></a></p>
<p>3) Executing the help task for a specific task name (or alias in this case)</p>
<p><a href="//blogfiles/image%5B14%5D.png"><img style="background-image: none; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; padding-top: 0px; border-width: 0px;" title="image[14]" src="//blogfiles/image%5B14%5D_thumb.png" alt="image[14]" width="681" height="298" border="0" /></a></p>
<p>4) Executing with verbose logging</p>
<p><a href="//blogfiles/image%5B17%5D.png"><img style="background-image: none; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; padding-top: 0px; border-width: 0px;" title="image[17]" src="//blogfiles/image%5B17%5D_thumb.png" alt="image[17]" width="681" height="382" border="0" /></a></p>
<p>The first version of BTE provides the following tasks built into the application (as defined by the help output):</p>
<pre class="brush: plain;">BuildTaskExecutor BinaryOutputVersion|bov
Renames the project output to include the product version of the project output.

BuildTaskExecutor Help|/?
Displays help information about the available tasks

BuildTaskExecutor IncrementAssemblyVersion|iav
Increments version number parts in AssemblyVersionAttribute entries in code files

BuildTaskExecutor SyncWixVersion|swv
Synchronizes the product version in a Wix project to the version of a binary file.

BuildTaskExecutor TfsCheckout|tfsedit
Checks out files from TFS based on a search pattern. The checkout will use default credentials resolved for the identified files.

BuildTaskExecutor WixOutputVersion|wov
Renames the Wix project output to include the wix product version.{% endhighlight %}
<p>The next post will provide a scenario where these tasks are used to manage the version of a solution without a full TFS environment.</p>
