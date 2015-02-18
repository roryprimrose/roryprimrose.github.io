---
title: Using a Vista x64 network printer from Vista x86
categories : IT Related
date: 2008-03-17 08:40:57 +10:00
---

I have installed Vista x64 on my server which has a USB printer attached to it. I have shared this printer so that other machines on the network can use it as a network printer. I found that I could see the printer from my Vista x86 laptop, but it just wouldn't print.

The printer properties on the printer server have some advanced properties where you can specify x86 drivers for the printer for when clients add the network printer. The problem is that the printer drivers come with Vista natively. This means that the manufacturer (HP) doesn't provide the drivers for it as a download. After some research, I found that there is a really easy way around this issue. 

The rough steps are:

1. Add a new printer to your x86 machine
1. Select Local printer attached to this machine, uncheck automatic detection
1. Select Create a new port and select Local Port
1. Enter the address of the printer ([\\servername\printername][0])
1. Select the printer make and model
Easy

[0]: file://\\servername\printername
