---
title: Using the EntityAdapter for Azure Table Storage
tags : Azure
date: 2014-01-07 16:18:14 +10:00
---

I got a request for an example of how to use the EntityAdapter class I [previously posted about][0]. Here is an example of a PersonAdapter.

<!--more-->

{% highlight csharp %}
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
    
public class PersonAdapter : EntityAdapter<Person>
{
    public PersonAdapter()
    {
    }
    
    public PersonAdapter(Person person) : base(person)
    {
    }
    
    public static string BuildPartitionKey(string email)
    {
        var index = email.IndexOf("@");
    
        return email.Substring(index);
    }
    
    public static string BuildRowKey(string email)
    {
        var index = email.IndexOf("@");
    
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
}
{% endhighlight %}

This adapter can be used to read and write entities to ATS like the following.

{% highlight csharp %}
public async Task<IEnumerable<Person>> ReadDomainUsersAsync(string domain)
{
    var storageAccount = CloudStorageAccount.Parse("YourConnectionString");
    
    // Create the table client
    var client = storageAccount.CreateCloudTableClient();
    
    var table = client.GetTableReference("People");
    
    var tableExists = await table.ExistsAsync().ConfigureAwait(false);
    
    if (tableExists == false)
    {
        // No items could possibly be returned
        return new List<Person>();
    }
    
    var partitionKey = PersonAdapter.BuildPartitionKey(domain);
    var partitionKeyFilter = TableQuery.GenerateFilterCondition(
        "PartitionKey",
        QueryComparisons.Equal,
        partitionKey);
    
    var query = new TableQuery<PersonAdapter>().Where(partitionKeyFilter);
    
    var results = table.ExecuteQuery(query);
    
    if (results == null)
    {
        return new List<Person>();
    }
    
    return results.Select(x => x.Value);
}
    
public async Task WritePersonAsync(Person person)
{
    var storageAccount = CloudStorageAccount.Parse("YourConnectionString");
    
    // Create the table client
    var client = storageAccount.CreateCloudTableClient();
    
    var table = client.GetTableReference("People");
    
    await table.CreateIfNotExistsAsync().ConfigureAwait(false);
    
    var adapter = new PersonAdapter(person);
    var operation = TableOperation.InsertOrReplace(adapter);
    
    table.Execute(operation);
}
{% endhighlight %}

Hope this helps.

[0]: /2014/01/07/azure-entityadapter-with-unsupported-table-types/
