---
title: Writing batches to Azure Table Storage
tags : Azure
date: 2013-11-14 07:24:38 +10:00
---

Writing records to Azure Table Storage in batches is handy when you are writing a lot of records because it reduces the transaction cost. There are restrictions however. The batch must:

* Be no more than 100 records
* Have the same partition key
* Have unique row keys

Writing batches is easy, even adhering to the above rules. The problem however is that it can start to result in a lot of boilerplate style code. I created a batch writer class to abstract this logic away.

<!--more-->

{% highlight csharp %}
namespace MyProject.Server.DataAccess.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Table;
    using Seterlund.CodeGuard;
    using MyProject.Server.DataAccess.Azure.Properties;
    
    /// <summary>
    ///     The <see cref=&quot;TableBatchWriter&quot; />
    ///     class manages the process of writing a batch of entitites to a <see cref=&quot;TableBatchOperation&quot; /> instance.
    /// </summary>
    [CLSCompliant(false)]
    public class TableBatchWriter
    {
        /// <summary>
        ///     The maximum ats table batch size.
        /// </summary>
        private const int MaxAtsTableBatchSize = 100;
    
        /// <summary>
        ///     The batch tasks.
        /// </summary>
        private readonly List<Task> _batchTasks;
    
        /// <summary>
        ///     The table to write the batch to.
        /// </summary>
        private readonly CloudTable _table;
    
        /// <summary>
        ///     The current operation.
        /// </summary>
        private TableBatchOperation _currentOperation;
    
        /// <summary>
        ///     The partition key for the current batch.
        /// </summary>
        private string _currentPartitionKey;
    
        /// <summary>
        ///     The row keys for the current partition key.
        /// </summary>
        private List<string> _partitionRowKeys;
    
        /// <summary>
        ///     The total items written to the table.
        /// </summary>
        private int _totalItems;
    
        /// <summary>
        ///     Initializes a new instance of the <see cref=&quot;TableBatchWriter&quot; /> class.
        /// </summary>
        public TableBatchWriter(CloudTable table)
        {
            Guard.That(() => table).IsNotNull();
    
            _table = table;
    
            _batchTasks = new List<Task>();
            _partitionRowKeys = new List<string>();
            _currentOperation = new TableBatchOperation();
        }
    
        /// <summary>
        ///     Adds the specified entity.
        /// </summary>
        /// <param name=&quot;entity&quot;>The entity.</param>
        /// <exception cref=&quot;System.InvalidOperationException&quot;>The entity has a row key conflict in the current batch.</exception>
        public void Add(ITableEntity entity)
        {
            Guard.That(() => entity).IsNotNull();
    
            if (Count == 0)
            {
                // This is the first entry
                _currentPartitionKey = entity.PartitionKey;
            }
            else if (entity.PartitionKey != _currentPartitionKey)
            {
                Debug.WriteLine(
                    &quot;PartitionKey changed from '{0}' to '{1}' at index {2}. Writing batch of {3} items to table storage.&quot;,
                    _currentPartitionKey,
                    entity.PartitionKey,
                    _totalItems - 1,
                    Count);
    
                WriteBatch();
    
                _partitionRowKeys = new List<string>();
                _currentPartitionKey = entity.PartitionKey;
            }
            else if (_partitionRowKeys.Contains(entity.RowKey))
            {
                // There are existing items in the batch and we haven't changed partition key
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.TableBatchWriter_RowKeyConflict,
                    _currentPartitionKey,
                    entity.RowKey);
    
                throw new InvalidOperationException(message);
            }
    
            _partitionRowKeys.Add(entity.RowKey);
            _currentOperation.InsertOrReplace(entity);
            _totalItems++;
    
            if (Count == MaxAtsTableBatchSize)
            {
                Debug.WriteLine(
                    &quot;Batch count of {0} has been reached at index {1}. Writing batch to table storage.&quot;,
                    MaxAtsTableBatchSize,
                    _totalItems - 1);
    
                WriteBatch();
            }
        }
    
        /// <summary>
        ///     Adds the items.
        /// </summary>
        /// <param name=&quot;items&quot;>The items.</param>
        public void AddItems(IEnumerable<ITableEntity> items)
        {
            Guard.That(() => items).IsNotNull();
    
            foreach (var item in items)
            {
                Add(item);
            }
        }
    
        /// <summary>
        ///     Executes the batch writing asynchronously.
        /// </summary>
        /// <returns>A <see cref=&quot;Task&quot; /> value.</returns>
        public async Task ExecuteAsync()
        {
            // Check if there is a final batch that has not been actioned yet
            if (Count > 0)
            {
                Debug.WriteLine(&quot;Writing final batch of {0} entries to table storage.&quot;, Count);
    
                WriteBatch();
            }
    
            if (_batchTasks.Count == 0)
            {
                return;
            }
    
            await Task.WhenAll(_batchTasks).ConfigureAwait(false);
    
            // Clean up resources
            _batchTasks.Clear();
            _partitionRowKeys = new List<string>();
            _currentOperation = new TableBatchOperation();
        }
    
        private void WriteBatch()
        {
            var task = _table.ExecuteBatchAsync(_currentOperation);
    
            _batchTasks.Add(task);
    
            _currentOperation = new TableBatchOperation();
        }
    
        /// <summary>
        ///     Gets the count.
        /// </summary>
        /// <value>
        ///     The count.
        /// </value>
        public int Count
        {
            get
            {
                return _currentOperation.Count;
            }
        }
    }
}
{% endhighlight %}

With this class you can add as many entities as you like and then wait on ExecuteAsync to finish off the work. The only issue that this class doesn’t cover is where you have a RowKey conflict that happens to fall across batches. Not much you can do about that though.