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

{% highlight csharp linenos %}
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
    
    /// <summary&gt;
    ///     The <see cref=&quot;TableBatchWriter&quot; /&gt;
    ///     class manages the process of writing a batch of entitites to a <see cref=&quot;TableBatchOperation&quot; /&gt; instance.
    /// </summary&gt;
    [CLSCompliant(false)]
    public class TableBatchWriter
    {
        /// <summary&gt;
        ///     The maximum ats table batch size.
        /// </summary&gt;
        private const int MaxAtsTableBatchSize = 100;
    
        /// <summary&gt;
        ///     The batch tasks.
        /// </summary&gt;
        private readonly List<Task&gt; _batchTasks;
    
        /// <summary&gt;
        ///     The table to write the batch to.
        /// </summary&gt;
        private readonly CloudTable _table;
    
        /// <summary&gt;
        ///     The current operation.
        /// </summary&gt;
        private TableBatchOperation _currentOperation;
    
        /// <summary&gt;
        ///     The partition key for the current batch.
        /// </summary&gt;
        private string _currentPartitionKey;
    
        /// <summary&gt;
        ///     The row keys for the current partition key.
        /// </summary&gt;
        private List<string&gt; _partitionRowKeys;
    
        /// <summary&gt;
        ///     The total items written to the table.
        /// </summary&gt;
        private int _totalItems;
    
        /// <summary&gt;
        ///     Initializes a new instance of the <see cref=&quot;TableBatchWriter&quot; /&gt; class.
        /// </summary&gt;
        public TableBatchWriter(CloudTable table)
        {
            Guard.That(() =&gt; table).IsNotNull();
    
            _table = table;
    
            _batchTasks = new List<Task&gt;();
            _partitionRowKeys = new List<string&gt;();
            _currentOperation = new TableBatchOperation();
        }
    
        /// <summary&gt;
        ///     Adds the specified entity.
        /// </summary&gt;
        /// <param name=&quot;entity&quot;&gt;The entity.</param&gt;
        /// <exception cref=&quot;System.InvalidOperationException&quot;&gt;The entity has a row key conflict in the current batch.</exception&gt;
        public void Add(ITableEntity entity)
        {
            Guard.That(() =&gt; entity).IsNotNull();
    
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
    
                _partitionRowKeys = new List<string&gt;();
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
    
        /// <summary&gt;
        ///     Adds the items.
        /// </summary&gt;
        /// <param name=&quot;items&quot;&gt;The items.</param&gt;
        public void AddItems(IEnumerable<ITableEntity&gt; items)
        {
            Guard.That(() =&gt; items).IsNotNull();
    
            foreach (var item in items)
            {
                Add(item);
            }
        }
    
        /// <summary&gt;
        ///     Executes the batch writing asynchronously.
        /// </summary&gt;
        /// <returns&gt;A <see cref=&quot;Task&quot; /&gt; value.</returns&gt;
        public async Task ExecuteAsync()
        {
            // Check if there is a final batch that has not been actioned yet
            if (Count &gt; 0)
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
            _partitionRowKeys = new List<string&gt;();
            _currentOperation = new TableBatchOperation();
        }
    
        private void WriteBatch()
        {
            var task = _table.ExecuteBatchAsync(_currentOperation);
    
            _batchTasks.Add(task);
    
            _currentOperation = new TableBatchOperation();
        }
    
        /// <summary&gt;
        ///     Gets the count.
        /// </summary&gt;
        /// <value&gt;
        ///     The count.
        /// </value&gt;
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


