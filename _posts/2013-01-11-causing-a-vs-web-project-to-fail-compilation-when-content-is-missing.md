---
title: Causing a VS Web Project to fail compilation when content is missing
tags : TFS
date: 2013-01-11 13:48:13 +10:00
---

Sometimes things go wrong. For example, when files are not on your local disk when you expect them to be. I see this pop up every now and then when working in a team environment or even as a single develop across multiple machines. Usually it is because something has not be submitted into source control. Maybe the file was never bound to source control in the first place. I have seen Visual Studio simply miss files as well.

Normally this is not a problem. The compiler will complain if a C# class file is missing because it can’t do its job. Unfortunately a Web project does not throw a compiler error when a content file is missing. Thankfully there is an easy fix.

<!--more-->

Take a new Web project for example.

![][0]

We can simulate this issue by simply going down into the file system and renaming Site.css to another name.

![][1]

The project will now identify the missing file in Solution Explorer if you refresh the window.

![][2]

Ok, but what about a build.

{% highlight text %}
1>------ Rebuild All started: Project: MvcApplication1, Configuration: Debug Any CPU ------
1>  MvcApplication1 -> c:\Dev\MvcApplication1\bin\MvcApplication1.dll
========== Rebuild All: 1 succeeded, 0 failed, 0 skipped ==========

Build Summary
-------------
00:00.883 - Success - Debug Any CPU - MvcApplication1\MvcApplication1.csproj

Total build time: 00:00.893

========== Rebuild All: 1 succeeded or up-to-date, 0 failed, 0 skipped ==========
{% endhighlight %}

I see this as a problem. You can check this in and every other developer/machine will be essentially broken. Worse still, any TFS gated build will pass as well. There is therefore no way out of the box to ensure that you do not corrupt your code line.

The trick here is to modify the project file. Edit the project file and you will find the following at the bottom:

{% highlight xml %}
<!-- To modify your build process, add your task inside one of the targets below and uncomment it.
     Other similar extension points exist, see Microsoft.Common.targets.
<Target Name="BeforeBuild">
</Target>
<Target Name="AfterBuild">
</Target> -->
{% endhighlight %}

We want to remove the comments surrounding BeforeBuild and change it to the following:

{% highlight xml %}
<!-- To modify your build process, add your task inside one of the targets below and uncomment it.
     Other similar extension points exist, see Microsoft.Common.targets.
<Target Name="AfterBuild">
</Target> -->
<target name="BeforeBuild">
    <itemgroup>
        <missingcontentfiles include="@(Content)" condition="!Exists(%(Content.FullPath))" />
    </itemgroup>
    <message text="Item: %(Content.FullPath)" />
    <message text="Missing: %(MissingContentFiles.FullPath)" />
    <error text="Content file '%(MissingContentFiles.FullPath)' was not found." condition="'@(MissingContentFiles)' != ''" continueonerror="true" />
    <error text="One or more content files are missing." condition="'@(MissingContentFiles)' != ''" />
</target>
{% endhighlight %}

This will output warnings for each file that is missing, then fail the compilation. This allows you to see multiple files that are missing before compilation is halted.

{% highlight text %}
1>------ Build started: Project: MvcApplication1, Configuration: Debug Any CPU ------
1>C:\Dev\MvcApplication1\MvcApplication1.csproj(329,5): warning : Content file 'C:\Dev\MvcApplication1\Scripts\jquery.validate.min.js' was not found.
1>C:\Dev\MvcApplication1\MvcApplication1.csproj(329,5): warning : Content file 'C:\Dev\MvcApplication1\Content\Site.css' was not found.
1>C:\Dev\MvcApplication1\MvcApplication1.csproj(330,5): error : One or more content files are missing.
========== Build: 0 succeeded, 1 failed, 0 up-to-date, 0 skipped ==========
Build Summary
-------------
00:00.649 - Failed  - Debug Any CPU - MvcApplication1\MvcApplication1.csproj
Total build time: 00:00.910
========== Build: 0 succeeded or up-to-date, 1 failed, 0 skipped ==========
{% endhighlight %}

![][3]

Job done.
[0]: /files/image_150.png
[1]: /files/image_151.png
[2]: /files/image_152.png
[3]: /files/image_153.png
