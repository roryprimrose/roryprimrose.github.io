---
title: SCOM event collection fails with (0x8007000E)
categories: IT Related
tags: SCOM
date: 2009-03-11 13:59:00 +10:00
---

I have written some custom SCOM management packs that read from a database and populate event collection data (see [here][0] and [here][1] for some examples from other people). My first management pack works without any problems. The second management pack (essentially a copy of the first) fails with an out of memory exception. The Operations Manager event log on the SCOM server contains an entry that starts with the following: 

> Converting data batch to XML failed with error "Not enough storage is available to complete this operation." (0x8007000E) in rule "[RuleName]" 

This has been plaguing me for weeks but I now have an answer. 

The definition of a collection event record includes a description field. The description field may be defined with a static value, a property bag (params) value or combination of both. The property bag value can be fully qualified (in the format of $Data/Property[@Name='&lt;PropertyBagItemName&gt;']$) or referenced by index. See [here][2] for more information. 

<!--more-->

My management pack originally contained the following: 

```xml
<Description>%16</Description> 
```

In the case of my second management pack, the index is incorrect. There were field differences in the database output between the two management packs such that the param index was pointing to a different field from the database. I fixed this up so that it was pointing to a "Message" property bag value populated from the database. 

```xml
<Description>$Data/Property[@Name='Message']$</Description> 
```

Now the management pack works. The difference between the two fields was that the original field (identified by index 16) had a null value whereas the "Message" field did have a value. My scripts deal with null values by ensuring they are always converted to empty strings using the following old VBA trick: 

```vbnet
Call propertyBag.AddValue("Message", rs.Fields("Message").Value & "") 
```

After a bit of playing around, I found that there isn't a problem with the property bag containing empty values, but there does seem to be an issue with the event collection properties themselves having empty values when identified by index. If the fully qualified reference to the property bag item is used and it has an empty value, there is no problem. 

Always make references using the fully qualified property bag reference in order to avoid this problem. This will also avoid problems should the structure of your property bag change. 

[0]: http://www.eggheadcafe.com/software/aspnet/30204739/generate-events-with-vbs.aspx
[1]: http://contoso.se/blog/?p=310
[2]: http://technet.microsoft.com/en-us/library/dd391802.aspx
