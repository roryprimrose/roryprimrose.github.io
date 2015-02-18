---
title: ComboBox SelectedText without focus
categories : .Net
date: 2007-03-18 09:28:00 +10:00
---

I have just come across something that really surprises me about the WinForms ComboBox. If the control is in DropDown style, the SelectedText, SelectionStart and SelectionLength properties are all empty if the control doesn't have focus. This is crazy! I can't read or write the selected value in the edit part of the control as a result of a button click. Surely there is a way around this
