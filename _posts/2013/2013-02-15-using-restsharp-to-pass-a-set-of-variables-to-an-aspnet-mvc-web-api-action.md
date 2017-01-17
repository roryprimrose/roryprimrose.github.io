---
title: Using RestSharp to pass a set of variables to an ASP.Net MVC Web API action
categories: .Net
tags: ASP.Net
date: 2013-02-15 16:23:53 +10:00
---

Looks like a lot of people hit this issue and come up with lots of "interesting" solutions to get this to work. The answer is surprisingly simple however.

Assume that the controller action is like the following:

<!--more-->

```csharp
[HttpGet]
public HttpResponseMessage MyAction(
    [ModelBinder]List<string> referenceNames, DateTime startDate, DateTime endDate)
{
}
```

How do you get RestSharp to send the set of strings to the action so that they are deserialized correctly? The answer is like this.

```csharp
var client = new RestClient(Config.ServiceAddress);
var request = new RestRequest(ActionLocation, Method.GET);
    
request.RequestFormat = DataFormat.Json;
request.AddParameter("ReferenceNames", "NameA");
request.AddParameter("ReferenceNames", "NameB");
request.AddParameter("StartDate", DateTime.Now.AddYears(-2).ToString("D"));
request.AddParameter("EndDate", DateTime.Now.AddYears(2).ToString("D"));
```

There are two things that make this work:

1. Attribute the action parameter with [ModelBinder] – see [here][0] for more info
1. Make multiple calls to request.AddParameter for the same parameter name
    
Do this and you should be good to go.

[0]: http://blogs.msdn.com/b/jmstall/archive/2012/04/16/how-webapi-does-parameter-binding.aspx
