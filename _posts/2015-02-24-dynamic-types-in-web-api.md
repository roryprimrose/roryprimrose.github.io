---
title: Dynamic types in Web API
categories : .Net
tags : ASP.Net
date: 2015-02-24 13:39:57 +11:00
---

I am looking at the possibility of using dynamic types in a Web API for my current client. They have a need to return data that mostly has the same structure. I want to run a few POCs to see if this is possible.

Rather than store each data type in it's own data structure (such as a database table), storing core type information and then dynamic fields would be a very flexible outcome. Is it something that Web API will support though?

The first POC is to determine whether Web API will render dynamic types of differing structures. At the same time, can this be done with a Task based API. Here is the first test.

<!--more-->

{% highlight csharp %}
using System.Threading.Tasks;
using System.Web.Http;

namespace DynamicService.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public async Task<IHttpActionResult> Get()
        {
            dynamic first = new
            {
                FirstName = "Sam",
                LastName = "Goodall"
            };

            dynamic second = new
            {
                Location = "SomewhereVille",
                Populate = 1233323
            };

            dynamic result = new[] { first, second };

            return Ok(result);
        }
    }
}
{% endhighlight %}

Web API renders the following data for this action.

{% highlight text %}
[
  {
    "firstName": "Sam",
    "lastName": "Goodall"
  },
  {
    "location": "SomewhereVille",
    "populate": 1233323
  }
]
{% endhighlight %}

So yes, Web API will return dynamic types. This isn't a surprise given that it needs to support any model type that we would normally throw at it.