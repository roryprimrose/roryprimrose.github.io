---
title: Updated Azure Table Storage EntityAdapter
categories: .Net
tags: 
date: 2019-04-05 15:34:00 +11:00
---

[Way back in 2013][0] I originally posted my ```EntityAdapter<T>``` to make it really easy to read and write .Net classes to Azure Table Storage. There have been [a few updates][1] since the original post, some of which were never made public. This post provides the latest version of that class and some examples of extensibility.

<!--more-->

This is the latest version of ```EntityAdapter<T>```.

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using EnsureThat;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public abstract class EntityAdapter<T> : ITableEntity where T : class, new()
{
    private static readonly object _syncLock = new object();
    private static List<AdditionalPropertyMetadata> _additionalProperties;
    private string _partitionKey;
    private string _rowKey;

    protected EntityAdapter()
    {
    }

    protected EntityAdapter(T value)
    {
        Ensure.Any.IsNotNull(value, nameof(value));

        Value = value;
    }

    public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    {
        Ensure.Any.IsNotNull(properties, nameof(properties));

        Value = new T();

        TableEntity.ReadUserObject(this, properties, operationContext);
        TableEntity.ReadUserObject(Value, properties, operationContext);

        var additionalMappings = GetAdditionPropertyMappings(Value, operationContext);

        if (additionalMappings.Count > 0)
        {
            ReadAdditionalProperties(properties, additionalMappings);
        }

        ReadValues(properties, operationContext);
    }

    public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
    {
        var properties = TableEntity.WriteUserObject(Value, operationContext);

        var additionalMappings = GetAdditionPropertyMappings(Value, operationContext);

        if (additionalMappings.Count > 0)
        {
            WriteAdditionalProperties(additionalMappings, properties);
        }

        WriteValues(properties, operationContext);

        return properties;
    }

    protected abstract string BuildPartitionKey();

    protected abstract string BuildRowKey();

    protected void ClearCache()
    {
        lock (_syncLock)
        {
            _additionalProperties = null;
        }
    }

    protected virtual TypeConverter GetTypeConverter(PropertyInfo propertyInfo)
    {
        Ensure.Any.IsNotNull(propertyInfo, nameof(propertyInfo));

        var propertyType = propertyInfo.PropertyType;

        return TypeDescriptor.GetConverter(propertyType);
    }

    protected virtual void ReadAdditionalProperty(PropertyInfo propertyInfo, EntityProperty propertyValue)
    {
        Ensure.Any.IsNotNull(propertyInfo, nameof(propertyInfo));
        Ensure.Any.IsNotNull(propertyValue, nameof(propertyValue));

        var converter = GetTypeConverter(propertyInfo);

        try
        {
            var convertedValue = converter.ConvertFromInvariantString(propertyValue.StringValue);

            propertyInfo.SetValue(Value, convertedValue);
        }
        catch (NotSupportedException ex)
        {
            const string MessageFormat = "Failed to write string value to the '{0}' property.";
            var message = string.Format(CultureInfo.CurrentCulture, MessageFormat, propertyInfo.Name);

            throw new NotSupportedException(message, ex);
        }
    }

    protected virtual void ReadValues(
        IDictionary<string, EntityProperty> properties,
        OperationContext operationContext)
    {
    }

    protected virtual void WriteAdditionalProperty(
        IDictionary<string, EntityProperty> properties,
        PropertyInfo propertyInfo,
        object propertyValue)
    {
        Ensure.Any.IsNotNull(properties, nameof(properties));
        Ensure.Any.IsNotNull(propertyInfo, nameof(propertyInfo));

        var converter = GetTypeConverter(propertyInfo);

        try
        {
            var convertedValue = converter.ConvertToInvariantString(propertyValue);

            properties[propertyInfo.Name] = EntityProperty.GeneratePropertyForString(convertedValue);
        }
        catch (NotSupportedException ex)
        {
            const string MessageFormat = "Failed to convert property '{0}' to a string value.";
            var message = string.Format(CultureInfo.CurrentCulture, MessageFormat, propertyInfo.Name);

            throw new NotSupportedException(message, ex);
        }
    }

    protected virtual void WriteValues(
        IDictionary<string, EntityProperty> properties,
        OperationContext operationContext)
    {
    }

    private static List<AdditionalPropertyMetadata> GetAdditionPropertyMappings(
        T value,
        OperationContext operationContext)
    {
        if (_additionalProperties != null)
        {
            return _additionalProperties;
        }

        List<AdditionalPropertyMetadata> additionalProperties;

        lock (_syncLock)
        {
            // Check the mappings again to protect against race conditions on the lock
            if (_additionalProperties != null)
            {
                return _additionalProperties;
            }

            additionalProperties = ResolvePropertyMappings(value, operationContext);

            _additionalProperties = additionalProperties;
        }

        return additionalProperties;
    }

    private static List<AdditionalPropertyMetadata> ResolvePropertyMappings(
        T value,
        OperationContext operationContext)
    {
        var storageSupportedProperties = TableEntity.WriteUserObject(value, operationContext);
        var objectProperties = value.GetType().GetTypeInfo().GetProperties();
        var infrastructureProperties = typeof(ITableEntity).GetTypeInfo().GetProperties();
        var missingProperties =
            objectProperties.Where(
                objectProperty => storageSupportedProperties.ContainsKey(objectProperty.Name) == false);

        var additionalProperties = missingProperties.Select(
            x => new AdditionalPropertyMetadata
            {
                IsInfrastructureProperty = infrastructureProperties.Any(y => x.Name == y.Name),
                PropertyMetadata = x
            });

        return additionalProperties.ToList();
    }

    private void ReadAdditionalProperties(
        IDictionary<string, EntityProperty> properties,
        IEnumerable<AdditionalPropertyMetadata> additionalMappings)
    {
        // Populate the properties missing from ReadUserObject
        foreach (var additionalMapping in additionalMappings)
        {
            var propertyType = additionalMapping.PropertyMetadata.PropertyType;

            if (additionalMapping.IsInfrastructureProperty)
            {
                ReadInfrastructureProperty(additionalMapping, propertyType);
            }
            else if (properties.ContainsKey(additionalMapping.PropertyMetadata.Name))
            {
                // This is a property that has an unsupport type
                // Use a converter to resolve and apply the correct value
                var propertyValue = properties[additionalMapping.PropertyMetadata.Name];

                ReadAdditionalProperty(additionalMapping.PropertyMetadata, propertyValue);
            }

            // The else case here is that the model now contains a property that was not originally stored when the entity was last written
            // This property will assume the default value for its type
        }
    }

    private void ReadInfrastructureProperty(AdditionalPropertyMetadata additionalMapping, Type propertyType)
    {
        // We don't want to use a string conversion here
        // Explicitly map the types across
        if (additionalMapping.PropertyMetadata.Name == nameof(ITableEntity.Timestamp) &&
            propertyType == typeof(DateTimeOffset))
        {
            additionalMapping.PropertyMetadata.SetValue(Value, Timestamp);
        }
        else if (additionalMapping.PropertyMetadata.Name == nameof(ITableEntity.ETag) &&
                 propertyType == typeof(string))
        {
            additionalMapping.PropertyMetadata.SetValue(Value, ETag);
        }
        else if (additionalMapping.PropertyMetadata.Name == nameof(ITableEntity.PartitionKey) &&
                 propertyType == typeof(string))
        {
            additionalMapping.PropertyMetadata.SetValue(Value, PartitionKey);
        }
        else if (additionalMapping.PropertyMetadata.Name == nameof(ITableEntity.RowKey) &&
                 propertyType == typeof(string))
        {
            additionalMapping.PropertyMetadata.SetValue(Value, RowKey);
        }
        else
        {
            const string UnsupportedPropertyMessage =
                "The {0} interface now defines a property {1} which is not supported by this adapter.";

            var message = string.Format(
                CultureInfo.CurrentCulture,
                UnsupportedPropertyMessage,
                typeof(ITableEntity).FullName,
                additionalMapping.PropertyMetadata.Name);

            throw new InvalidOperationException(message);
        }
    }

    private void WriteAdditionalProperties(
        IEnumerable<AdditionalPropertyMetadata> additionalMappings,
        IDictionary<string, EntityProperty> properties)
    {
        // Populate the properties missing from WriteUserObject
        foreach (var additionalMapping in additionalMappings)
        {
            if (additionalMapping.IsInfrastructureProperty)
            {
                // We need to let the storage mechanism handle the write of the infrastructure properties
                continue;
            }

            var propertyValue = additionalMapping.PropertyMetadata.GetValue(Value);

            WriteAdditionalProperty(properties, additionalMapping.PropertyMetadata, propertyValue);
        }
    }

    public string ETag { get; set; }

    public string PartitionKey
    {
        get
        {
            if (_partitionKey == null)
            {
                _partitionKey = BuildPartitionKey();
            }

            return _partitionKey;
        }
        set { _partitionKey = value; }
    }

    public string RowKey
    {
        get
        {
            if (_rowKey == null)
            {
                _rowKey = BuildRowKey();
            }

            return _rowKey;
        }
        set { _rowKey = value; }
    }

    public DateTimeOffset Timestamp { get; set; }

    public T Value { get; private set; }

    private struct AdditionalPropertyMetadata
    {
        public bool IsInfrastructureProperty { get; set; }

        public PropertyInfo PropertyMetadata { get; set; }
    }
}
```

The main changes in this version are to better support extensibility. For example, I wanted to read and write properties that are ```NodaTime.Instant``` types. This can be achieved with the following:

```csharp
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EnsureThat;
using Microsoft.WindowsAzure.Storage.Table;
using NodaTime;

