---
title: Mixed WCF endpoint bindings for hosted workflow services
categories : .Net
tags : WCF, WF, WIF
date: 2010-11-04 15:37:00 +10:00
---

I am working on a project where I have multiple hosted WF services. All the services were secured using federated security with WIF and a custom STS. This has all worked fine until I added a new service workflow that needed to be accessible by anonymous users. This is where things get a little tricky.

The WCF configuration for the hosted services looked a little like this before the anonymous service was added.

    
        <system.serviceModel&gt;
            <protocolMapping&gt;
                <add scheme=&quot;http&quot;
                     binding=&quot;ws2007FederationHttpBinding&quot; /&gt;
            </protocolMapping&gt; 
            <bindings&gt;
                <ws2007FederationHttpBinding&gt;
                    <binding&gt;
                        <security mode=&quot;TransportWithMessageCredential&quot;&gt;
                            <message establishSecurityContext=&quot;false&quot;&gt;
                                <issuerMetadata address=&quot;https://localhost/JabiruSts/Service.svc/mex&quot; /&gt;
                                <claimTypeRequirements&gt;
                                    <!--Following are the claims offered by STS 'https://localhost/JabiruSts/Service.svc'. Add or uncomment claims that you require by your application and then update the federation metadata of this application.--&gt;
                                    <add claimType=&quot;http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name&quot;
                                         isOptional=&quot;true&quot; /&gt;
                                    <add claimType=&quot;http://schemas.microsoft.com/ws/2008/06/identity/claims/role&quot;
                                         isOptional=&quot;true&quot; /&gt;
                                </claimTypeRequirements&gt;
                            </message&gt;
                        </security&gt;
                    </binding&gt;
                </ws2007FederationHttpBinding&gt;
            </bindings&gt;
            <behaviors&gt;
                <serviceBehaviors&gt;
                    <behavior&gt;
                        <federatedServiceHostConfiguration /&gt;
                        <!-- To avoid disclosing metadata information, set the value below to false and remove the metadata endpoint above before deployment --&gt;
                        <serviceMetadata httpGetEnabled=&quot;true&quot; /&gt;
                        <!-- To receive exception details in faults for debugging purposes, set the value below to true.  Set to false before deployment to avoid disclosing exception information --&gt;
                        <serviceDebug includeExceptionDetailInFaults=&quot;true&quot; /&gt;
                        <serviceCredentials&gt;
                            <serviceCertificate findValue=&quot;DefaultApplicationCertificate&quot;
                                                storeLocation=&quot;LocalMachine&quot;
                                                storeName=&quot;My&quot;
                                                x509FindType=&quot;FindBySubjectName&quot; /&gt;
                        </serviceCredentials&gt;
                        <errorHandler type=&quot;Neovolve.Jabiru.Server.Services.FaultHandler, Neovolve.Jabiru.Server.Services&quot; /&gt;
                    </behavior&gt;
                </serviceBehaviors&gt;
            </behaviors&gt;
            <extensions&gt;
                <behaviorExtensions&gt;
                    <add name=&quot;errorHandler&quot;
                         type=&quot;Neovolve.Toolkit.Communication.ErrorHandlerElement, Neovolve.Toolkit&quot; /&gt;
                    <add name=&quot;federatedServiceHostConfiguration&quot;
                         type=&quot;Microsoft.IdentityModel.Configuration.ConfigureServiceHostBehaviorExtensionElement, Microsoft.IdentityModel, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35&quot; /&gt;
                </behaviorExtensions&gt;
            </extensions&gt;
            <serviceHostingEnvironment multipleSiteBindingsEnabled=&quot;true&quot; /&gt;
        </system.serviceModel&gt;{% endhighlight %}

The key thing to notice is that WCF4 uses default configuration values. These services use ws2007FederationHttpBinding in order to communicate with the STS but this is not the default binding for the HTTP stack. The configuration makes this work by defining the federation binding for HTTP using the protocolMapping/add element. The bindings/ws2007FederationBinding configuration does not specify a name as it represents the default configuration for that type of binding.

I attempted to modify this configuration with an explicit service configuration for my anonymous service that uses an IRegistration contract. There were a few roadblocks along the way, but here are the tips to get you across the line.

The first step is to define a configuration name against the xamlx file. This is accessed by viewing the properties of the workflow (the workflow, not any child activity).![image][0]

This configuration name needs to match to a WCF service configuration name. This configuration can then define a different binding than the default for the HTTP stack. It can then also define the behaviour to use for that endpoint binding.

The relevant change to the above configuration is to add the following.

    <services&gt;
        <service name=&quot;Neovolve.Jabiru.Server.Services.Registration&quot;&gt;
            <endpoint binding=&quot;basicHttpBinding&quot;
                bindingConfiguration=&quot;SecureBasicHttpBindingConfig&quot;
                contract=&quot;IRegistration&quot; /&gt;
        </service&gt;
    </services&gt;
    <bindings&gt;
        <basicHttpBinding&gt;
            <binding name=&quot;SecureBasicHttpBindingConfig&quot;&gt;
                <security mode=&quot;Transport&quot;&gt;</security&gt;
            </binding&gt;
        </basicHttpBinding&gt;
    </bindings&gt;{% endhighlight %}

The second important step is to define the “correct” contract name. Ordinarily the contract name in WCF would be the [Namespace].[TypeName] value of the service contract. For some reason this only worked when I specified just the type name.

My hosted workflow services can now respond to a mix of endpoint bindings for the service.

[0]: //blogfiles/image_51.png
