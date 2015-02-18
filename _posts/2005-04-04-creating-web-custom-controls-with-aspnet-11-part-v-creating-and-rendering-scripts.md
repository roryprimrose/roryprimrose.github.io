---
title: Creating Web Custom Controls With ASP.Net 1.1 - Part V - Creating and Rendering Scripts
date: 2005-04-04 05:53:00 +10:00
---

When I create Web Custom Controls, I usually have some JavaScript that needs to be rendered with them. There are several methods of building and rendering scripts for controls and some of these methods are definitely better than others. 

I have seen scripts developed as hard-coded strings in a controls code too many times. Sometimes when I see these hard-coded strings, the developers are at least using a StringBuilder object. StringBuilders make large amounts of string concatenations very quick. They also tend to cause the developer to construct the string contents in a line by line fashion. Scripts built as hard-coded strings are not usually very large so any performance benefit gained from using a StringBuilder would be negligible, but better formatting of the code from a line by line style is easier to read. 

Regardless of whether a StringBuilder is used or not, building up a string is clumsy as it makes the script really difficult to develop and even harder to maintain. A better way of developing a script for a control is by adding a script file to the project and rendering the contents of the file to the browser. In doing this, developing and maintaining the script will be easier as you will be editing the code just like any other text based file.

No matter how to choose to build your scripts for a control, you need to render them at some point. So where is the best place to render these scripts? 

Most people will register their scripts with the Page object either by calling RegisterClientScriptBlock or RegisterStartUpScript which will get the page to render the script contents for the control. The RegisterClientScriptBlock method will cause the page to render the script just after the opening FORM tag while the RegisterStartUpScript method will cause the page to render the script just before the closing FORM tag. Without registering a script with the page, the only other way of rendering it is when the control itself renders. 

The benefit of registering scripts with the page is that the registration is done using a key value. You may use this key value to ensure that a script is registered only for the first instance of the control on the page, or for each instance. If a script only needs to be rendered once, calls should be made to Page.IsClientScriptBlockRegistered or Page.IsStartUpScriptRegistered to determine if the script identified by the key value has already been registered with the page by another instance of the control. This will avoid duplicate scripts from being registered. 

Rendering scripts in the controls rendering process means that the script will be rendered for each instance of the control on the page. There may be times when this script duplication is quite valid, however this is not as neat and maintainable as registering them with the page using unique key values. More importantly, if the script should only be rendered once in the page, this is not possible if the script is rendered by the control. This is because as the control renders itself, it has no knowledge that another instance of the control has already rendered the script to the page. Registering the scripts with the page gives you the opportunity to control when and how often the controls script will be rendered. 

Where I want the script to render once for all instances of a particular control, I normally use a key value that includes the namespace and object name of the control with a _Purpose suffix, such as "MyDev_Web_Controls_ImgButton_Behaviours". Where I want a script to be registered for each instance of the control on the page, I include the ClientID value of the control in the key value, such as "MyDev_Web_Controls_ImgButton_InitButton_" & ClientID. 

The later case has issues with it in that usually such a script will need to be able to identify the specific control instance on the client. Using the controls ClientID property value in the script is the answer here. This means that a dynamic script is required so that the ClientID value can be used, making the idea of developing the script in a file a little different. To get this to work, the script would first need to be loaded from the file, then have string manipulations applied to it to substitute in the controls ClientID property value before it is rendered. I am not a fan of this method and do my best to avoid such a design.

The most appropriate time to register a script with a page is in the controls OnPreRender event. It is at this event that all properties of the control have been set and the control is about to render itself. Rendering scripts before OnPreRender might cause issues as some property values might influence what scripts are rendered or how they are rendered. Registering the scripts after OnPreRender, such as in the Render process, is not going to work well either because at that point the page has already rendered scripts registered with calls to RegisterClientScriptBlock. 

To summaries, I always register scripts in the controls OnPreRender event by calling Page.RegisterClientScriptBlock or Page.RegisterStartUpScript. Depending on the nature of the script, the key values I use for the registration ensure that the scripts are rendered once on the page, or for each instance of the control on the page.


