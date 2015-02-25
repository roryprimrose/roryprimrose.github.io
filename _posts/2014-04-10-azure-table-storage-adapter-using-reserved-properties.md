---
title: Azure Table Storage Adapter Using Reserved Properties
tags : Azure
date: 2014-04-10 22:27:23 +10:00
---

I posted earlier this year about [an adapter class][0] to be the bridge between ITableEntity and a domain model class when using Azure table storage. I hit a problem with this today when I was dealing with a model class that had a Timestamp property. 

While the adapter class is intended to encapsulate ITableEntity to prevent it leaking from the data layer, this particular model actually wanted to expose the Timestamp value from ITableEntity. This didn’t go down too well.

<!--more-->

> Microsoft.WindowsAzure.Storage.StorageException: An incompatible primitive type 'Edm.String[Nullable=True]' was found for an item that was expected to be of type 'Edm.DateTime[Nullable=False]'. ---&gt; Microsoft.Data.OData.ODataException: An incompatible primitive type 'Edm.String[Nullable=True]' was found for an item that was expected to be of type 'Edm.DateTime[Nullable=False]'

The simple fix to the adapter class is to filter out ITableEntity properties from the custom property mapping.

{% highlight csharp %}
private static List<PropertyInfo> ResolvePropertyMappings(
    T value,
    IDictionary<string, EntityProperty> properties)
{
    var objectProperties = value.GetType().GetProperties();
    var infrastructureProperties = typeof(ITableEntity).GetProperties();
    var missingProperties =
        objectProperties.Where(objectProperty => properties.ContainsKey(objectProperty.Name) == false);
    var additionalProperties =
        missingProperties.Where(x => infrastructureProperties.Any(y => x.Name == y.Name) == false);
    
    return additionalProperties.ToList();
}
{% endhighlight %}

This makes sure that the Timestamp property is not included in the properties to write to table storage which leads to the StorageException. This still leaves the model returned from table storage without the Timestamp property being assigned. The adapter class implementation can fix this with the following code.

{% highlight csharp %}
/// <inheritdoc />
protected override void ReadValues(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
{
    base.ReadValues(properties, operationContext);
    
    // Write the timestamp property into the same property on the entity
    Value.Timestamp = Timestamp;
}
{% endhighlight %}

Now we are up and running again.

[0]: /2014/01/07/using-the-entityadapter-for-azure-table-storage/
