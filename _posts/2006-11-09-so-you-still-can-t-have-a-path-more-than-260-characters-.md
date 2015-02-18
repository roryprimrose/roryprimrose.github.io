---
title: So you still can't have a path more than 260 characters???
categories : .Net, IT Related
tags : TFS
date: 2006-11-09 12:10:24 +10:00
---

I got hit with an issue yesterday when the local path of a file in TFS had a length greater than 260 characters. I couldn't run a Get Latest and the IDE also seemed to hang on opening other projects. It reminded me about a situation a couple of years ago when I hit this same issue using longer paths in the framework. 

Using reflector, I can see the following code in quite a few places in the framework.

{% highlight csharp linenos %}if (num14 &gt;= Path.MaxPath) { throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong")); }{% endhighlight %}

Path.MaxPath references a constant that has a value of 260. The exception returns this information:

_> The specified path, file name, or both are too long. The fully qualified file name must be less than 260 characters, and the directory name must be less than 248 characters._

[Wikipedia][0] has some interesting [comparisons of file systems][1] and [Microsoft] also have some [file naming information][2] on MSDN. Being a developer for the [Microsoft] platform, the main file system of concern to me&#160; is NTFS. Under NTFS, the maximum file name length of 254 characters + &quot;.&quot; and a maximum pathname length of 32,767 Unicode characters with each path component (directory or filename) up to 255 characters long. Just for interest, FAT32 is 255 bytes for the maximum filename length and apparently unlimited for the maximum pathname length.

So the file systems we typically use with .Net and Windows support long paths. Why is it that the .Net framework and TFS won't allow them? Being limited to 260 characters for the entire path seems ludicrous when the limitation seems to be each part of the path has to be a maximum of 255 characters.

[0]: http://en.wikipedia.org/wiki/Main_Page
[1]: http://en.wikipedia.org/wiki/Comparison_of_file_systems#Limits
[2]: http://msdn.microsoft.com/library/default.asp?url=/library/en-us/fileio/fs/naming_a_file.asp
