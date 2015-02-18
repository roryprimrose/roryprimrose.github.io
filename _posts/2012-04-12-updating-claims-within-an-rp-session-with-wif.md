---
title: Updating claims within an RP session with WIF
categories : .Net
tags : Azure, WIF
date: 2012-04-12 22:17:14 +10:00
---

I have a scenario where a web application is using WIF to manage federated security. The system will get a SAML token from an STS for the authenticated user. The token is only going to contain the NameIdentifier claim (a typical Windows Live token for example). This means that the application itself needs to manage the account information related to an authenticated user.

The application will store the first name, last name and email address of the user. These values will be populated into the IClaimsPrincipal for an existing account using a custom ClaimsAuthenticationManager implementation.{% highlight csharp linenos %}
public override IClaimsPrincipal Authenticate(String resourceName, IClaimsPrincipal incomingPrincipal)
{
    IClaimsPrincipal principal = base.Authenticate(resourceName, incomingPrincipal);
    
    if (principal == null)
    {
        return null;
    }
    
    if (principal.Identity.IsAuthenticated == false)
    {
        return principal;
    }
    
    IClaimsIdentity identity = principal.Identity as IClaimsIdentity;
    
    if (identity == null)
    {
        return principal;
    }
    
    if (identity.Claims == null)
    {
        return principal;
    }
    
    IUnityContainer container = DomainContainer.Current;
    IAccountManager accountManager = container.Resolve<IAccountManager>();
    
    try
    {
        String userName = identity.GetClaimValue<String>(ClaimTypes.NameIdentifier);
        Account account = accountManager.Accounts.FirstOrDefault(x => x.UserName == userName);
    
        if (account != null)
        {
            identity.UpdateFromAccount(account);
        }
        else
        {
            // For anti forgery in MVC posts, there must be a name and we want it to be unique
            identity.UpsertClaim(ClaimTypes.Name, Guid.NewGuid().ToString());
        }
    }
    finally
    {
        container.Teardown(accountManager);
    }
    
    return principal;
}
{% endhighlight %}

This code will ensure that the principal executing against the web requests will contain the claims that match the users account. There are two scenarios that this does not cater for.

1. New users (there isn’t an account yet)
1. Existing users that update their account
    
The user will use an online form to submit changes to their account in either of these scenarios. The issue here is that the FedAuth security token stored in the request/response cookie is now out of date as it contains the serialized claims from when the authentication session was created, but these claims have now changed. The cookie is not updated to reflect any changes to the principal.

On a side note, I did find that simply updating the claims on the principal did cause the updated claims to be persisted for that web instance. I don’t understand where this logic sits as the FedAuth cookie is supposed to contain the serialized claims when [IsSessionMode = false][0]. Regardless, this doesn’t cover the case of NLB servers or when the application is hosted across multiple Azure web roles.

The stale token cookie meant that the application used the old claims when the web request was processed by a different web role instance (in the Azure dev fabric). The only two options to fix the stale claims when an account is updated appear to be:

1. Expire the authentication session
1. Force a new FedAuth cookie in the response
    
I don’t like option #1 because the application is essentially logging the user out of the system, causing a redirect to a protected resource to bounce them back out to the STS so that they will then be provided a new principal that has the current set of claims when they log back in (as populated by the ClaimsAuthenticationManager). My main issue with this is that the UX is inconsistent and confusing. The scenario is worse when the user has not told the STS to persist their authentication information therefore forcing them to manually log in to the application again.

This leaves me with having to update the FedAuth cookie mid-session from within the RP. The main reason that this is not ideal is because it is creating a coupling between the RP and the security implementation. It really is the lesser of two evils however and I think that a better UX wins over a purist architecture.

The next issue is that there is no clean API to use to write a new FedAuth cookie to the HttpResponse using the available FederatedAuthentication information. I came up with this extension method with the help of Reflector.{% highlight csharp linenos %}
using System;
using System.Diagnostics.Contracts;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Web;
    
/// <summary>
/// The <see cref="ClaimsPrincipalExtensions"/> class is used to provide extension methods for the <see cref="IClaimsPrincipal"/> interface.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Updates the session security token.
    /// </summary>
    /// <param name="principal">
    /// The principal. 
    /// </param>
    /// <param name="fam">
    /// The federation authentication module. 
    /// </param>
    /// <param name="sam">
    /// The session authentication module. 
    /// </param>
    public static void UpdateSessionSecurityToken(
        this IClaimsPrincipal principal, WSFederationAuthenticationModule fam, SessionAuthenticationModule sam)
    {
        Contract.Requires<ArgumentNullException>(principal != null);
        Contract.Requires<ArgumentNullException>(fam != null);
        Contract.Requires<ArgumentNullException>(sam != null);
    
        String cookieContext = GetSessionTokenContext(fam);
        TimeSpan configuredSessionTokenLifeTime = ConfiguredSessionTokenLifeTime(fam);
        DateTime validFrom = DateTime.UtcNow;
        DateTime validTo = validFrom.Add(configuredSessionTokenLifeTime);
        SessionSecurityToken securityToken = sam.CreateSessionSecurityToken(
            principal, cookieContext, validFrom, validTo, fam.PersistentCookiesOnPassiveRedirects);
    
        sam.WriteSessionTokenToCookie(securityToken);
    }
    
    /// <summary>
    /// Configureds the session token life time.
    /// </summary>
    /// <param name="fam">
    /// The fam. 
    /// </param>
    /// <returns>
    /// A <see cref="TimeSpan"/> instance. 
    /// </returns>
    private static TimeSpan ConfiguredSessionTokenLifeTime(WSFederationAuthenticationModule fam)
    {
        TimeSpan defaultTokenLifetime = SessionSecurityTokenHandler.DefaultTokenLifetime;
    
        if (fam.ServiceConfiguration == null)
        {
            return defaultTokenLifetime;
        }
    
        if (fam.ServiceConfiguration.SecurityTokenHandlers == null)
        {
            return defaultTokenLifetime;
        }
    
        SessionSecurityTokenHandler handler =
            fam.ServiceConfiguration.SecurityTokenHandlers[typeof(SessionSecurityToken)] as
            SessionSecurityTokenHandler;
    
        if (handler != null)
        {
            defaultTokenLifetime = handler.TokenLifetime;
        }
    
        return defaultTokenLifetime;
    }
    
    /// <summary>
    /// Gets the session token context.
    /// </summary>
    /// <param name="fam">
    /// The fam. 
    /// </param>
    /// <returns>
    /// A <see cref="String"/> instance. 
    /// </returns>
    private static String GetSessionTokenContext(WSFederationAuthenticationModule fam)
    {
        String sessionTokenContextPrefix = "(" + fam.GetType().Name + ")";
        String signOutUrl = WSFederationAuthenticationModule.GetFederationPassiveSignOutUrl(
            fam.Issuer, fam.SignOutReply, fam.SignOutQueryString);
    
        return sessionTokenContextPrefix + signOutUrl;
    }
}
{% endhighlight %}

This extension method will write a new FedAuth cookie to the response stream using the same implementation that WIF uses to create the cookie in the first place. This now allows the principal to provide the current set of claims to a web farm regardless of which web instance processes the request.

[0]: http://blogs.msdn.com/b/vbertocci/archive/2010/05/26/your-fedauth-cookies-on-a-diet-issessionmode-true.aspx
