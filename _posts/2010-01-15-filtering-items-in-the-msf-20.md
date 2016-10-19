---
title: Filtering items in the MSF 2.0
categories: .Net
tags: Sync Framework
date: 2010-01-15 16:56:00 +10:00
---

I’ve been playing with the MSF over the last couple of years. For too long I have been dabbling in a services based project that synchronises data between a set of clients. The system has been through several designs starting with hand-crafted change tracking which was really tricky. The next version used MSF on the client manage this process. This didn’t have the best result as there was no central replica for the data held by the service. The third design used a proxy provider so that central metadata information was held by the service. The fourth and latest design completely pushes MSF into the service.

The latest design allows clients to simply work with services and not have to have any understanding of MSF. There are a few hurdles with this design however. The provider implementation on the server needs to implement a preview sync so it can tell the client what changes needs to happen without doing them at that time. When a change does happens, the client will only action a single change at a time each of which must operate within a sync session in the service. This means that the sync provider also needs to work with a filtered sync session.

<!--more-->

I created a POC project to prove that I could actually achieve these features with MSF before I invested any more time in the latest design. The POC aims to sync a data item that looks like the following.    

```csharp
using System;
    
namespace CachedSyncPOC
{
    public class ItemData
    {
        public ItemData()
        {
            Id = Guid.NewGuid().ToString();
            Data = Guid.NewGuid().ToString();
        }
    
        public String Id
        {
            get;
            set;
        }
    
        public String Data
        {
            get;
            set;
        }
    }
}
```

The code aims to store both the Id and the Data of each item in the metadata store of each replica.

## Preview mode

This was actually easy to implement and is done in two parts.

The first part is that the provider notifies any interested parties of changes found using a custom event raised in GetChangeBatch.    

```csharp
public override ChangeBatch GetChangeBatch(
    UInt32 batchSize, SyncKnowledge destinationKnowledge, out Object changeDataRetriever)
{
    ChangeBatch batch = Metadata.GetChangeBatch(batchSize, destinationKnowledge); 
    
    IList changes = new List(batch.Count());
    ItemDataRetriever retriever = new ItemDataRetriever(Metadata);
    
    foreach (ItemChange change in batch)
    {
        changes.Add(retriever.LoadFromSyncId(change.ItemId));
    }
    
    OnChangesFound(
        new ChangesFoundEventArgs
        {
            Changes = changes,
            ReplicaId = ReplicaId.GetGuidId()
        });
    
    changeDataRetriever = retriever;
    
    return batch;
}
```

The second part is that the ProcessChangeBatch simply ignores any changes when in preview mode.    

```csharp
public override void ProcessChangeBatch(
    ConflictResolutionPolicy resolutionPolicy, 
    ChangeBatch sourceChanges, 
    Object changeDataRetriever, 
    SyncCallbacks syncCallbacks, 
    SyncSessionStatistics sessionStatistics)
{
    if (IsPreview)
    {
        return;
    }
    
    // Use a NotifyingChangeApplier object to process the changes. 
    // This object is passed as the INotifyingChangeApplierTarget
    // object that will be called to apply changes to the item store.
    NotifyingChangeApplier changeApplier = new NotifyingChangeApplier(IdFormats);
    INotifyingChangeApplierTarget2 applier = new ItemDataChangeApplier(this, Metadata, Filter);
    
    changeApplier.ApplyChanges(
        resolutionPolicy, 
        Configuration.CollisionConflictResolutionPolicy, 
        sourceChanges, 
        (IChangeDataRetriever)changeDataRetriever, 
        Metadata.GetKnowledge(), 
        Metadata.GetForgottenKnowledge(), 
        applier, 
        null, 
        SessionContext, 
        syncCallbacks);
}
```

This example is a little simplistic in that the event does not indicate what action is going to be taken for each item but that should be easily implemented down the track.

## Filtering

The MSF team has [provided many examples][0] on how to use the framework. My initial reaction to the [custom filtering sample][1] was one of complete dread. My filtering requirements are simple and the sample code provided is very complex. I [posted a question][2] on the MSF forum to seek some advice. While the advice was good, it unfortunately pushed me back into the provided filter sample code.

As I started to look through the code, I realised that there were a couple of different types of filtering being demonstrated. I came across a MSDN document about MSF filtering ([here][3]) and found that what I needed wasn’t actually that complex. I need to filter an item in a session rather than filter a change unit or implement full custom filtering. It is the latter two that are demonstrated in the sync filtering sample code.

