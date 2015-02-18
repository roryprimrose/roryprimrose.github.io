---
title: Azure EntityAdapter with unsupported table types
tags : Azure
date: 2014-01-07 16:02:45 +10:00
---

I recently [posted][0] about an EntityAdapter class that can be the bridge between an ITableEntity that Azure table services requires and a domain model class that you actually want to use. I found an issue with this implementation where TableEntity.ReadUserObject and TableEntity.WriteUserObject that the EntityAdapter rely on will only support mapping properties for types that are intrinsically supported by ATS. This means your domain model will end up with default values for properties that are not String, Binary, Boolean, DateTime, DateTimeOffset, Double, Guid, Int32 or Int64.

I hit this issue because I started working with a model class that exposes an enum property. The integration tests failed because the read of the entity using the adapter returned the default enum value for the property rather than the one I attempted to write to the table. I have updated the EntityAdapter class to cater for this by using reflection and type converters to fill in the gaps.

The class now looks like the following:

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
    
        /// <summary&gt;
        ///     The <see cref=&quot;EntityAdapter{T}&quot; /&gt;
        ///     class provides the base adapter implementation for reading and writing a POCO class with Azure Table Storage.
        /// </summary&gt;
        /// <typeparam name=&quot;T&quot;&gt;
        ///     The type of value.
        /// </typeparam&gt;
        [CLSCompliant(false)]
        public abstract class EntityAdapter<T&gt; : ITableEntity where T : class, new()
        {
            /// <summary&gt;
            ///     The synchronization lock.
            /// </summary&gt;
            /// <remarks&gt;A dictionary is not required here because the static will have a different value for each generic type.</remarks&gt;
            private static readonly Object _syncLock = new Object();
    
            /// <summary&gt;
            ///     The additional properties to map for types.
            /// </summary&gt;
            /// <remarks&gt;A dictionary is not required here because the static will have a different value for each generic type.</remarks&gt;
            private static List<PropertyInfo&gt; _additionalProperties;
    
            /// <summary&gt;
            ///     The partition key
            /// </summary&gt;
            private string _partitionKey;
    
            /// <summary&gt;
            ///     The row key
            /// </summary&gt;
            private string _rowKey;
    
            /// <summary&gt;
            ///     The entity value.
            /// </summary&gt;
            private T _value;
    
            /// <summary&gt;
            ///     Initializes a new instance of the <see cref=&quot;EntityAdapter{T}&quot; /&gt; class.
            /// </summary&gt;
            protected EntityAdapter()
            {
            }
    
            /// <summary&gt;
            ///     Initializes a new instance of the <see cref=&quot;EntityAdapter{T}&quot; /&gt; class.
            /// </summary&gt;
            /// <param name=&quot;value&quot;&gt;
            ///     The value.
            /// </param&gt;
            protected EntityAdapter(T value)
            {
                Guard.That(value, &quot;value&quot;).IsNotNull();
    
                _value = value;
            }
    
            /// <inheritdoc /&gt;
            public void ReadEntity(IDictionary<string, EntityProperty&gt; properties, OperationContext operationContext)
            {
                _value = new T();
    
                TableEntity.ReadUserObject(Value, properties, operationContext);
    
                var additionalMappings = GetAdditionPropertyMappings(Value, properties);
    
                if (additionalMappings.Count &gt; 0)
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
    
            /// <inheritdoc /&gt;
            public IDictionary<string, EntityProperty&gt; WriteEntity(OperationContext operationContext)
            {
                var properties = TableEntity.WriteUserObject(Value, operationContext);
    
                var additionalMappings = GetAdditionPropertyMappings(Value, properties);
    
                if (additionalMappings.Count &gt; 0)
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
    
            /// <summary&gt;
            ///     Builds the entity partition key.
            /// </summary&gt;
            /// <returns&gt;
            ///     The partition key of the entity.
            /// </returns&gt;
            protected abstract string BuildPartitionKey();
    
            /// <summary&gt;
            ///     Builds the entity row key.
            /// </summary&gt;
            /// <returns&gt;
            ///     The <see cref=&quot;string&quot; /&gt;.
            /// </returns&gt;
            protected abstract string BuildRowKey();
    
            /// <summary&gt;
            ///     Reads the values from the specified properties.
            /// </summary&gt;
            /// <param name=&quot;properties&quot;&gt;
            ///     The properties of the entity.
            /// </param&gt;
            /// <param name=&quot;operationContext&quot;&gt;
            ///     The operation context.
            /// </param&gt;
            protected virtual void ReadValues(
                IDictionary<string, EntityProperty&gt; properties,
                OperationContext operationContext)
            {
            }
    
            /// <summary&gt;
            ///     Writes the entity values to the specified properties.
            /// </summary&gt;
            /// <param name=&quot;properties&quot;&gt;
            ///     The properties.
            /// </param&gt;
            /// <param name=&quot;operationContext&quot;&gt;
            ///     The operation context.
            /// </param&gt;
            protected virtual void WriteValues(
                IDictionary<string, EntityProperty&gt; properties,
                OperationContext operationContext)
            {
            }
    
            /// <summary&gt;
            ///     Gets the additional property mappings.
            /// </summary&gt;
            /// <param name=&quot;value&quot;&gt;The value.</param&gt;
            /// <param name=&quot;properties&quot;&gt;The mapped properties.</param&gt;
            /// <returns&gt;
            ///     The additional property mappings.
            /// </returns&gt;
            private static List<PropertyInfo&gt; GetAdditionPropertyMappings(
                T value,
                IDictionary<string, EntityProperty&gt; properties)
            {
                if (_additionalProperties != null)
                {
                    return _additionalProperties;
                }
    
                List<PropertyInfo&gt; additionalProperties;
    
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
    
            /// <summary&gt;
            ///     Resolves the additional property mappings.
            /// </summary&gt;
            /// <param name=&quot;value&quot;&gt;The value.</param&gt;
            /// <param name=&quot;properties&quot;&gt;The properties.</param&gt;
            /// <returns&gt;The additional properties.</returns&gt;
            private static List<PropertyInfo&gt; ResolvePropertyMappings(
                T value,
                IDictionary<string, EntityProperty&gt; properties)
            {
                var objectProperties = value.GetType().GetProperties();
    
                return
                    objectProperties.Where(objectProperty =&gt; properties.ContainsKey(objectProperty.Name) == false).ToList();
            }
    
            /// <inheritdoc /&gt;
            public string ETag
            {
                get;
                set;
            }
    
            /// <inheritdoc /&gt;
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
    
            /// <inheritdoc /&gt;
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
    
            /// <inheritdoc /&gt;
            public DateTimeOffset Timestamp
            {
                get;
                set;
            }
    
            /// <summary&gt;
            ///     Gets the value managed by the adapter.
            /// </summary&gt;
            /// <value&gt;
            ///     The value.
            /// </value&gt;
            public T Value
            {
                get
                {
                    return _value;
                }
            }
        }
    }{% endhighlight %}

[0]: /post/2013/11/18/Entity-Adapter-for-Azure-Table-Storage.aspx
