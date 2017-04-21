---
title: Let's Encrypt with SonarQube
categories: .Net
tags: 
date: 2017-04-21 15:49:00 +10:00
---

Using the same concept of putting [Octopus Deploy behind a reverse proxy][0], I want to wrap SonarQube behind an IIS reverse proxy so I can use Let's Encrypt certificates to host it. 

<!--more-->

There are a few steps to get this up and running.

1. Create a new IIS website.
2. Configure IIS with Let's Encrypt for which I used [Certify][1].
3. Configure IIS with dependencies to support reverse proxy. See [here][2] for the start of the blog series.
4. Add web.config that pipes requests to SonarQube but also supports Let's Encrypt.

This last point is where things can get a little tricky. All http requests to the reverse proxy website should be piped over to the internal SonarQube server. The exception to this is where requests come in from Let's Encrypt for validation of the new certificate request. 

The following web.config handles all this.

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
                <rule name="ReverseProxyInboundRule1" stopProcessing="true">
                    <match url="(.*)" />
                    <action type="Rewrite" url="http://localhost:9000/{R:1}" />
                    <serverVariables>
                        <set name="X_FORWARDED_PROTO" value="https" />
                    </serverVariables>
                </rule>
            </rules>
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
* Tell HTTPS response to only use HTTPS in the future via the HSTS header. 
* Forward HTTPS requests onto the internal SonarQube server at ```http://localhost:9000```

[0]: /2017/04/21/lets-encrypt-with-octopus-deploy/
[1]: https://certify.webprofusion.com/
[2]: https://blogs.msdn.microsoft.com/friis/2016/08/25/setup-iis-with-url-rewrite-as-a-reverse-proxy-for-real-world-apps/