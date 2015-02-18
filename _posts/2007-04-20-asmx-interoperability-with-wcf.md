---
title: ASMX interoperability with WCF
categories : .Net
tags : ASP.Net, WCF
date: 2007-04-20 15:42:47 +10:00
---

If you have an ASMX client, you can get it to call a WCF endpoint with some restrictions. You have to use the basicHttpBinding on the WCF service and the service implementation (or contract) needs to be decorated with the XmlSerializerFormat attribute.

I have encountered a problem under SSL though. I am wanting to use username/basic authentication with the service over SSL. [This article][0] makes the following reference:

_> The easiest straightforward way for a successful interoperability scenario is to leverage on transport-layer security. This also means that a properly configured WCF implementation can interoperate with a__[> Basic Profile 1.0][1]__>  compliant ASP.NET Web Service (ASMX) that is currently deployed via SSL / HTTPS as well as with a WSE 2.0 service or client and likewise._

_> WCF has a standard binding called “<basicHttpBinding&gt;” which derives its name from the Basic Profile specifications. There is a security mode within this binding called “TransportWithMessageCredential”. You can choose either a transport or a message credentials in this security mode. Setting it to <message clientCredentialType="UserName"/&gt; uses Transport-Level Security (SSL / HTTPS) with SOAP-Level Username token security credentials. This is in accordance with the__[> WSS SOAP Message Security Username Token Profile 1.0][2]__>  and it implements WSS SOAP Message Security 1.0 specification for username/password (for client authentication) over HTTPS (for privacy)._

My WCF client consumes the endpoint correctly as the authenticated user. However, when using an ASMX web reference, I end up with the following error:

_> System.Web.Services.Protocols.SoapHeaderException: An error occurred when verifying security for the message._

So far, I haven't got a solution.

Update:

Still no solution, but I have read in more places that indicate this should be fine. The following articles refer to the same kind of setup I am running. The only difference I that my client is an asmx client, but with a basicHttpBinding, this should be fine.

MSDN Library - [Bindings and Security][3]:

_> BasicHttp_

_> In code, use_[_> BasicHttpBinding_][4]_> ; in configuration, use the_[_> basicHttpBinding Element_][5]_> .__> This binding is designed to be used with a range of existing technologies, such as the following:_* _> ASMX (version 1) Web services._
* _> Web Service Enhancements (WSE) applications._
* _> Basic Profile as defined in the WS-I specification (http://www.ws-i.org)._
* _> Basic security profile as defined in WS-I.__> By default, this binding is not secure. It is designed to interoperate with ASMX services. When security is enabled, the binding is designed for seamless interoperation with IIS security mechanisms, such as Basic authentication, Digest, and Integrated Windows security. For more information, see_[_> Transport Security Overview_][6]_> . This binding supports the following:_* _> HTTPS transport security._
* _> HTTP Basic authentication._
* _> WS-Security._

[William Tay][7] - [Enterprise .NET Community: Securing your WCF Service][8][Michele Leroux Bustamante][9] - [Fundamentals of WCF Security][10]

[0]: http://wcf.netfx3.com/content/WindowsCommunicationFoundationWCFInteroperabilityandMigrationwithWSE20.aspx
[1]: http://www.ws-i.org/Profiles/BasicProfile-1.0-2004-04-16.html
[2]: http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0.pdf
[3]: http://msdn2.microsoft.com/en-us/library/ms731172.aspx
[4]: http://msdn2.microsoft.com/system.servicemodel.basichttpbinding.aspx
[5]: http://msdn2.microsoft.com/ms731361.aspx
[6]: http://msdn2.microsoft.com/ms729700.aspx
[7]: http://www.softwaremaker.net/blog/
[8]: http://www.theserverside.net/tt/articles/showarticle.tss?id=SecuringWCFService
[9]: http://www.dasblonde.net/
[10]: http://www.code-magazine.com/articleprint.aspx?quickid=0611051
