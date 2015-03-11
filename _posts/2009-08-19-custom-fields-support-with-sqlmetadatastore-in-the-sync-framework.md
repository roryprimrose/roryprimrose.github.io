---
title: Custom fields support with SqlMetadataStore in the sync framework
categories : .Net
date: 2009-08-19 13:59:52 +10:00
---

I am putting together a sync framework provider and have come across some issues that do not appear to be documented. I’m adding custom fields to the item metadata so that I can store additional information for items that are synchronised with another provider. To do this, you define the custom fields for items as part of initializing the metadata store. The tricky thing is that it isn’t clear what data types are supported and how to correctly use the data types that are supported.

I have the following initialization code.

<!--more-->

{% highlight csharp %}
List<FieldSchema> customFields = new List<FieldSchema>
                                        {
                                            new FieldSchema("IsContainer", typeof(Boolean)),
                                            new FieldSchema("Name", typeof(String)),
                                            new FieldSchema("Path", typeof(String))
                                        };
String[] indexFieldNames = new[]
                                {
                                    "IsContainer", "Name", "Path"
                                };
List<IndexSchema> indexFields = new List<IndexSchema>
                                    {
                                        new IndexSchema(indexFieldNames, true)
                                    };
    
Metadata = MetaStore.InitializeReplicaMetadata(IdFormats, ReplicaId, customFields, indexFields);
{% endhighlight %}

This code throws the following exception:

> _Microsoft.Synchronization.MetadataStorage.MetadataStoreInvalidOperationException: Incorrect constructor used_

It turns out that defining a FieldSchema as a string requires a defined length, hence the incorrect constructor has been used as there is another constructor that takes the size in addition to the name and type used here. This is not a good exception message as it does not adequately describe why there is a problem or how to fix it. The remarks documentation for the name and type constructor overload is also incorrect as it says the following.

This form of the constructor sets _MaxLength_ to the size of dataType, in bytes.

There must be something wrong with the implementation of this constructor as this does not appear to be the case. If the length is provided to the constructor overload then no exception is thrown when constructing FieldSchema instances.

The code now looks like the following:

{% highlight csharp %}
List<FieldSchema> customFields = new List<FieldSchema>
                                        {
                                            new FieldSchema("IsContainer", typeof(Boolean)),
                                            new FieldSchema("Name", typeof(String), 256),
                                            new FieldSchema("Path", typeof(String), 256)
                                        };
String[] indexFieldNames = new[]
                                {
                                    "IsContainer", "Name", "Path"
                                };
List<IndexSchema> indexFields = new List<IndexSchema>
                                    {
                                        new IndexSchema(indexFieldNames, true)
                                    };
    
Metadata = MetaStore.InitializeReplicaMetadata(IdFormats, ReplicaId, customFields, indexFields);
{% endhighlight %}

This now throws an exception saying:

> _System.NotSupportedException: The data type is not supported._

With a stack trace that looks like the following:

> _Microsoft.Synchronization.MetadataStorage.Utility.ConvertDataTypeToSyncMetadataFieldType(Type dataType) 
> Microsoft.Synchronization.MetadataStorage.SqlMetadataStore.CreateCustomFieldDefinition(FieldSchema fieldSchema, String parameterName) 
> Microsoft.Synchronization.MetadataStorage.SqlMetadataStore.InitializeReplicaMetadata(SyncIdFormatGroup idFormats, SyncId replicaId, IEnumerable`1 customItemFieldSchemas, IEnumerable`1 customIndexedFieldSchemas) 
> Neovolve.Jabiru.Client.Synchronization.Remote.StoreSyncProvider.LoadMetadataStore() in R:\Codeplex\Jabiru\Jabiru\Neovolve.Jabiru.Client\Neovolve.Jabiru.Client.Synchronization.Remote\StoreSyncProvider.cs: line 668 
> Neovolve.Jabiru.Client.Synchronization.Remote.StoreSyncProvider..ctor(IStoreProcessor processor) in R:\Codeplex\Jabiru\Jabiru\Neovolve.Jabiru.Client\Neovolve.Jabiru.Client.Synchronization.Remote\StoreSyncProvider.cs: line 64 
> Neovolve.Jabiru.Client.Synchronization.Remote.UnitTests.ServiceSyncProviderTests.RunSync(String localPath, IStoreProcessor processor, Guid storeGuid) in R:\Codeplex\Jabiru\Jabiru\Neovolve.Jabiru.Client\Neovolve.Jabiru.Client.Synchronization.Remote.UnitTests\ServiceSyncProviderTests.cs: line 160 
> Neovolve.Jabiru.Client.Synchronization.Remote.UnitTests.ServiceSyncProviderTests.SyncDeletesItemFromProcessorWhenFileIsDeletedTest() in R:\Codeplex\Jabiru\Jabiru\Neovolve.Jabiru.Client\Neovolve.Jabiru.Client.Synchronization.Remote.UnitTests\ServiceSyncProviderTests.cs: line 116_
  
If you follow down the rabbit hole using Reflector, Microsoft.Synchronization.MetadataStorage.Utility.ConvertDataTypeToSyncMetadataFieldType has the following code:

{% highlight csharp %}
internal static SYNC_METADATA_FIELD_TYPE ConvertDataTypeToSyncMetadataFieldType(Type dataType)
{
    if (dataType == typeof(byte[]))
    {
        return SYNC_METADATA_FIELD_TYPE.SYNC_METADATA_FIELD_TYPE_BYTEARRAY;
    }
    if (dataType == typeof(string))
    {
        return SYNC_METADATA_FIELD_TYPE.SYNC_METADATA_FIELD_TYPE_STRING;
    }
    if (dataType == typeof(byte))
    {
        return SYNC_METADATA_FIELD_TYPE.SYNC_METADATA_FIELD_TYPE_UINT8;
    }
    if (dataType == typeof(ushort))
    {
        return SYNC_METADATA_FIELD_TYPE.SYNC_METADATA_FIELD_TYPE_UINT16;
    }
    if (dataType == typeof(uint))
    {
        return SYNC_METADATA_FIELD_TYPE.SYNC_METADATA_FIELD_TYPE_UINT32;
    }
    if (dataType == typeof(ulong))
    {
        return SYNC_METADATA_FIELD_TYPE.SYNC_METADATA_FIELD_TYPE_UINT64;
    }
    if (dataType != typeof(Guid))
    {
        throw new NotSupportedException(StringResources.NotSupportedDataType);
    }
    return SYNC_METADATA_FIELD_TYPE.SYNC_METADATA_FIELD_TYPE_GUID;
}
{% endhighlight %}

The answer to this issue is that the Boolean data type is not supported. Unfortunately the documentation for InitializeReplicaMetadata does not to mention this. The way around this is to use a byte value to represent the Boolean state.

Code that successfully executes now looks like this:

{% highlight csharp %}
List<FieldSchema> customFields = new List<FieldSchema>
                                        {
                                            new FieldSchema("IsContainer", typeof(Byte)),
                                            new FieldSchema("Name", typeof(String), 256),
                                            new FieldSchema("Path", typeof(String), 256)
                                        };
String[] indexFieldNames = new[]
                                {
                                    "IsContainer", "Name", "Path"
                                };
List<IndexSchema> indexFields = new List<IndexSchema>
                                    {
                                        new IndexSchema(indexFieldNames, true)
                                    };
    
Metadata = MetaStore.InitializeReplicaMetadata(IdFormats, ReplicaId, customFields, indexFields);
{% endhighlight %}
