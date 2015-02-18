---
title: CodeCampOz: Not a WIF of federation
categories : .Net
tags : WIF
date: 2010-11-21 14:17:00 +10:00
---

My presentation of [Not a WIF of federation][0] is done and dusted for [CodeCampOz 2010][1]. I got a little cut short by the early arrival of pizza's so there are a couple of things I had to skip.

One of the important points I was not able to make is that the demo code displayed is not production ready. It's not that the code itself is flawed rather that the certificates used were development certificates. It is important to secure your WIF implementation with production quality certificates in order to ensure that system security is maintained.

Identity delegation was the last demo that I presented. I didn't get the opportunity to show the code that was changed in the STS application to make this work. The code in the STS project had not be changed up until this point. The GetOutputClaimsIdentity method of the CustomSecurityTokenService class simply returned a new ClaimsIdentity created from the provided principal. The delegated identity scenario needs to explicitly cater for the ActAs scenario by attaching the delegating identity to the Actor property on the delegated identity being returned.

{% highlight csharp linenos %}
protected override IClaimsIdentity GetOutputClaimsIdentity(IClaimsPrincipal principal, RequestSecurityToken request, Scope scope)
{
    if (null == principal)
    {
        throw new ArgumentNullException("principal");
    }
     
    IClaimsIdentity claimsIdentity = new ClaimsIdentity(principal.Identity);
    
    // If there is an ActAs token in the RST, return a copy of it as the top-most identity
    // and put the caller's identity into the Actor property of this identity.
    if (request.ActAs != null)
    {
        IClaimsIdentity actAsSubject = request.ActAs.GetSubject()[0];
        IClaimsIdentity actAsIdentity = actAsSubject.Copy();
    
        // Find the last actor in the actAs identity
        IClaimsIdentity lastActor = actAsIdentity;
        while (lastActor.Actor != null)
        {
            lastActor = lastActor.Actor;
        }
    
        // Set the caller's identity as the last actor in the delegation chain
        lastActor.Actor = claimsIdentity;
    
        // Return the actAsIdentity instead of the caller's identity in this case
        claimsIdentity = actAsIdentity;
    }
    
    return claimsIdentity;
}
{% endhighlight %}

The RP that receives the delegated identity can now look at the Actor property on the IClaimsIdentity to see if the RP is being invoked by a delegating identity.

You can download the source code for the demos and the PowerPoint deck from the link below. The solution in the zip requires that you have WIF installed and the default development certificates configured by FedUtil. You can do this by creating a new project and running FedUtil and adding an STS/IP project. The other requirement is that you have a localhost SSL certificate configured in IIS.

[CCOZ2010-NotAWIFOfFederation.zip (1.06 mb)][2]

[0]: /post/2010/07/18/Speaking-at-CodeCampOz.aspx
[1]: http://www.codecampoz.com
[2]: /blogfiles/2010%2f11%2fCCOZ2010-NotAWIFOfFederation.zip