public abstract class NodaEntityAdapter<T> : EntityAdapter<T> where T : class, new()
{
    protected NodaEntityAdapter()
    {
    }

    protected NodaEntityAdapter(T value)
        : base(value)
    {
    }

    protected override void ReadAdditionalProperty(PropertyInfo propertyInfo, EntityProperty propertyValue)
    {
        Ensure.Any.IsNotNull(propertyInfo, nameof(propertyInfo));
        Ensure.Any.IsNotNull(propertyValue, nameof(propertyValue));

        if (propertyInfo.PropertyType == typeof(Instant?))
        {
            var offsetValue = propertyValue.DateTimeOffsetValue;

            if (offsetValue.HasValue)
            {
                var instant = Instant.FromDateTimeOffset(offsetValue.Value);

                propertyInfo.SetValue(Value, instant);
            }
        }
        else if (propertyInfo.PropertyType == typeof(Instant))
        {
            var offsetValue = propertyValue.DateTimeOffsetValue;
            var instant = Instant.FromDateTimeOffset(offsetValue.Value);

            propertyInfo.SetValue(Value, instant);
        }
        else
        {
            base.ReadAdditionalProperty(propertyInfo, propertyValue);
        }
    }

    protected override void WriteAdditionalProperty(
        IDictionary<string, EntityProperty> properties,
        PropertyInfo propertyInfo,
        object propertyValue)
    {
        Ensure.Any.IsNotNull(properties, nameof(properties));
        Ensure.Any.IsNotNull(propertyInfo, nameof(propertyInfo));

