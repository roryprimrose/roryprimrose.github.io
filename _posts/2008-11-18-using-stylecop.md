---
title: Using StyleCop
categories : .Net, Applications
tags : Extensibility, StyleCop
date: 2008-11-18 11:45:00 +10:00
---

 SC is a great tool for keeping code style consistent. I don't agree with all of the default rules though. Rules that you don't want can be disabled using a Settings.StyleCop file. By default, each project will use its own settings file. Having to maintain a settings file for each project is very cumbersome. Thankfully, there is a better way. 

 The [Source Analysis blog][0] has a [post][1] about how settings are inherited from parent directories. This makes sharing settings between projects very easy. Here are some ideas for how to set this up and make the settings file easy to work with. 

 These instructions assume that all the projects in the solution are in child directories under the location that the solution file resides in. 

**How to manage your StyleCop settings**

 If a Settings.StyleCop file doesn't already exist, right-click on a project and select StyleCop Settings. Remove any of the rules that you don't want applied and click OK. 

 Open Windows Explorer for that project and move the Settings.StyleCop file into the parent (solution) directory. 

 Right-click on the solution in Solution Explorer and select _Add -&gt; Existing Item_. 

 Select Settings.StyleCop from the solution directory. 

![Add Existing Item][2]

 This will add a link to this file into the solution, but not specific to a project. For example: 

![Add as Solution Item][3]

 The next step is to ensure that the default application for this file in Visual Studio is the SC settings editor. 

 Right-click the Settings.StyleCop file and select _Open With_. 

![Settings Editor][4]

 Ensure that _StyleCopSettingsEditor_ is set as the default. If it isn't, select it and click _Set as Default_and click _OK_. 

 From now on, you can simply open the Settings.StyleCop file like any other file and it will open up in the settings. 

**Note:** Using this configuration, don't edit the settings for StyleCop against the project directly as these settings will not be available to the other projects in the solution. 

 This is my current settings file: [Settings.StyleCop (2.94 kb)][5]

[0]: http://blogs.msdn.com/sourceanalysis
[1]: http://blogs.msdn.com/sourceanalysis/pages/sharing-source-analysis-settings-across-projects.aspx
[2]: //files/WindowsLiveWriter/UsingStyleCop_A0CF/image_3.png
[3]: //files/WindowsLiveWriter/UsingStyleCop_A0CF/image_6.png
[4]: //files/WindowsLiveWriter/UsingStyleCop_A0CF/image_9.png
[5]: /files/2008/11/Settings.StyleCop
