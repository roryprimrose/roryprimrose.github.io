---
title: ASP.Net Virtual Theme Provider 1.0
date: 2006-06-29 14:40:00 +10:00
---

 ASP.Net 2.0 is bundled with some great technology, especially what is available through the provider model. One of the new providers in 2.0 is the [VirtualPathProvider][0]. [Scott Guthrie][1] put out [some information][2] about this provider, as has [David Ebbo][3] in [this post][4]. 
 My VirtualThemeProvider uses the ASP.Net VirtualPathProvider to provide flexible theme support for ASP.Net projects. There were two primary objectives for this provider. Firstly, I wanted to support the ability to filter directories and files that are available in a theme directory for a given named value. Secondly, I wanted to support a global theme concept that included the ability to merge the theme directory with the global directory. 

 Take the example of a web project that has mutliple themes that are assigned depending on the type of user. Each theme has a subset of styles that display that theme according to different accessibility requirements (such as vision impared users). 

<!--more-->

 By using this provider, defining filter sets will allow the application to include or exclude items from the theme directories when processing a request. In this example, the style of the site that is for full vision users, would exclude the vision impaired files within that theme. The style of site that is for the vision impaired users would exclude the files in the theme for the full vision users. You may also have a text only version of the site, which could be acheived by filtering all css files from the theme directories. 

 This example project may also define a set of common property values assigned to controls through a skin file. This is useful because those properties would only need to be defined in a skin file in the theme directory rather than against each instance of the control in all the pages. What if you want those same skin definitions applied to all themes? Currently, this is not possible without having to copy the same skin file across all theme directories. By allowing a global directory to be merged with the theme directory, the global skin file for the application can be shared with all themes. 

 Combining these two objectives into one solution provides a very powerful and flexible theme solution. 

 In order to use the VirtualThemeProvider, do the following: 

1. Download the code (link below) and compile the Neovolve.VirtualThemeProvider assembly.
1. Reference the assembly in your web project.
1. Move your theme directories into the new application theme directory. By default, the path of the application theme directory will be "Themes/". This name can be configured through application settings in web.config by providing a value to the key "VirtualThemePathProviderThemePath". Keep in mind that App_Themes still needs to exist.
1. Move the common theme files to the global theme directory. By default, the global theme directory name is "Global". This name can be configured through application settings in web.config providing a value to the key "VirtualThemePathProviderGlobalThemeName".
1. Include a VirtualThemePathFilters.config file in each directory that requires filtering. The definitions of each of thiese files will only be applied to the directories and files in that physical directory.

 An example web application including filter set defintion configuration files is included with the full source in the download link below. The source for the VirtualThemeProvider contains full xml code documentation which has been compiled into an assembly help file. 

 Please direct any questions, suggestions or feedback to the forum. 

 Download: [Neovolve.VirtualThemeProvider 1.0.zip (275.96 kb)][5]

[0]: http://msdn2.microsoft.com/en-us/library/bhesyfec.aspx
[1]: http://weblogs.asp.net/scottgu
[2]: http://weblogs.asp.net/scottgu/archive/2005/11/27/431650.aspx
[3]: http://blogs.msdn.com/davidebb
[4]: http://blogs.msdn.com/davidebb/archive/2005/11/27/497339.aspx
[5]: /files/2008/9/Neovolve.VirtualThemeProvider%201.0.zip
