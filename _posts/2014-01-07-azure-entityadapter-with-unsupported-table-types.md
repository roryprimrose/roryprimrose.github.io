---
title: Azure EntityAdapter with unsupported table types
tags: Azure
date: 2014-01-07 16:02:45 +10:00
---

I recently [posted][0] about an EntityAdapter class that can be the bridge between an ITableEntity that Azure table services requires and a domain model class that you actually want to use. I found an issue with this implementation where TableEntity.ReadUserObject and TableEntity.WriteUserObject that the EntityAdapter rely on will only support mapping properties for types that are intrinsically supported by ATS. This means your domain model will end up with default values for properties that are not String, Binary, Boolean, DateTime, DateTimeOffset, Double, Guid, Int32 or Int64.

I hit this issue because I started working with a model class that exposes an enum property. The integration tests failed because the read of the entity using the adapter returned the default enum value for the property rather than the one I attempted to write to the table. I have updated the EntityAdapter class to cater for this by using reflection and type converters to fill in the gaps.

<!--more-->

The class now looks like the following:

```csharp
namespace MySystem.DataAccess.Azure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Seterlund.CodeGuard;
    
    /// <summary>
    ///     The <see cref="EntityAdapter{T}" />
    ///     class provides the base adapter implementation for reading and writing a POCO class with Azure Table Storage.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of value.
    /// </typeparam>
    [CLSCompliant(false)]
    public abstract class EntityAdapter<T> : ITableEntity where T : class, new()
    {
        /// <summary>
        ///     The synchronization lock.
        /// </summary>
        /// <remarks>A dictionary is not required here because the static will have a different value for each generic type.</remarks>
        private static readonly Object _syncLock = new Object();
    
        /// <summary>
        ///     The additional properties to map for types.
        /// </summary>
        /// <remarks>A dictionary is not required here because the static will have a different value for each generic type.</remarks>
        private static List<PropertyInfo> _additionalProperties;
    
        /// <summary>
        ///     The partition key
        /// </summary>
        private string _partitionKey;
    
        /// <summary>
        ///     The row key
        /// </summary>
        private string _rowKey;
    
        /// <summary>
        ///     The entity value.
        /// </summary>
        private T _value;
    
        /// <summary>
        ///     Initializes a new instance of the <see cref="EntityAdapter{T}" /> class.
        /// </summary>
        protected EntityAdapter()
        {
        }
    
        /// <summary>
        ///     Initializes a new instance of the <see cref="EntityAdapter{T}" /> class.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        protected EntityAdapter(T value)
        {
            Guard.That(value, "value").IsNotNull();
    
            _value = value;
        }
    
        /// <inheritdoc />
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            _value = new T();
    
            TableEntity.ReadUserObject(Value, properties, operationContext);
    
            var additionalMappings = GetAdditionPropertyMappings(Value, properties);
    
            if (additionalMappings.Count > 0)
            {
                // Populate the properties missing from ReadUserObject
                foreach (var additionalMapping in additionalMappings)
                {
                    if (properties.ContainsKey(additionalMapping.Name) == false)
                    {
                        // We will let the object assign its default value for that property
                        continue;
                    }
    
                    var propertyValue = properties[additionalMapping.Name];
                    var converter = TypeDescriptor.GetConverter(additionalMapping.PropertyType);
                    var convertedValue = converter.ConvertFromInvariantString(propertyValue.StringValue);
    
                    additionalMapping.SetValue(Value, convertedValue);
                }
            }
    
            ReadValues(properties, operationContext);
        }
    
        /// <inheritdoc />
        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = TableEntity.WriteUserObject(Value, operationContext);
    
            var additionalMappings = GetAdditionPropertyMappings(Value, properties);
    
            if (additionalMappings.Count > 0)
            {
                // Populate the properties missing from WriteUserObject
                foreach (var additionalMapping in additionalMappings)
                {
                    var propertyValue = additionalMapping.GetValue(Value);
                    var converter = TypeDescriptor.GetConverter(additionalMapping.PropertyType);
                    var convertedValue = converter.ConvertToInvariantString(propertyValue);
    
                    properties[additionalMapping.Name] = EntityProperty.GeneratePropertyForString(convertedValue);
                }
            }
    
            WriteValues(properties, operationContext);
    
            return properties;
        }
    
        /// <summary>
        ///     Builds the entity partition key.
        /// </summary>
        /// <returns>
        ///     The partition key of the entity.
        /// </returns>
        protected abstract string BuildPartitionKey();
    
        /// <summary>
        ///     Builds the entity row key.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected abstract string BuildRowKey();
    
        /// <summary>
        ///     Reads the values from the specified properties.
        /// </summary>
        /// <param name="properties">
        ///     The properties of the entity.
        /// </param>
        /// <param name="operationContext">
        ///     The operation context.
        /// </param>
        protected virtual void ReadValues(
            IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
        }
    
        /// <summary>
        ///     Writes the entity values to the specified properties.
        /// </summary>
        /// <param name="properties">
        ///     The properties.
        /// </param>
        /// <param name="operationContext">
        ///     The operation context.
        /// </param>
        protected virtual void WriteValues(
            IDictionary<string, EntityProperty> properties,
            OperationContext operationContext)
        {
        }
    
        /// <summary>
        ///     Gets the additional property mappings.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="properties">The mapped properties.</param>
        /// <returns>
        ///     The additional property mappings.
        /// </returns>
        private static List<PropertyInfo> GetAdditionPropertyMappings(
            T value,
            IDictionary<string, EntityProperty> properties)
        {
            if (_additionalProperties != null)
            {
                return _additionalProperties;
            }
    
            List<PropertyInfo> additionalProperties;
    
            lock (_syncLock)
            {
                // Check the mappings again to protect against race conditions on the lock
                if (_additionalProperties != null)
                {
                    return _additionalProperties;
                }
    
                additionalProperties = ResolvePropertyMappings(value, properties);
    
                _additionalProperties = additionalProperties;
            }
    
            return additionalProperties;
        }
    
        /// <summary>
        ///     Resolves the additional property mappings.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="properties">The properties.</param>
        /// <returns>The additional properties.</returns>
        private static List<PropertyInfo> ResolvePropertyMappings(
            T value,
            IDictionary<string, EntityProperty> properties)
        {
            var objectProperties = value.GetType().GetProperties();
    
            return
                objectProperties.Where(objectProperty => properties.ContainsKey(objectProperty.Name) == false).ToList();
        }
    
        /// <inheritdoc />
        public string ETag
        {
            get;
            set;
        }
    
        /// <inheritdoc />
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
    
        /// <inheritdoc />
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
    
        /// <inheritdoc />
        public DateTimeOffset Timestamp
        {
            get;
            set;
        }
    
        /// <summary>
        ///     Gets the value managed by the adapter.
        /// </summary>
        /// <value>
        ///     The value.
        /// </value>
        public T Value
        {
            get
            {
                return _value;
            }
        }
    }
}
```

[0]: /2013/11/18/entity-adapter-for-azure-table-storage/
