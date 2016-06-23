---
title: Dynamic schema in Web API
categories: .Net
tags: ASP.Net
date: 2015-02-24 13:47:48 +11:00
---

Following on from the previous post, I want to prove that I can define a schema for metadata and have that metadata returned with core type information in a Web API.

For this POC, I will define the core type (DataEntry) and then dynamic metadata (JSON encoded data). I want the Web API to correctly handle the type information for this metadata and return the correct wire formatting.

This POC uses DynamicObject to be the bridge between the DataType, its metadata and its metadata schema. It provides the type information for Web API so that it can be correctly returned over the wire.

<!--more-->

{% highlight csharp %}
namespace DynamicService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Newtonsoft.Json;

    public class ValuesController : ApiController
    {
        public async Task<IHttpActionResult> Get()
        {
            var firstEntry = new DataEntry
            {
                Id = Guid.NewGuid(),
                Name = "First",
                Description = Guid.NewGuid().ToString(),
                Metadata = "{ \"Stuff\": \"148504DC-C57C-4EF2-9497-D7585D2C4998\", \"Created\": \"" + DateTimeOffset.UtcNow + "\", \"Active\": true }"
            };

            var firstSchema = new Dictionary<string, Type>
            {
                {
                    "Stuff", typeof(Guid)
                },
                {
                    "Created", typeof(DateTimeOffset)
                },
                {
                    "Active", typeof(bool)
                }
            };

            var dynamicFirstEntry = new DynamicDataEntry(firstEntry, firstSchema);

            var secondEntry = new DataEntry
            {
                Id = Guid.NewGuid(),
                Name = "Second",
                Description = Guid.NewGuid().ToString(),
                Metadata = "{ \"Price\": 1233, \"ResourceUri\": \"http://www.google.com\"}"
            };

            var secondSchema = new Dictionary<string, Type>
            {
                {
                    "Price", typeof(int)
                },
                {
                    "ResourceUri", typeof(Uri)
                }
            };

            var dynamicSecondEntry = new DynamicDataEntry(secondEntry, secondSchema);

            var result = new[] { dynamicFirstEntry, dynamicSecondEntry };

            return Ok(result);
        }

        private class DataEntry
        {
            public string Name { get; set; }

            public Guid Id { get; set; }

            public string Description { get; set; }

            public string Metadata { get; set; }
        }

        private class DynamicDataEntry : DynamicObject
        {
            private readonly DataEntry _entry;
            private readonly IDictionary<string, string> _entryMetadata; 
            private readonly IDictionary<string, Type> _schema;

            public DynamicDataEntry(DataEntry entry, IDictionary<string, Type> schema)
            {
                _entry = entry;
                _schema = schema;
                _entryMetadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(_entry.Metadata);
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                var members = new List<string>(_entryMetadata.Keys);

                members.Add("Id");
                members.Add("Name");
                members.Add("Description");  

                return members;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                if (binder.Name == "Metadata")
                {
                    result = null;
                    return false;
                }

                if (_schema.ContainsKey(binder.Name))
                {
                    var typeConverter = TypeDescriptor.GetConverter(_schema[binder.Name]);

                    result = typeConverter.ConvertFromString(_entryMetadata[binder.Name]);

                    return true;
                }

                var entryProperty = typeof (DataEntry).GetProperty(binder.Name);

                result = entryProperty.GetValue(_entry);
                return true;
            }
        }
    }
}
{% endhighlight %}

Web API renders the following data for this action.

{% highlight text %}
[
  {
    "stuff": "148504dc-c57c-4ef2-9497-d7585d2c4998",
    "created": "2015-02-24T02:51:19+00:00",
    "active": true,
    "id": "4ba569bc-a9fe-49de-af17-ed943433b4ca",
    "name": "First",
    "description": "f19c122d-029a-4b66-a72b-fc347d285229"
  },
  {
    "price": 1233,
    "resourceUri": "http://www.google.com",
    "id": "b3c369e4-16b9-4ce2-b998-155d82f66028",
    "name": "Second",
    "description": "8af95779-bd11-48b1-a337-62e03ba8e957"
  }
]
{% endhighlight %}

This all worked perfectly. The schema was correctly interpreted by Web API by using a TypeConverter to convert the metadata values according to the schema. You can see that the boolean and the integer are correct formatted in the JSON response.

I like this a lot.