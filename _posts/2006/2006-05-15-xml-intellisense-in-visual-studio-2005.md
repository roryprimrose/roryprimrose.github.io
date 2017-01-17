---
title: XML Intellisense in Visual Studio 2005
categories: .Net, Applications, IT Related
tags: ASP.Net, Great Tools
date: 2006-05-15 06:13:00 +10:00
---

Last week, a colleague told me how to get intellisense support in xml documents in the VS2005 IDE. It is really easy, although there are some interesting quirks in the process.

To pull this off, you first need to get an xsd file that defines your xml file. The IDE uses the xsd file to know what elements and attributes are available for a specified location in the document.

The next step is to get at the property grid for your xml file. Just selecting the file in Solution Explorer and then displaying the Properties window doesn't give you the property grid contents that you need. This property grid view just displays the generic properties for a file, rather than being more specific to the xml file type. To get at the property that needs to be changed, open the xml file and show the Properties window. You should now see that a Schema property is available.

Set the Schemas property value to the file path to your xsd file. I have found that the editor for the Schemas property is quite broken. The Add button doesn't work and also has a few other issues. It's easiest to just copy and paste the file path from explorer to the property grid rather than fighting with the editor dialog.

Enjoy.


