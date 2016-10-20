---
title: Dynamic realm discovery for federated authentication
tags: Azure
date: 2013-02-14 22:39:08 +10:00
---

I have a web role (RP) running in Windows Azure that uses ACS 2.0 as the identity provider (IP). The web role is configured with a certificate to work with the authentication negotiation and subsequent security session. The certificate supports both domain.com and www.domain.com. The issue is that the federation authentication configuration of the web role can only specify one realm and the realm attribute is a required value.

<!--more-->

```xml
<wsFederation passiveRedirectEnabled="true" issuer="http://[addressOfAcs]" realm="http://www.domain.com" requireHttps="true" />
```

This works great if the user is browsing on www.domain.com and then goes through the authentication process. A security token will be issued for www.domain.com to which the user is redirected back to. The RP will then validate that the token was issued to the configured audience uri. These thankfully allow multiple addresses to be specified.

```xml
<audienceUris>
    <add value="http://www.domain.com" />
    <add value="http://domain.com" />
</audienceUris>
```

The problem is when you want to use the www-less address or any other host header for that matter. In this case, the user is browsing domain.com and goes through the authentication process. The token will be issued for www.domain.com but the user is then redirected back to the original address under the domain.com location. An exception is then thrown at this point.

> System.IdentityModel.Services.FederationException : ID3206: A SignInResponse message may only redirect within the current web application: 'http://domain.com/' is not allowed.
> Stack trace at System.IdentityModel.Services.WSFederationAuthenticationModule.SignInWithResponseMessage(HttpRequestBase\ request)    
> at System.IdentityModel.Services.WSFederationAuthenticationModule.OnAuthenticateRequest(Object\ sender, EventArgs args) 
> at System.Web.HttpApplication.SyncEventExecutionStep.System.Web.HttpApplication.IExecutionStep.Execute() 
> at System.Web.HttpApplication.ExecuteStep(IExecutionStep step, Boolean& completedSynchronously)
 
The fix here is to put together some dynamic realm discovery logic. Creating a custom WSFederationAuthenticationModule class provides some good hooks into the authentication process. We want to override OnRedirectingToIdentityProvider and set the realm as per the location of the current request. 

For example:

```csharp
namespace MyApplication.Web.Security
{
    using System;
    using System.IdentityModel.Services;
    using System.Security.Principal;
    using System.Web;
    using Seterlund.CodeGuard;
    
    public class DynamicRealmFederationAuthenticationModule : WSFederationAuthenticationModule
    {
        protected override void OnRedirectingToIdentityProvider(RedirectingToIdentityProviderEventArgs e)
        {
            e.SignInRequestMessage.Realm = DetermineDynamicRealm();
    
            base.OnRedirectingToIdentityProvider(e);
        }
    
        private static Uri BuildRequestedAddress(HttpRequest request)
        {
            Guard.That(() => request).IsNotNull();
            Guard.That(() => request.Headers).IsNotNull();
            Guard.That(() => request.Url).IsNotNull();
    
            var originalRequest = request.Url;
            var serverName = request.Headers["Host"];
            var address = string.Concat(originalRequest.Scheme, "://", serverName);
    
            address += originalRequest.PathAndQuery;
    
            return new Uri(address);
        }
    
        private string DetermineDynamicRealm()
        {
            // Set the realm to be the current domain name
            string realm;
            const string SecureHttp = "https://";
            var hostUri = BuildRequestedAddress(HttpContext.Current.Request);
            var port = string.Empty;
    
            if (Realm.StartsWith(SecureHttp, StringComparison.OrdinalIgnoreCase))
            {
                realm = SecureHttp;
    
                if (hostUri.Port != 443)
                {
                    port = ":" + hostUri.Port;
                }
            }
            else
            {
                realm = "http://";
    
                if (hostUri.Port != 80)
                {
                    port = ":" + hostUri.Port;
                }
            }
    
            realm += hostUri.Host + port;
    
            return realm;
        }
    }
}
```

A couple of things to note about this code. 

Firstly, the DetermineDynamicRealm method could have been streamlined, but I also needed it to cater for my location development environment. The local environment uses the Azure SDK emulator for the web role and a custom website project as a development STS. Both the web role and the development STS use straight http as well as custom ports. The DetermineDynamicRealm method logic caters for both local development and production deployment.

Secondly, the BuildRequestedAddress method uses request headers to figure out the port of the request from the browser. This is because the web role sits behind a load balancer that does some trickery with ports. We need to look at the headers of the request to determine the port as the browser sees it rather than how the request on the current HttpContext sees it.

Next up, the web.config needs to be updated to use this module rather than the module that comes out of the box.

```xml
<add name="WSFederationAuthenticationModule" type="MyApplication.Web.Security.DynamicRealmFederationAuthenticationModule, MyApplication.Web" preCondition="managedHandler" />
```

The only remaining step is to set up ACS with an additional RP configuration for domain.com. You should now be able to authenticate and be redirected back to either domain.com or www.domain.com. 


