---
title: Using the EntityAdapter for Azure Table Storage
tags : Azure
date: 2014-01-07 16:18:14 +10:00
---

I got a request for an example of how to use the EntityAdapter class I [previously posted about][0]. Here is an example of a PersonAdapter.

    public enum Gender
    {
        Unspecified = 0,
    
        Female,
    
        Mail
    }
    
    public class Person
    {
        public string Email
        {
            get;
            set;
        }
    
        public String FirstName
        {
            get;
            set;
        }
    
        public Gender Gender
        {
            get;
            set;
        }
    
        public string LastName
        {
            get;
            set;
        }
    }
    
    public class PersonAdapter : EntityAdapter<Person&gt;
    {
        public PersonAdapter()
        {
        }
    
        public PersonAdapter(Person person) : base(person)
        {
        }
    
        public static string BuildPartitionKey(string email)
        {
            var index = email.IndexOf(&quot;@&quot;);
    
            return email.Substring(index);
        }
    
        public static string BuildRowKey(string email)
        {
            var index = email.IndexOf(&quot;@&quot;);
    
            return email.Substring(0, index);
        }
    
        protected override string BuildPartitionKey()
        {
            return BuildPartitionKey(Value.Email);
        }
    
        protected override string BuildRowKey()
        {
            return BuildRowKey(Value.Email);
        }
    }{% endhighlight %}

This adapter can be used to read and write entities to ATS like the following.

    public async Task<IEnumerable<Person&gt;&gt; ReadDomainUsersAsync(string domain)
    {
        var storageAccount = CloudStorageAccount.Parse(&quot;YourConnectionString&quot;);
    
        // Create the table client
        var client = storageAccount.CreateCloudTableClient();
    
        var table = client.GetTableReference(&quot;People&quot;);
    
        var tableExists = await table.ExistsAsync().ConfigureAwait(false);
    
        if (tableExists == false)
        {
            // No items could possibly be returned
            return new List<Person&gt;();
        }
    
        var partitionKey = PersonAdapter.BuildPartitionKey(domain);
        var partitionKeyFilter = TableQuery.GenerateFilterCondition(
            &quot;PartitionKey&quot;,
            QueryComparisons.Equal,
            partitionKey);
    
        var query = new TableQuery<PersonAdapter&gt;().Where(partitionKeyFilter);
    
        var results = table.ExecuteQuery(query);
    
        if (results == null)
        {
            return new List<Person&gt;();
        }
    
        return results.Select(x =&gt; x.Value);
    }
    
    public async Task WritePersonAsync(Person person)
    {
        var storageAccount = CloudStorageAccount.Parse(&quot;YourConnectionString&quot;);
    
        // Create the table client
        var client = storageAccount.CreateCloudTableClient();
    
        var table = client.GetTableReference(&quot;People&quot;);
    
        await table.CreateIfNotExistsAsync().ConfigureAwait(false);
    
        var adapter = new PersonAdapter(person);
        var operation = TableOperation.InsertOrReplace(adapter);
    
        table.Execute(operation);
    }{% endhighlight %}

Hope this helps.

[0]: /post/2014/01/07/Azure-EntityAdapter-with-unsupported-table-types.aspx
