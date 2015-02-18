---
title: Azure Table Storage Adapter - Fixes and Features
date: 2014-05-18 14:31:45 +10:00
---

I have posted over the last couple of months about an adapter class that I have been using for using Azure table storage. The adapter has been really useful to bridge between Azure table entities and application domain models. There have been some interesting scenarios that have been discovered while using this technique. Here has been the history so far:

* [Entity Adapter for Azure Table Storage][0]
* [Azure EntityAdapter with unsupported table types][1]
* [Using the EntityAdapter for Azure Table Storage][2]
* [Azure Table Storage Adapter Using Reserved Properties][3]

This class has evolved well although two issues have been identified. 

1. The last post indicates a workaround where your domain model exposes a property from ITableEntity (namely the Timestamp property). While there is a workaround, it would be nice if the adapter just took care of this for you.
1. Commenter Andy highlighted a race condition where unsupported property types were not read correctly where the first operation on the type was a read instead of a write (see [here][4])

The race condition that Andy found is one of the production issues I have seen on occasion that I was not able to pin down (big shout out and thanks to Andy).

This latest version of the EntityAdapter class fixes the two above issues.

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
            private static readonly object _syncLock = new object();
    
            /// <summary&gt;
            ///     The additional properties to map for types.
            /// </summary&gt;
            /// <remarks&gt;A dictionary is not required here because the static will have a different value for each generic type.</remarks&gt;
            private static List<AdditionalPropertyMetadata&gt; _additionalProperties;
    
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
            [SuppressMessage(&quot;Microsoft.Design&quot;, &quot;CA1062:Validate arguments of public methods&quot;, MessageId = &quot;0&quot;,
                Justification = &quot;Parameter is validated using CodeGuard.&quot;)]
            public void ReadEntity(IDictionary<string, EntityProperty&gt; properties, OperationContext operationContext)
            {
                Guard.That(properties, &quot;properties&quot;).IsNotNull();
    
                _value = new T();
    
                TableEntity.ReadUserObject(Value, properties, operationContext);
    
                var additionalMappings = GetAdditionPropertyMappings(Value, operationContext);
    
                if (additionalMappings.Count &gt; 0)
                {
                    ReadAdditionalProperties(properties, additionalMappings);
                }
    
                ReadValues(properties, operationContext);
            }
    
            /// <inheritdoc /&gt;
            public IDictionary<string, EntityProperty&gt; WriteEntity(OperationContext operationContext)
            {
                var properties = TableEntity.WriteUserObject(Value, operationContext);
    
                var additionalMappings = GetAdditionPropertyMappings(Value, operationContext);
    
                if (additionalMappings.Count &gt; 0)
                {
                    WriteAdditionalProperties(additionalMappings, properties);
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
            ///     Clears the cache.
            /// </summary&gt;
            protected void ClearCache()
            {
                lock (_syncLock)
                {
                    _additionalProperties = null;
                }
            }
    
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
            /// <param name=&quot;operationContext&quot;&gt;The operation context.</param&gt;
            /// <returns&gt;
            ///     The additional property mappings.
            /// </returns&gt;
            private static List<AdditionalPropertyMetadata&gt; GetAdditionPropertyMappings(
                T value,
                OperationContext operationContext)
            {
                if (_additionalProperties != null)
                {
                    return _additionalProperties;
                }
    
                List<AdditionalPropertyMetadata&gt; additionalProperties;
    
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
    
            /// <summary&gt;
            ///     Resolves the additional property mappings.
            /// </summary&gt;
            /// <param name=&quot;value&quot;&gt;The value.</param&gt;
            /// <param name=&quot;operationContext&quot;&gt;The operation context.</param&gt;
            /// <returns&gt;
            ///     The additional properties.
            /// </returns&gt;
            private static List<AdditionalPropertyMetadata&gt; ResolvePropertyMappings(
                T value,
                OperationContext operationContext)
            {
                var storageSupportedProperties = TableEntity.WriteUserObject(value, operationContext);
                var objectProperties = value.GetType().GetProperties();
                var infrastructureProperties = typeof(ITableEntity).GetProperties();
                var missingProperties =
                    objectProperties.Where(
                        objectProperty =&gt; storageSupportedProperties.ContainsKey(objectProperty.Name) == false);
    
                var additionalProperties = missingProperties.Select(
                    x =&gt; new AdditionalPropertyMetadata
                    {
                        IsInfrastructureProperty = infrastructureProperties.Any(y =&gt; x.Name == y.Name),
                        PropertyMetadata = x
                    });
    
                return additionalProperties.ToList();
            }
    
            /// <summary&gt;
            ///     Reads the additional properties.
            /// </summary&gt;
            /// <param name=&quot;properties&quot;&gt;The properties.</param&gt;
            /// <param name=&quot;additionalMappings&quot;&gt;The additional mappings.</param&gt;
            /// <exception cref=&quot;System.InvalidOperationException&quot;&gt;
            ///     The ITableEntity interface now defines a property that is not
            ///     supported by this adapter.
            /// </exception&gt;
            private void ReadAdditionalProperties(
                IDictionary<string, EntityProperty&gt; properties,
                IEnumerable<AdditionalPropertyMetadata&gt; additionalMappings)
            {
                // Populate the properties missing from ReadUserObject
                foreach (var additionalMapping in additionalMappings)
                {
                    if (additionalMapping.IsInfrastructureProperty)
                    {
                        // We don't want to use a string conversion here
                        // Explicitly map the types across
                        if (additionalMapping.PropertyMetadata.Name == &quot;Timestamp&quot; &&
                            additionalMapping.PropertyMetadata.PropertyType == typeof(DateTimeOffset))
                        {
                            // This is the timestamp property
                            additionalMapping.PropertyMetadata.SetValue(Value, Timestamp);
                        }
                        else if (additionalMapping.PropertyMetadata.Name == &quot;ETag&quot; &&
                                 additionalMapping.PropertyMetadata.PropertyType == typeof(string))
                        {
                            // This is the timestamp property
                            additionalMapping.PropertyMetadata.SetValue(Value, ETag);
                        }
                        else if (additionalMapping.PropertyMetadata.Name == &quot;PartitionKey&quot; &&
                                 additionalMapping.PropertyMetadata.PropertyType == typeof(string))
                        {
                            // This is the timestamp property
                            additionalMapping.PropertyMetadata.SetValue(Value, PartitionKey);
                        }
                        else if (additionalMapping.PropertyMetadata.Name == &quot;RowKey&quot; &&
                                 additionalMapping.PropertyMetadata.PropertyType == typeof(string))
                        {
                            // This is the timestamp property
                            additionalMapping.PropertyMetadata.SetValue(Value, RowKey);
                        }
                        else
                        {
                            const string UnsupportedPropertyMessage =
                                &quot;The {0} interface now defines a property {1} which is not supported by this adapter.&quot;;
    
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
    
            /// <summary&gt;
            ///     Writes the additional properties.
            /// </summary&gt;
            /// <param name=&quot;additionalMappings&quot;&gt;The additional mappings.</param&gt;
            /// <param name=&quot;properties&quot;&gt;The properties.</param&gt;
            private void WriteAdditionalProperties(
                IEnumerable<AdditionalPropertyMetadata&gt; additionalMappings,
                IDictionary<string, EntityProperty&gt; properties)
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
    
            /// <summary&gt;
            ///     The <see cref=&quot;AdditionalPropertyMetadata&quot; /&gt;
            ///     provides information about additional storage properties for an entity type.
            /// </summary&gt;
            private struct AdditionalPropertyMetadata
            {
                /// <summary&gt;
                ///     Gets or sets a value indicating whether this instance is infrastructure property.
                /// </summary&gt;
                /// <value&gt;
                ///     <c&gt;true</c&gt; if this instance is infrastructure property; otherwise, <c&gt;false</c&gt;.
                /// </value&gt;
                public bool IsInfrastructureProperty
                {
                    get;
                    set;
                }
    
                /// <summary&gt;
                ///     Gets or sets the property metadata.
                /// </summary&gt;
                /// <value&gt;
                ///     The property metadata.
                /// </value&gt;
                public PropertyInfo PropertyMetadata
                {
                    get;
                    set;
                }
            }
        }
    }{% endhighlight %}

[0]: http://www.neovolve.com/post/2013/11/18/Entity-Adapter-for-Azure-Table-Storage.aspx
[1]: http://www.neovolve.com/post/2014/01/07/Azure-EntityAdapter-with-unsupported-table-types.aspx
[2]: http://www.neovolve.com/post/2014/01/07/Using-the-EntityAdapter-for-Azure-Table-Storage.aspx
[3]: http://www.neovolve.com/post/2014/04/10/Azure-Table-Storage-Adapter-Using-Reserved-Properties.aspx
[4]: http://www.neovolve.com/post/2014/01/07/Azure-EntityAdapter-with-unsupported-table-types.aspx#comment-1370371154