        if (propertyValue == null)
        {
            return;
        }

        if (propertyValue is Instant)
        {
            var value = (Instant) propertyValue;
            var offsetValue = value.ToDateTimeOffset();

            properties[propertyInfo.Name] = EntityProperty.GeneratePropertyForDateTimeOffset(offsetValue);
        }
        else
        {
            base.WriteAdditionalProperty(properties, propertyInfo, propertyValue);
        }
    }
}
```

Storing, reading and writing entity data can sometimes result in duplicate data. The extensibility points in the adapter can allow you to slim down how the data is stored in the table. This not only reduces the amount of data stored in each row but also how much data is transferred over the wire. My scenarios here tend to be where the PartitionKey and RowKey values are generated using properties on the entity. In these cases, there is no need to store those properties in the storage table because they can be parsed out of the keys. 
 
 In this example, I have a ```StatusChange``` class that uses ```ValidationAddressId``` and optional ```MailHostId``` properties for the PartitionKey and then uses the ```Started``` time-based property for the RowKey.

 ```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using NodaTime;
using MyProduct.Models;

public class StatusChangeAdapter : NodaEntityAdapter<StatusChange>
{
    private const string NullValueMarker = "null";
    private const char Separator = '|';

    public StatusChangeAdapter()
    {
    }

