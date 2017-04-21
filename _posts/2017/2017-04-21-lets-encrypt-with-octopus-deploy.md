---
title: Let's Encrypt with Octopus Deploy
categories: .Net
tags: 
date: 2017-04-21 14:19:00 +10:00
---

I [posted][0] a couple of months ago about some ideas I had for integrating Let's Encrypt certificates with the hosting of Octopus Deploy. The idea was to use many PowerShell scripts with a scheduled release to request a new certificate and install it against the Octopus Server. 

I never got to complete this and have since come up with a much easier solution, the reverse proxy. The way a reverse proxy works is by exposing one website as a wrapper around another (internal) website. This is a feature that IIS supports and works well with Octopus Deploy.

<!--more-->

There are a few steps to get this up and running.

1. Remove the HTTPS configuration in the Octopus server and use a new port for http that is not exposed in the firewall. 
2. Create a new IIS website.
3. Configure IIS with Let's Encrypt for which I used [Certify][1].
4. Configure IIS with dependencies to support reverse proxy. See [here][2] for the start of the blog series.
5. Add web.config that pipes requests to Octopus but also supports Let's Encrypt.

This last point is where things can get a little tricky. All http requests to the reverse proxy website should be piped over to the internal Octopus Deploy server. The exception to this is where requests come in from Let's Encrypt for validation of the new certificate request. 

The following web.config handles all this. The only thing you have to edit is to change ```[MY-OCTOPUS-FQDN]``` to your Octopus Deploy address (like octopus.mycompany.com).

```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
    <system.webServer>
        <rewrite>
            <rules>
                <rule name="LetsEncrypt" stopProcessing="true">
                    <match url=".well-known/acme-challenge/*" />
                    <conditions logicalGrouping="MatchAll" trackAllCaptures="false" />
                    <action type="None" />
                </rule>
                <rule name="Redirect HTTP Traffic" stopProcessing="true">
                    <match url="(.*)" />
                    <action type="Redirect" url="https://[MY-OCTOPUS-FQDN]/" />
                    <conditions>
                        <add input="{HTTPS}" pattern="^OFF$" />
                    </conditions>
                </rule>
                <rule name="ReverseProxyInboundRule1" stopProcessing="true">
                    <match url="(.*)" />
                    <action type="Rewrite" url="http://localhost:9000/{R:1}" />
                    <conditions>
                        <add input="{HTTP_HOST}" pattern="[MY-OCTOPUS-FQDN](.*)" />
                    </conditions>
                     <serverVariables>
                         <set name="HTTP_ACCEPT_ENCODING" value="" />
                     </serverVariables>
                </rule>
            </rules>
            <outboundRules>
                <rule name="ReverseProxyOutboundRule1" preCondition="ResponseIsHtml1">
                    <match filterByTags="A, Form, Img" pattern="^http(s)?://localhost:9000/(.*)" />
                    <action type="Rewrite" value="http{R:1}://[MY-OCTOPUS-FQDN]/{R:2}" />
                </rule>
                <preConditions>
                    <preCondition name="ResponseIsHtml1">
                        <add input="{RESPONSE_CONTENT_TYPE}" pattern="^text/html" />
                    </preCondition>
                </preConditions>
            </outboundRules>
        </rewrite>
        <httpProtocol>
            <customHeaders>
                <remove name="X-Powered-By" />
                <add name="strict-transport-security" value="max-age=16070400" />
            </customHeaders>
        </httpProtocol>
    </system.webServer>
</configuration>
```

This configuration does the following:

* Let through any Let's Encrypt requests to its own website
* Redirect HTTP requests to HTTPS
* Tell HTTPS response to only use HTTPS in the future via the HSTS header. 
* Forward HTTPS requests onto the internal Octopus Deploy server at ```http://localhost:9000```
* Rewrite any response from Octopus Deploy to change references of ```http://localhost:9000``` to ```https://[MY-OCTOPUS-FQDN]``` so that all resources are correctly referenced in the outside world.

[0]: /2017/02/01/octopus-deploy-lets-encrypt-dns/
[1]: https://certify.webprofusion.com/
[2]: https://blogs.msdn.microsoft.com/friis/2016/08/25/setup-iis-with-url-rewrite-as-a-reverse-proxy-for-real-world-apps/