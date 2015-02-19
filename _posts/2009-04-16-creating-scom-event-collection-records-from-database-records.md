---
title: Creating SCOM event collection records from database records
categories : .Net
tags : SCOM
date: 2009-04-16 16:52:00 +10:00
---

SCOM supports collecting event records that can be reported on. It has inbuilt support for reading the Windows Event Log or a CSV file, but becomes limited beyond that. I played with the idea of getting SCOM to read records out from a database in order to populate these SCOM events. Unfortunately there is not much information on the net about how to get it to work.

The attached SCOM management pack shows an example of how to get this to work. It uses a Microsoft.Windows.TimedScript.EventProvider to read database records into a PropertyBag and to define how the database fields map to SCOM fields. A rule data source then points to the EventProvider and provides the configuration options for hitting the database.

A word of caution though, you need to be careful not to dump too much information into the SCOM data warehouse as it is not designed for collecting large amounts of event records in this manner.

[Neovolve.ScomEvents.xml (17.32 kb)][0]

[0]: /files/2009%2f4%2fNeovolve.ScomEvents.xml