You need to update the provider implementation and provide a filter type in order to filter an item in a sync session. The filter type in my example works with the Id property of the filter item and provides logic for comparing filters between providers.    

```csharp
using System;
using Microsoft.Synchronization;
    
namespace CachedSyncPOC
{
    public class ItemDataFilter : ISyncFilter
    {
        public ItemDataFilter(String id)
        {
            if (String.IsNullOrEmpty(id))
            {
                const String IdParameterName = "id";
    
                throw new ArgumentNullException(IdParameterName);
            }
    
            Id = id;
        }
    
        public Boolean IsIdentical(ISyncFilter otherFilter)
        {
            ItemDataFilter itemFilter = otherFilter as ItemDataFilter;
    
            return itemFilter != null && Id.Equals(itemFilter.Id);
        }
    
        public Byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    
        public String Id
        {
            get;
            set;
        }
    }
}
```

The provider needs to support a couple of filter interfaces. I need to implement both interfaces as I intend on using the same provider as both source and destination provider.    

```csharp
internal class CustomProvider : KnowledgeSyncProvider, ISupportFilteredSync, IRequestFilteredSync, IDisposable
{
    public void SpecifyFilter(FilterRequestCallback filterRequest)
    {
        if (Filter != null)
        {
            if (!filterRequest(Filter, FilteringType.CurrentItemsOnly))
            {
                throw new Exception("Filter not accepted at source");
            }
        }
    }
    
    public Boolean TryAddFilter(Object filter, FilteringType filteringType)
    {
        ISyncFilter syncFilter = filter as ISyncFilter;
    
        if (syncFilter == null)
        {
            return false;
        }
    
        return true;
    }
    
    // Rest of class removed for brevity
    
}
```

The next change is the GetChangeBatch method needs to deal with the filter. The ChangeBatch returned to the other provider should only contain changes related to the filter. This was the bit I was dreading in the filter process, but the MSF makes this really easy for item filtering. The GetFilteredChangeBatch method takes a delegate that determines whether items should be in the filtered change batch or not.    

```csharp
public override ChangeBatch GetChangeBatch(
    UInt32 batchSize, SyncKnowledge destinationKnowledge, out Object changeDataRetriever)
{
    ChangeBatch batch;
    
    if (Filter != null)
    {
        FilterInfo filterInfo = new ItemListFilterInfo(IdFormats);
    
        batch = Metadata.GetFilteredChangeBatch(batchSize, destinationKnowledge, filterInfo, ItemFilterCallback);
    }
    else
    {
        batch = Metadata.GetChangeBatch(batchSize, destinationKnowledge);
    }
    
    IList changes = new List(batch.Count());
    ItemDataRetriever retriever = new ItemDataRetriever(Metadata);
    
    foreach (ItemChange change in batch)
    {
        changes.Add(retriever.LoadFromSyncId(change.ItemId));
    }
    
    OnChangesFound(
        new ChangesFoundEventArgs
            {
                Changes = changes, 
                ReplicaId = ReplicaId.GetGuidId()
            });
    
    changeDataRetriever = retriever;
    
    return batch;
}
    
private Boolean ItemFilterCallback(ItemMetadata itemmetadata)
{
    // TODO: Cache this lookup in GetChangeBatch as we don't want to unnecessarily call this for each item checked
    ItemMetadata metadata = Metadata.FindItemMetadataByUniqueIndexedField("Id", Filter.Id);
    
    if (metadata == null)
    {
        return false;
    }
    
    return itemmetadata.GlobalId == metadata.GlobalId;
}
```

That's all there is to it. Not too hard after all.

My POC project is attached to this post for reference.

[CachedSyncPOC.zip (21.08 kb)][4]

[0]: http://code.msdn.microsoft.com/sync
[1]: http://code.msdn.microsoft.com/sync/Release/ProjectReleases.aspx?ReleaseId=3419
[2]: http://social.microsoft.com/Forums/en-US/synctechnicaldiscussion/thread/6a0ebe87-7b48-4c05-91cf-e9e0a347f777/
[3]: http://msdn.microsoft.com/en-us/library/bb902843(SQL.105).aspx
[4]: /files/2010/1/CachedSyncPOC.zip
