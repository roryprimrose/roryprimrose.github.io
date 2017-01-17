---
title: Azure Table Storage Adapter - Fixes and Features
date: 2014-05-18 14:31:45 +10:00
---

I have posted over the last couple of months about an adapter class that I have been using for using Azure table storage. The adapter has been really useful to bridge between Azure table entities and application domain models. There have been some interesting scenarios that have been discovered while using this technique. Here has been the history so far:

* [Entity Adapter for Azure Table Storage][0]
* [Azure EntityAdapter with unsupported table types][1]
* [Using the EntityAdapter for Azure Table Storage][2]
* [Azure Table Storage Adapter Using Reserved Properties][3]

<!--more-->

This class has evolved well although two issues have been identified. 

1. The last post indicates a workaround where your domain model exposes a property from ITableEntity (namely the Timestamp property). While there is a workaround, it would be nice if the adapter just took care of this for you.
1. Commenter Andy highlighted a race condition where unsupported property types were not read correctly where the first operation on the type was a read instead of a write (see [here][4])

The race condition that Andy found is one of the production issues I have seen on occasion that I was not able to pin down (big shout out and thanks to Andy).

This latest version of the EntityAdapter class fixes the two above issues.

<!--more-->

```csharp
namespace MySystem.Server.DataAccess.Azure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Seterlund.CodeGuard;
    
    [CLSCompliant(false)]
    public abstract class EntityAdapter<T> : ITableEntity where T : class, new()
    {
        private static readonly object _syncLock = new object();
        private static List<AdditionalPropertyMetadata> _additionalProperties;
        private string _partitionKey;
        private string _rowKey;
        private T _value;
    
        protected EntityAdapter()
        {
        }
    
        protected EntityAdapter(T value)
        {
            Guard.That(value, "value").IsNotNull();
    
            _value = value;
        }
    
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0",
            Justification = "Parameter is validated using CodeGuard.")]
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            Guard.That(properties, "properties").IsNotNull();
    
            _value = new T();
    
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
    
        protected virtual void ReadValues(
            IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
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
            var objectProperties = value.GetType().GetProperties();
            var infrastructureProperties = typeof(ITableEntity).GetProperties();
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
                if (additionalMapping.IsInfrastructureProperty)
                {
                    // We don't want to use a string conversion here
                    // Explicitly map the types across
                    if (additionalMapping.PropertyMetadata.Name == "Timestamp" &&
                        additionalMapping.PropertyMetadata.PropertyType == typeof(DateTimeOffset))
                    {
                        additionalMapping.PropertyMetadata.SetValue(Value, Timestamp);
                    }
                    else if (additionalMapping.PropertyMetadata.Name == "ETag" &&
                                additionalMapping.PropertyMetadata.PropertyType == typeof(string))
                    {
                        additionalMapping.PropertyMetadata.SetValue(Value, ETag);
                    }
                    else if (additionalMapping.PropertyMetadata.Name == "PartitionKey" &&
                                additionalMapping.PropertyMetadata.PropertyType == typeof(string))
                    {
                        additionalMapping.PropertyMetadata.SetValue(Value, PartitionKey);
                    }
                    else if (additionalMapping.PropertyMetadata.Name == "RowKey" &&
                                additionalMapping.PropertyMetadata.PropertyType == typeof(string))
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
                else if (properties.ContainsKey(additionalMapping.PropertyMetadata.Name))
                {
                    // This is a property that has an unsupport type
                    // Use a converter to resolve and apply the correct value
                    var propertyValue = properties[additionalMapping.PropertyMetadata.Name];
                    var converter = TypeDescriptor.GetConverter(additionalMapping.PropertyMetadata.PropertyType);
                    var convertedValue = converter.ConvertFromInvariantString(propertyValue.StringValue);
    
                    additionalMapping.PropertyMetadata.SetValue(Value, convertedValue);
                }
    
                // The else case here is that the model now contains a property that was not originally stored when the entity was last written
                // This property will assume the default value for its type
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
                var converter = TypeDescriptor.GetConverter(additionalMapping.PropertyMetadata.PropertyType);
                var convertedValue = converter.ConvertToInvariantString(propertyValue);
    
                properties[additionalMapping.PropertyMetadata.Name] =
                    EntityProperty.GeneratePropertyForString(convertedValue);
            }
        }
    
        public string ETag
        {
            get;
            set;
        }
    
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
    
            set
            {
                _partitionKey = value;
            }
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
    
            set
            {
                _rowKey = value;
            }
        }
    
        public DateTimeOffset Timestamp
        {
            get;
            set;
        }
    
        public T Value
        {
            get
            {
                return _value;
            }
        }
    
        private struct AdditionalPropertyMetadata
        {
            public bool IsInfrastructureProperty
            {
                get;
                set;
            }
    
            public PropertyInfo PropertyMetadata
            {
                get;
                set;
            }
        }
    }
}
```

[0]: /2013/11/18/entity-adapter-for-azure-table-storage/
[1]: /2014/01/07/azure-entityadapter-with-unsupported-table-types/
[2]: /2014/01/07/using-the-entityadapter-for-azure-table-storage/
[3]: /2014/04/10/azure-table-storage-adapter-using-reserved-properties/
[4]: /2014/01/07/azure-entityadapter-with-unsupported-table-types/#comment-1370371154
