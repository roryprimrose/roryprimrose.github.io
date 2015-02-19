---
title: Entity Adapter for Azure Table Storage
tags : Azure
date: 2013-11-18 21:17:58 +10:00
---

When working with Azure Table Storage you will ultimately have to deal with ITableEntity. My solution to date has been to create a class that derives from my model class and then implement ITableEntity. This derived class can them provide the plumbing for table storage while allowing the layer to return the correct model type. 

The problem here is that ITableEntity is still leaking outside of the Azure DAL even though it is represented as the expected type. While I don’t like my classes leaking knowledge inappropriately to higher layers I also don’t like plumbing logic that converts between two model classes that are logically the same (although tools like AutoMapper do take some of this pain away).

Using an entity adapter is a really clean way to get your cake and eat it. The original code of this concept was posted by the Windows Azure Storage Team (you can read it [here][0]). I’ve taken that code and tweaked it slightly to make it a little more reusable.

{% highlight csharp linenos %}
namespace MyProject.DataAccess.Azure
{
    using System;
    using System.Collections.Generic;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Seterlund.CodeGuard;
    
    internal abstract class EntityAdapter<T> : ITableEntity where T : class, new()
    {
        private string _partitionKey;
    
        private string _rowKey;
    
        private T _value;
    
        protected EntityAdapter()
        {
        }
    
        protected EntityAdapter(T value)
        {
            Guard.That(value, &quot;value&quot;).IsNotNull();
    
            _value = value;
        }
    
        /// <inheritdoc />
        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            _value = new T();
    
            TableEntity.ReadUserObject(_value, properties, operationContext);
    
            ReadValues(properties, operationContext);
        }
    
        /// <inheritdoc />
        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var properties = TableEntity.WriteUserObject(Value, operationContext);
    
            WriteValues(properties, operationContext);
    
            return properties;
        }
    
        protected abstract string BuildPartitionKey();
    
        protected abstract string BuildRowKey();
    
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
    
        public T Value
        {
            get
            {
                return _value;
            }
        }
    }
}
{% endhighlight %}

This class has the flexibility to build a partition and row key for simple adapter usage and then be extended to override ReadValues and WriteValues to store additional metadata with your value for more complex scenarios. To write your value to table storage you simply wrap it in a new instance of your adapter which will pass the value down to the appropriate base constructor. Reading the entity from table storage will then select the Value property on the way back out.

This method allows for the adapter to be an internal bridge between your model class and table storage. The type being returned from the DAL is now POCO while table storage has an ITableEntity that it can use.

[0]: http://blogs.msdn.com/b/windowsazurestorage/archive/2013/09/07/announcing-storage-client-library-2-1-rtm.aspx
