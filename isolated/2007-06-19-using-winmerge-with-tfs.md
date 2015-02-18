---
title: Using WinMerge with TFS
categories : .Net, Applications
tags : Great Tools, ReSharper, TFS
date: 2007-06-19 21:49:00 +10:00
---

Someone at work was kind enough to figure out the correct command line switches to use in order to replace the standard TFS compare/merge tool with [WinMerge][0]. I originally blamed [Pants][1] for the info, but he then accused [Eddie][2]. Here are the goods:

In Visual Studio do the following:

* Click on **Tools** menu
* Click on **Options** menu item
* Expand **Source Control** tree item
* Select **Visual Studio Team Foundation Server** tree item
* Click on **Configure User Tools...** button

**Comparing**

To use WinMerge as the Compare/Diff tool:

* Click the **Add...** button
* For Extension, type *****
* For Operation, select **Compare**
* For Command, browse for **C:\Program Files\WinMerge\WinMerge.exe**
* For Arguments, type **/x /e /ub /wl /dl %6 /dr %7 %1 %2**
* Click **OK** to accept

**Merging**

To use WinMerge as the Merge tool:

* Click the **Add...** button
* For Extension, type *****
* For Operation, select **Merge**
* For Command, browse for **C:\Program Files\WinMerge\WinMerge.exe**
* For Arguments, type **/x /e /ub /wl /dl %6 /dr %7 %1 %2 %4**
* Click **OK** to accept

Note: You need to click on the **Save** button on the tool bar within WinMerge merge to commit a merge before exiting the screen

**Merge Error Workaround**

If you encounter an error that says "The manual merge for <filename&gt; has been canceled", the fix is to use a batch file to redirect the call to WinMerge to ensure a 0 return code.

This problem and workaround was originally found [here][3] by a colleague. He tested WinMerge and found that it does correctly return 0. Regardless of this, this batch workaround seems to work well. I have modified the original version to include my switches.

Create a batch file with the following content:

> @rem winmergeFromTFS.bat  
> @rem 2007-08-01  
> @rem File created by Paul Oliver to get Winmerge to play nicely with TFS  
> @rem  
> @rem To use, tell TFS to use this command as the merge command  
> @rem And then set this as your arguments:  
> @rem %6 %7 %1 %2 %4  
> "C:\Program Files\WinMerge\WinMerge.exe" /x /e /ub /wl /dl %1 /dr %2 %3 %4 %5  
> exit 0

The merge instructions above will then be changed to:

* Click the **Add...** button
* For Extension, type *****
* For Operation, select **Merge**
* For Command, browse for **C:\Program Files\WinMerge\winmergeFromTFS.bat**
* For Arguments, type **%6 %7 %1 %2 %4**
* Click **OK** to accept

**Modified C# Filter**

WinMerge uses filter files to filter the entries found when comparing folders. The latest version of WinMerge bundles a C# filter file. I have updated it to include additional entries. Copy the content below into C:\Program Files\WinMerge\Filters\CSharp_loose.flt.

> ## This is a directory/file filter for WinMerge  
> ## This filter suppresses various binaries found in Visual C# source trees  
> name: Visual C# loose  
> desc: Suppresses various binaries found in Visual C# source trees

> ## This is an inclusive (loose) filter  
> ## (it lets through everything not specified)def: include  
> ## Filters for filenames begin with f:  
> ## Filters for directories begin with d:  
> ## (Inline comments begin with " ##" and extend to the end of the line)  
> f: \.aps$ ## VC Binary version of resource file, for quick loading  
> f: \.bsc$ ## VC Browser database  
> f: \.dll$ ## Windows DLL  
> f: \.exe$ ## Windows executable  
> f: \.obj$ ## VC object module file  
> f: \.pdb$ ## VC program database file (debugging symbolic information)  
> f: \.res$ ## VC compiled resources file (output of RC [resource compiler])  
> f: \.suo$ ## VC options file (binary)  
> f: \.cache$ ## jQuery1520454518222482875_1352953951247  
> f: \.resource$ ## Compiled resource file.  
> f: \.xfrm ## jQuery15208427655482664704_1386642834837  
> f: \.bak$ ## backup  
> f: \.vssscc$ ## Source control metadata  
> f: \.vspscc$ ## Project source control metadata  
> f: \.user$ ## User settings  
> f: \.resharper$ ## Resharper data  
  
> d: \\bin$ ## Build directory  
> d: \\obj$ ## Object directory  
> d: \\cvs$ ## CVS control directory  
> d: \\.svn$ ## Subversion control directory

Updated:

After checking out [James Manning's blog post][4] about compare/merge tools (thanks [Grant][5]), I have updated the command line arguments. For the most part, it is a merge of the switches in this original post and James' switches. The /x /e switches give the tool the same behaviour as the TFS tool for using the ESC key and identifying identical files. James' has include the use of the /ub /dl /dr switches to avoid MRU persistence and adds version labels to the left and right sides. I have also added /wl so that the left (server) side file is read-only.

If you want to know more about the WinMerge switches, you can get the online help from [here][6].

**Updated:**

As suggested by [Scott Munro][7], I have removed %3 from the merge switches, but I have still retained the /wl switch.

**Updated:**

Added TFS error workaround and my version of the C# filter.

**Updated:**

Added a reg file that contains the original settings outlined above (does not include workaround).

[WinMerge for TFS.reg (1.12 kb)][8]

**Updated:**

Added reg files for VS2010 x86 and x64

[WinMerge_for_TFS_VS2010_x86.reg (1.12 kb)][9]

[WinMerge_for_TFS_VS2010_x64.reg (1.14 kb][10]

**Updated:**

Added reg files for VS2012 x86 and x64

[WinMerge_for_TFS_VS2012_x64.reg (1.14 kb)][11]

[WinMerge_for_TFS_VS2012_x86.reg (1.12 kb)][12]

**Updated:**

Added reg files for VS2013 x86 and x64

[WinMerge_for_TFS_VS2013_x64.reg (1.14 kb)][13]

[WinMerge_for_TFS_VS2013_x86.reg (1.12 kb)][14]

[0]: http://www.winmerge.org/
[1]: http://withpantscomesdignity.blogspot.com/
[2]: http://eddiedebear.blogspot.com/
[3]: http://forums.microsoft.com/MSDN/ShowPost.aspx?PostID=1679236&amp;SiteID=1
[4]: http://blogs.msdn.com/jmanning/articles/535573.aspx
[5]: http://www.holliday.com.au/
[6]: http://www.winmerge.org/2.6/manual/CommandLine.html
[7]: http://developers.de/blogs/scott_munro/archive/2007/08/09/update-to-winmerge-with-tfs.aspx
[8]: /blogfiles/2009%2f6%2fWinMerge+for+TFS.reg
[9]: /blogfiles/2010%2f5%2fWinMerge_for_TFS_VS2010_x86.reg
[10]: /blogfiles/2010%2f5%2fWinMerge_for_TFS_VS2010_x64.reg
[11]: /blogfiles/2012%2f11%2fWinMerge_for_TFS_VS2012_x64.reg
[12]: /blogfiles/2012%2f11%2fWinMerge_for_TFS_VS2012_x86.reg
[13]: /blogfiles/2013%2f11%2fWinMerge_for_TFS_VS2013_x64.reg
[14]: /blogfiles/2013%2f11%2fWinMerge_for_TFS_VS2013_x86.reg
