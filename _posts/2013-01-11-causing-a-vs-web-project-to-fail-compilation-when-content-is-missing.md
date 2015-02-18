---
title: Causing a VS Web Project to fail compilation when content is missing
tags : TFS
date: 2013-01-11 13:48:13 +10:00
---

Sometimes things go wrong. For example, when files are not on your local disk when you expect them to be. I see this pop up every now and then when working in a team environment or even as a single develop across multiple machines. Usually it is because something has not be submitted into source control. Maybe the file was never bound to source control in the first place. I have seen Visual Studio simply miss files as well.

Normally this is not a problem. The compiler will complain if a C# class file is missing because it can’t do its job. Unfortunately a Web project does not throw a compiler error when a content file is missing. Thankfully there is an easy fix.

Take a new Web project for example.![image][0]

We can simulate this issue by simply going down into the file system and renaming Site.css to another name.![image][1]

The project will now identify the missing file in Solution Explorer if you refresh the window.![image][2]

Ok, but what about a build.

    1&gt;------ Rebuild All started: Project: MvcApplication1, Configuration: Debug Any CPU ------
    1&gt;  MvcApplication1 -&gt; c:\Dev\MvcApplication1\bin\MvcApplication1.dll
    ========== Rebuild All: 1 succeeded, 0 failed, 0 skipped ==========
    
    Build Summary
    -------------
    00:00.883 - Success - Debug Any CPU - MvcApplication1\MvcApplication1.csproj
    
    Total build time: 00:00.893
    
    ========== Rebuild All: 1 succeeded or up-to-date, 0 failed, 0 skipped =========={% endhighlight %}

I see this as a problem. You can check this in and every other developer/machine will be essentially broken. Worse still, any TFS gated build will pass as well. There is therefore no way out of the box to ensure that you do not corrupt your code line.

The trick here is to modify the project file. Edit the project file and you will find the following at the bottom:

    <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
         Other similar extension points exist, see Microsoft.Common.targets.
    <Target Name=&quot;BeforeBuild&quot;&gt;
    </Target&gt;
    <Target Name=&quot;AfterBuild&quot;&gt;
    </Target&gt; --&gt;{% endhighlight %}

We want to remove the comments surrounding BeforeBuild and change it to the following:

    <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
         Other similar extension points exist, see Microsoft.Common.targets.
    <Target Name=&quot;AfterBuild&quot;&gt;
    </Target&gt; --&gt;
    <Target Name=&quot;BeforeBuild&quot;&gt;
      <ItemGroup&gt;
        <MissingContentFiles Include=&quot;@(Content)&quot; Condition=&quot;!Exists(%(Content.FullPath))&quot; /&gt;
      </ItemGroup&gt;
      <Message Text=&quot;Item: %(Content.FullPath)&quot; /&gt;
      <Message Text=&quot;Missing: %(MissingContentFiles.FullPath)&quot; /&gt;
      <Error Text=&quot;Content file '%(MissingContentFiles.FullPath)' was not found.&quot; Condition=&quot;'@(MissingContentFiles)' != ''&quot; ContinueOnError=&quot;true&quot;  /&gt;
      <Error Text=&quot;One or more content files are missing.&quot; Condition=&quot;'@(MissingContentFiles)' != ''&quot; /&gt;
    </Target&gt;{% endhighlight %}

This will output warnings for each file that is missing, then fail the compilation. This allows you to see multiple files that are missing before compilation is halted.

    1&gt;------ Build started: Project: MvcApplication1, Configuration: Debug Any CPU ------
    1&gt;C:\Dev\MvcApplication1\MvcApplication1.csproj(329,5): warning : Content file 'C:\Dev\MvcApplication1\Scripts\jquery.validate.min.js' was not found.
    1&gt;C:\Dev\MvcApplication1\MvcApplication1.csproj(329,5): warning : Content file 'C:\Dev\MvcApplication1\Content\Site.css' was not found.
    1&gt;C:\Dev\MvcApplication1\MvcApplication1.csproj(330,5): error : One or more content files are missing.
    ========== Build: 0 succeeded, 1 failed, 0 up-to-date, 0 skipped ==========
    
    Build Summary
    -------------
    00:00.649 - Failed  - Debug Any CPU - MvcApplication1\MvcApplication1.csproj
    
    Total build time: 00:00.910
    
    ========== Build: 0 succeeded or up-to-date, 1 failed, 0 skipped =========={% endhighlight %}
![image][3]

Job done.

[0]: //blogfiles/image_150.png
[1]: //blogfiles/image_151.png
[2]: //blogfiles/image_152.png
[3]: //blogfiles/image_153.png
