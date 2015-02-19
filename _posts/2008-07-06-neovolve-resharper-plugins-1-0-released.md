---
title: Neovolve ReSharper Plugins 1.0 released
categories : .Net, Applications, My Software
tags : Extensibility, ReSharper
date: 2008-07-06 15:42:00 +10:00
---

I have just released the [ReSharper Plugins 1.0][0] in my [NeovolveX][1] extensibility project on CodePlex. This plugin formats code as part of the ReSharper code format profiles.

The following is a copy of the [Using ReSharper Plugins 1.0][2] documentation on the [project site][1]

**Value Type Alias Formatter**

The ReSharper Plugins project currently contains a single plugin that provides C# code formatting functionality. This formatting function allows for type declarations to be formatted between their alias type and their CLR type. For example, depending of the format settings defined in a code cleanup profile, bool can be converted to Boolean or Boolean converted to bool.  
  
**Configuration**

The following displays the normal code format profile dialog.  
![ProfileSettings.jpg][3]
  
When the plugin is installed, the code format profile dialog will include an additional format option called _Type alias conversion_.  
![CustomFormatProfile.jpg][4]
  
The type conversion options are:  
* _Do not change_ - No change is made to the code
* _Convert to alias type_ - References of CLR types are converted to their alias types (Boolean to bool for example)
* _Convert from alias type_ - References of alias types are converted to their CLR types (bool to Boolean for example)

>  The _Do not change_ setting is the default value.  
  
**Type mappings**

The following are the type mappings that are supported for converting types between alias types and their CLR types.  
  
> Alias type &gt; CLR type 

> object &gt; Object 

> char &gt; Char 

> string&gt; String 

> bool&gt; Boolean 

> byte &gt; Byte 

> short &gt; Int16 

> int &gt; Int32 

> long &gt; Int64 

> ushort &gt; UInt16 

> uint &gt; UInt32 

> ulong &gt; UInt64 

> double &gt; Double 

> decimal &gt; Decimal 

> float &gt; Single 

> sbyte &gt; SByte 
  
**Code examples**

>  Code formatted to use alias types  
![AliasCode.jpg][5]
  
> Code formatted to use CLR types  
![CLRTypeCode.jpg][6]

Grab it from [here][0].

[0]: http://www.codeplex.com/NeovolveX/Release/ProjectReleases.aspx?ReleaseId=14666
[1]: http://www.codeplex.com/NeovolveX
[2]: http://www.codeplex.com/NeovolveX/Wiki/View.aspx?title=Using%20ReSharper%20Plugins%201.0
[3]: http://www.codeplex.com/Project/Download/FileDownload.aspx?ProjectName=NeovolveX&DownloadId=38352
[4]: http://www.codeplex.com/Project/Download/FileDownload.aspx?ProjectName=NeovolveX&DownloadId=38354
[5]: http://www.codeplex.com/Project/Download/FileDownload.aspx?ProjectName=NeovolveX&DownloadId=38358
[6]: http://www.codeplex.com/Project/Download/FileDownload.aspx?ProjectName=NeovolveX&DownloadId=38359
