---
title: Working with unmanaged and IDisposable resources in WF
categories: .Net
tags: WF
date: 2011-03-09 15:18:00 +10:00
---

Generally speaking you will want to steer clear of unmanaged or IDisposable resources when authoring WF workflows. The primary reasons for this are:

* Persistence
* Restoration after persistence
* Error handling

A workflow may be persisted at any time (with the exception of a [No Persist Zone][0]). There are two problems for unmanaged or IDisposable resources with regard to persistence. Firstly, the resource may not be released when the workflow is persisted. Secondly the state of the resource when the workflow is restored is unlikely to be the same as when the workflow was persisted. There is no native support in WF for dealing with these two issues.

<!--more-->

A workflow may encounter an exception at any time. The workflow needs to handle this scenario at the appropriate scope to ensure that the resource is released correctly. There is an additional concern here in that disposing an IDisposable instance may throw an ObjectDisposedException. This should be caught and ignored as there is nothing that can be done in such a scenario and the desired outcome has been achieved anyway. While not a runtime concern, addressing the exception handling issue in the WF designer results in a very messy implementation.

Consider the following example.![image][1]

This workflow creates a disposable resource and holds it in a workflow variable called Reader. The workflow makes this resource available to the child activities and then disposes it when the activities are finished. This workflow definition cannot handle the persistence and restoration issues outlined above. It can however make a best effort at error handling. The above activity creates the Stream and then uses a TryCatch around the usage of the resource.![image][2]

The first TryCatch surrounds the resource usage and attempts to dispose the resource in the Finally section. In order to handle an ObjectDisposedException, a second TryCatch is required in the Finally of the first TryCatch.![image][3]

The Catch section of the second TryCatch catches ObjectDisposedException and simply ignores it.

Overall this is a messy and incomplete solution. The next post will address this using a new custom WF activity.

[0]: http://msmvps.com/blogs/theproblemsolver/archive/2010/08/22/workflows-and-no-persist-zones.aspx
[1]: /files/image_81.png
[2]: /files/image_82.png
[3]: /files/image_83.png
