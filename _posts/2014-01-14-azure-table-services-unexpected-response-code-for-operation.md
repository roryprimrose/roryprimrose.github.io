---
title: Azure Table Services Unexpected response code for operation
tags : Azure
date: 2014-01-14 10:59:47 +10:00
---

I’ve just hit a StorageException with Azure Table Services that does not occur in the local emulator.

> Unexpected response code for operation : 5

The only hit on the net for this error is [here][0]. That post indicates that invalid characters are in either the PartitionKey or RowKey values. I know that this is not the case for my data set. It turns out this failure also occurs for invalid data in the fields. In my scenario I had a null value pushed into a DateTime property. The result of this is a DateTime value that will not be accepted by ATS.

[0]: http://blogs.msdn.com/b/cie/archive/2013/05/31/microsoft-windowsazure-storage-storageexception-unexpected-response-code-for-operation.aspx