    public StatusChangeAdapter(StatusChange statusChange)
        : base(statusChange)
    {
    }
    
    public static string BuildPartitionKey(Guid validationAddressId, Guid? mailHostId)
    {
        var key = validationAddressId.ToString() + Separator;

        if (mailHostId == null)
        {
            key += NullValueMarker;
        }
        else
        {
            key += mailHostId.Value;
        }

        return key;
    }

    public static string BuildRowKey(Instant started)
    {
        // NOTE: ToDateTimeOffset is required here because DateTimeOffset.Ticks is different to Instant.Ticks and legacy data has already been stored using DateTimeOffset.Ticks prior to the migration of code from DateTimeOffset to Instant
        return string.Format(CultureInfo.InvariantCulture, "{0:D19}", started.ToDateTimeOffset().Ticks);
    }

    protected override string BuildPartitionKey()
    {
        return BuildPartitionKey(Value.ValidationAddressId, Value.MailHostId);
    }

    protected override string BuildRowKey()
    {
        return BuildRowKey(Value.Started);
    }

    protected override void ReadValues(
        IDictionary<string, EntityProperty> properties,
        OperationContext operationContext)
    {
        var partitionKeyParts = PartitionKey.Split(Separator);

        // Get the ValidationAddressId and MailHostId from the partition key
        Value.ValidationAddressId = Guid.Parse(partitionKeyParts[0]);
        Value.MailHostId = ParseMailHostId(partitionKeyParts[1]);

        // Get the Started from the row key
        Value.Started = ParseStarted(RowKey);

        base.ReadValues(properties, operationContext);
    }

    protected override void WriteValues(
        IDictionary<string, EntityProperty> properties,
        OperationContext operationContext)
    {
        // Remove values that are stored as part of the partition key
        properties.Remove(nameof(StatusChange.ValidationAddressId));
        properties.Remove(nameof(StatusChange.MailHostId));

        // Remove values that are stored as part of the row key
        properties.Remove(nameof(StatusChange.Started));

        base.WriteValues(properties, operationContext);
    }

    private static Guid? ParseMailHostId(string partitionKeyPart)
    {
        Guid? mailHostId;

        if (partitionKeyPart == NullValueMarker)
        {
            mailHostId = null;
        }
        else
        {
            mailHostId = Guid.Parse(partitionKeyPart);
        }

        return mailHostId;
    }

    private static Instant ParseStarted(string rowKey)
    {
        var scheduledTicks = long.Parse(rowKey);
        var scheduledDateTime = DateTimeOffset.MinValue.AddTicks(scheduledTicks);
        var scheduledForTime = Instant.FromDateTimeOffset(scheduledDateTime);

        return scheduledForTime;
    }
}
```

The key parts in this example are the ```ReadValues``` and ```WriteValues``` methods where those three properties are prevented from being written to the table and then rehydrated when the row is read back into an instance of the class.

[0]: /2013/11/18/entity-adapter-for-azure-table-storage/
[1]: /2014/05/18/azure-table-storage-adapter-fixes-and-features/