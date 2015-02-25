---
title: Preventing passive federation for Web Api under a MVC4 website
categories : .Net
tags : ASP.Net, Azure, Web Api, WIF
date: 2013-05-02 12:53:08 +10:00
---

I have an ASP.Net MVC4 website that is running passive federation to Azure ACS. This works great for standard http requests from a browser. I have now added some web api services to the same solution but hit issues with acceptance testing.

I have secured my api action using the following.

{% highlight csharp %}
[Authorize(Roles = Role.Administrator)]
public class AdminReportController : ApiController
{
}
{% endhighlight %}

This achieves the objective but the acceptance test that verifies the security of the controller fails. It fails because it gets a 302 response on the unauthenticated call rather than the expected 401. If the http client running the REST request follows the 302, it will ultimately end up with a 200 response from the ACS authentication form. The overall outcome is achieved because the request denied the anonymous user, but the status code returned to the client and the body content does not reflect this.

The only way to get around this seems to be to hijack the WSFederationAuthenticationModule to tell it not to run passive redirection if the request is for the web api.

{% highlight csharp %}
public class WebApiSafeFederationAuthenticationModule : WSFederationAuthenticationModule
{
    protected override void OnAuthorizationFailed(AuthorizationFailedEventArgs e)
    {
        Guard.That(() => e).IsNotNull();
    
        base.OnAuthorizationFailed(e);
    
        if (e.RedirectToIdentityProvider == false)
        {
            return;
        }
    
        string requestedUrl = HttpContext.Current.Request.Url.ToString();
    
        if (requestedUrl.IndexOf("/api/", StringComparison.OrdinalIgnoreCase) > -1)
        {
            // We don't want web api requests to redirect to the STS
            e.RedirectToIdentityProvider = false;
    
            return;
        }
    }
}
{% endhighlight %}

This now allows web api to return its default pipeline for unauthorised requests.

I put a shout out on Twitter regarding this issue to which [Brock Allen][2] quickly confirmed my solution.

[0]: https://twitter.com/roryprimrose
[1]: http://t.co/XDnV6iANga
[2]: https://twitter.com/BrockLAllen/status/329785608983674880
