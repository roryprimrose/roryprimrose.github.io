---
title: Mixed WCF endpoint bindings for hosted workflow services
categories : .Net
tags : WCF, WF, WIF
date: 2010-11-04 15:37:00 +10:00
---

I am working on a project where I have multiple hosted WF services. All the services were secured using federated security with WIF and a custom STS. This has all worked fine until I added a new service workflow that needed to be accessible by anonymous users. This is where things get a little tricky.

The WCF configuration for the hosted services looked a little like this before the anonymous service was added.

{% highlight xml linenos %}
<system.serviceModel>
    <protocolMapping>
        <add scheme=&quot;http&quot;
                binding=&quot;ws2007FederationHttpBinding&quot; />
    </protocolMapping> 
    <bindings>
        <ws2007FederationHttpBinding>
            <binding>
                <security mode=&quot;TransportWithMessageCredential&quot;>
                    <message establishSecurityContext=&quot;false&quot;>
                        <issuerMetadata address=&quot;https://localhost/JabiruSts/Service.svc/mex&quot; />
                        <claimTypeRequirements>
                            <!--Following are the claims offered by STS 'https://localhost/JabiruSts/Service.svc'. Add or uncomment claims that you require by your application and then update the federation metadata of this application.-->
                            <add claimType=&quot;http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name&quot;
                                    isOptional=&quot;true&quot; />
                            <add claimType=&quot;http://schemas.microsoft.com/ws/2008/06/identity/claims/role&quot;
                                    isOptional=&quot;true&quot; />
                        </claimTypeRequirements>
                    </message>
                </security>
            </binding>
        </ws2007FederationHttpBinding>
    </bindings>
    <behaviors>
        <serviceBehaviors>
            <behavior>
                <federatedServiceHostConfiguration />
                <!-- To avoid disclosing metadata information, set the value below to false and remove the metadata endpoint above before deployment -->
                <serviceMetadata httpGetEnabled=&quot;true&quot; />
                <!-- To receive exception details in faults for debugging purposes, set the value below to true.  Set to false before deployment to avoid disclosing exception information -->
                <serviceDebug includeExceptionDetailInFaults=&quot;true&quot; />
                <serviceCredentials>
                    <serviceCertificate findValue=&quot;DefaultApplicationCertificate&quot;
                                        storeLocation=&quot;LocalMachine&quot;
                                        storeName=&quot;My&quot;
                                        x509FindType=&quot;FindBySubjectName&quot; />
                </serviceCredentials>
                <errorHandler type=&quot;Neovolve.Jabiru.Server.Services.FaultHandler, Neovolve.Jabiru.Server.Services&quot; />
            </behavior>
        </serviceBehaviors>
    </behaviors>
    <extensions>
        <behaviorExtensions>
            <add name=&quot;errorHandler&quot;
                    type=&quot;Neovolve.Toolkit.Communication.ErrorHandlerElement, Neovolve.Toolkit&quot; />
            <add name=&quot;federatedServiceHostConfiguration&quot;
                    type=&quot;Microsoft.IdentityModel.Configuration.ConfigureServiceHostBehaviorExtensionElement, Microsoft.IdentityModel, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35&quot; />
        </behaviorExtensions>
    </extensions>
    <serviceHostingEnvironment multipleSiteBindingsEnabled=&quot;true&quot; />
</system.serviceModel>
{% endhighlight %}

The key thing to notice is that WCF4 uses default configuration values. These services use ws2007FederationHttpBinding in order to communicate with the STS but this is not the default binding for the HTTP stack. The configuration makes this work by defining the federation binding for HTTP using the protocolMapping/add element. The bindings/ws2007FederationBinding configuration does not specify a name as it represents the default configuration for that type of binding.

I attempted to modify this configuration with an explicit service configuration for my anonymous service that uses an IRegistration contract. There were a few roadblocks along the way, but here are the tips to get you across the line.

The first step is to define a configuration name against the xamlx file. This is accessed by viewing the properties of the workflow (the workflow, not any child activity).![image][0]

This configuration name needs to match to a WCF service configuration name. This configuration can then define a different binding than the default for the HTTP stack. It can then also define the behaviour to use for that endpoint binding.

The relevant change to the above configuration is to add the following.

{% highlight xml linenos %}
<services>
    <service name=&quot;Neovolve.Jabiru.Server.Services.Registration&quot;>
        <endpoint binding=&quot;basicHttpBinding&quot;
            bindingConfiguration=&quot;SecureBasicHttpBindingConfig&quot;
            contract=&quot;IRegistration&quot; />
    </service>
</services>
<bindings>
    <basicHttpBinding>
        <binding name=&quot;SecureBasicHttpBindingConfig&quot;>
            <security mode=&quot;Transport&quot;></security>
        </binding>
    </basicHttpBinding>
</bindings>
{% endhighlight %}

The second important step is to define the “correct” contract name. Ordinarily the contract name in WCF would be the [Namespace].[TypeName] value of the service contract. For some reason this only worked when I specified just the type name.

My hosted workflow services can now respond to a mix of endpoint bindings for the service.

[0]: //files/image_51.png
