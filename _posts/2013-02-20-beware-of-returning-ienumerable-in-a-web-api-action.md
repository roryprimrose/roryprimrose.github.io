---
title: Beware of returning IEnumerable in a Web Api action
categories: .Net
tags: ASP.Net, Tracing, Web Api
date: 2013-02-20 14:27:16 +10:00
---

I have been hitting an issue with MVC4 Web Api where my global error handling filters were not executed. The only [advice out there][0] is that they will not execute if the action throws an HttpResponseException. 

I have finally figured out that returning a lazy IEnumerable instance will also cause the global error handler to not execute. In fact, it won’t cause controller or action level exception filters to execute either. 

<!--more-->

Consider the following:

{% highlight csharp %}
namespace MyService
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.ModelBinding;
    
    public class TestController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage MyAction()
        {
            var data = BuildData();
    
            return Request.CreateResponse(HttpStatusCode.OK, data);
        }
    
        private static IEnumerable<int> BuildData()
        {
            yield return 1;
    
            throw new InvalidOperationException();
        }
    }
}
{% endhighlight %}

This will write information using [a registered ITraceWriter][1] implementation (which [has its problems][2]) but will not fire the exception filter attributes. The reason is that the part of the pipeline that evaluates the data to send in the response (therefore forcing the enumeration and hitting an exception)would presumably be beyond the part of the pipeline that is covered by the error handling.

The fix is to put a .ToList() call on the IEnumerable before providing the data to Request.CreateResponse. This will force the enumeration to execute within the controller and the exception from the enumeration to be thrown. This will then be handled by the exception filter attributes.

[0]: http://weblogs.asp.net/fredriknormen/archive/2012/06/11/asp-net-web-api-exception-handling.aspx
[1]: http://www.asp.net/web-api/overview/testing-and-debugging/tracing-in-aspnet-web-api
[2]: http://aspnetwebstack.codeplex.com/workitem/862
