---
title: Getting the X509Certificate from a remote HTTPS resource
categories : .Net
date: 2011-10-27 16:45:22 +10:00
---

How do you download the X509Certificate that secures a HTTPS channel? I want to do this for two reasons.

1. Identify the reason for a trust failure
1. Identify if the certificate is about to expire

I ran all sorts of searches on the net about how to get the certificate of the remote host. Unfortunately the results were not quite what I was after.

The answer is that the certificate is provided by HttpWebRequest.ServicePoint.Certificate. The tricky bit is that the certificate is only available once a response has come back from the host. This makes sense, but it is misleading that the certificate based on the response is stored against the original request object. It is also a little confusing that the certificate is available on the request when the attempt to get the response threw an exception (a failed certificate trust for example).

The following code shows how this works.

{% highlight csharp linenos %}
HttpWebRequest webRequest = HttpWebRequest.Create(address) as HttpWebRequest;
    
try
{
    if (webRequest == null)
    {
        throw new InvalidOperationException(&quot;Invalid request type encoutnered&quot;);
    }
    
    webRequest.Method = WebRequestMethods.Http.Head;
    
    // At this point webRequest.ServicePoint.Certificate == null
    using (WebResponse webResponse = webRequest.GetResponse())
    {
        // At this point certificate validation has passed
        // and webRequest.ServicePoint.Certificate != null
        HttpWebResponse response = webResponse as HttpWebResponse;
    
        if (response != null)
        {
            responseMessage = response.StatusDescription;
            outcome = DetermineOutcome(response);
        }
        else
        {
            throw new InvalidOperationException(&quot;Invalid response type encoutnered&quot;);
        }
    }
}
catch (WebException ex)
{
    AuthenticationException authFailure = ex.InnerException as AuthenticationException;
    
    if (authFailure != null)
    {
        responseMessage = authFailure.Message;
    
        if (ex.Status == WebExceptionStatus.TrustFailure)
        {
            // At this point certificate validation has failed
            // however webRequest.ServicePoint.Certificate != null
            // TODO: Identify reason for trust failure using webRequest.ServicePoint.Certificate
        }
    } 
    else
    {
        responseMessage = ex.Message;
    }
}
{% endhighlight %}


