---
title: Posting dynamic data to Web API
categories: .Net
tags: ASP.Net
date: 2015-02-25 11:17:57 +11:00
---

The previous two posts ([here][0] and [here][1]) have looked at a POC for returning dynamic data from Web API and then adding a defined schema to a semi-dynamic object. This post is the next POC that looks at how to POST that data to the Web API.

The design goals are similar to the previous POCs. There is a type that has some known properties and then contains JSON serialized metadata for all the remaining dynamic properties. The storage and retrieval of the metadata should conform to some known schema for that metadata.

The code here uses JObject from Newtonsoft.JSON as the mechanism of making the dynamic data available to the controller action. We then need to separate out the known properties from the metadata properties and convert all of them to the correct type.

<!--more-->

{% highlight csharp %}
using System.Threading.Tasks;
using System.Web.Http;

namespace DynamicService.Controllers
{
    public class ValuesController : ApiController
    {
        private T ReadProperty<T>(IDictionary<string, Type> schema, string property, JObject entry)
        {
            var schemaKey = schema.Keys.FirstOrDefault(x => x.Equals(property, StringComparison.OrdinalIgnoreCase));

            if (schemaKey == null)
            {
                return default(T);
            }

            var entryProperty =
                entry.Properties().FirstOrDefault(x => x.Name.Equals(property, StringComparison.OrdinalIgnoreCase));

            if (entryProperty == null)
            {
                return default(T);
            }

            var value = entryProperty.Value.Value<string>();
            var schemaType = schema[schemaKey];
            var typeConverter = TypeDescriptor.GetConverter(schemaType);

            return (T)typeConverter.ConvertFromString(value);
        }

        public async Task<IHttpActionResult> Post(JObject entry)
        {
            var dataEntryProperties = typeof(DataEntry).GetProperties();
            var expectedSchema = new Dictionary<string, Type>
            {
                {
                    "Stuff", typeof(Guid)
                },
                {
                    "Created", typeof(DateTimeOffset)
                },
                {
                    "Active", typeof(bool)
                },
                {
                    "Population", typeof(int)
                }
            };

            // Augment the schema with the properties from DataEntry to support common data reading
            dataEntryProperties.ForEach(x => expectedSchema.Add(x.Name, x.PropertyType));

            var dataEntry = new DataEntry
            {
                Id = ReadProperty<Guid>(expectedSchema, "Id", entry),
                Name = ReadProperty<string>(expectedSchema, "Name", entry)
            };

            var dataEntryPropertyNames = dataEntryProperties.Select(x => x.Name);
            var metadataProperties = from x in entry.Properties()
                where dataEntryPropertyNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase) == false
                select x;
            var metadataSource = new Dictionary<string, object>();

            foreach (var metadataProperty in metadataProperties)
            {
                var schemaKey = expectedSchema.Keys.FirstOrDefault(x => x.Equals(metadataProperty.Name, StringComparison.OrdinalIgnoreCase));
                var propertyValue = ReadProperty<object>(expectedSchema, metadataProperty.Name, entry);
                
                metadataSource.Add(schemaKey, propertyValue);
            }

            var metadata = JsonConvert.SerializeObject(metadataSource);

            dataEntry.Metadata = metadata;

            return Ok(dataEntry);
        }

        private class DataEntry
        {
            public string Name { get; set; }

            public Guid Id { get; set; }

            public string Description { get; set; }

            public string Metadata { get; set; }
        }
    }
}
{% endhighlight %}

We can then send the following object to the Web API.

{% highlight csharp %}
request.AddObject(new
{
    id = Guid.NewGuid(),
    name = Guid.NewGuid().ToString(),
    population = 12332111,
    active = true,
    stuff = Guid.NewGuid(),
    created = DateTimeOffset.UtcNow
});
{% endhighlight %}

The action then returns the following echo response.

{% highlight text %}
{
  "name": "408e63cd-5b3f-46fc-9f15-77083922ace9",
  "id": "b9fccf1b-dc13-4432-8362-8009e659c489",
  "description": null,
  "metadata": "{\"Population\":12332111,\"Active\":true,\"Stuff\":\"322f63c4-1fa5-47b8-bde6-aecb305495f7\",\"Created\":\"2015-02-25T00:11:40+00:00\"}"
}
{% endhighlight %}

The key outcome here is that the metadata is correctly serialized for the int and bool properties and all information is stored in the correct place.

[0]: /2015/02/24/dynamic-types-in-web-api/
[1]: /2015/02/24/dynamic-schema-in-web-api/
