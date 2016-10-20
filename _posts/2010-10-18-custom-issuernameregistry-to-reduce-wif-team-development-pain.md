---
title: Custom IssuerNameRegistry to reduce WIF team development pain
categories: .Net
tags: Extensibility, WIF
date: 2010-10-18 13:54:33 +10:00
---

I have been implementing WIF into my [hosted synchronization project][0] over recent months. One of the issues that I keep hitting with WIF is managing STS certificates between multiple machines. In my case I have a desktop and a laptop that I use for development. The same issue outlined here applies to working in a development team.

The WIF SDK makes it easy to get up and running with an STS. The wizard application creates an STS project and development certificates that are then integrated into your Visual Studio solution. The certificates are created on the local machine and are specific to that machine. One is the signing certificate with the default name of STSTestCert and the other is the encrypting certificate with the default name of DefaultApplicationCertificate.

<!--more-->

The WIF configuration usually refers to these certificates using the subject distinguished name of the certificate. This will work where multiple development machines use certificates with the same subject where those certificates where created on each machine. Unfortunately the configuration for the Relying Party application identifies the trusted issuer certificate using a thumbprint. This thumbprint will be different across each machine.

```xml
<service name=&quot;Neovolve.Jabiru.Server.Service.ExchangeSession&quot;>
    <audienceUris>
        <add value=&quot;https://localhost/Jabiru/DataExchange.svc&quot; />
    </audienceUris>
    <issuerNameRegistry type=&quot;Microsoft.IdentityModel.Tokens.ConfigurationBasedIssuerNameRegistry, Microsoft.IdentityModel, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35&quot;>
        <trustedIssuers>
            <!-- This is the thumbprint for the certificate used sign the certificate - STSTestCert -->
            <add thumbprint=&quot;3E8D41EA2AF035D352D07FE46AACE352AD2F32B0&quot;
                    name=&quot;https://localhost/JabiruSts/Service.svc&quot; />
        </trustedIssuers>
    </issuerNameRegistry>
</service>
```

The above configuration is for one of the services in my project. The configuration identifies the trusted issuer using a specific certificate thumbprint.

One solution to this issue is to copy the certificates around each development machine. I’m not a fan of this solution as trying to use local certificates on other machines is problematic. My preference is to have each development machine generate their own certificates using the same subject names. If required this functionality could be easily wrapped up in a batch file that is part of the solution. This method of working with local certificates is the same as a development team getting IIS on each workstation to create self-signed certificates with the same name (localhost for example).

There is an alternative solution however as WIF provides an extensibility point that allows for a different implementation. The type attribute of the issuerNameRegistry node in the above configuration above identifies the type that provides the IssuerNameRegistry class for the service. There is only one implementation provided with WIF which is ConfigurationBasedIssuerNameRegistry. This class is hard-coded to only deal with certificate thumbprints. Creating a type that can handle more certificate matching options using [X509FindType][1] will be the answer to this restriction.

```csharp
namespace Neovolve.Jabiru.Server.Security
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    
    public struct IssuerCertificateMapping
    {
        public X509FindType FindType;
    
        public String FindValue;
    
        public String IssuerName;
    }
}
```

The IssuerCertificateMapping struct will define the relationship between an issuer name and the certificate matching criteria. The ConfiguredCertificateIssuerNameRegistry class will read the issuer configuration and provide the logic for matching against the security token certificate when the GetIssuerName is invoked.

```csharp
namespace Neovolve.Jabiru.Server.Security
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.IdentityModel.Tokens;
    using System.Security.Cryptography.X509Certificates;
    using System.Xml;
    using Microsoft.IdentityModel.Tokens;
    using Neovolve.Jabiru.Server.Security.Properties;
    
    public class ConfiguredCertificateIssuerNameRegistry : IssuerNameRegistry
    {
        private readonly List<IssuerCertificateMapping> _trustedIssuers = new List<IssuerCertificateMapping>();
    
        public ConfiguredCertificateIssuerNameRegistry()
        {
        }
    
        public ConfiguredCertificateIssuerNameRegistry(XmlNodeList customConfiguration)
        {
            if (customConfiguration == null)
            {
                throw new ArgumentNullException(&quot;customConfiguration&quot;);
            }
    
            for (Int32 index = 0; index < customConfiguration.Count; index++)
            {
                XmlNode node = customConfiguration[index];
    
                if (node.Name != &quot;trustedIssuers&quot;)
                {
                    continue;
                }
    
                LoadIssueConfiguration(node);
            }
        }
    
        public override String GetIssuerName(SecurityToken securityToken)
        {
            if (securityToken == null)
            {
                throw new ArgumentNullException(&quot;securityToken&quot;);
            }
    
            X509SecurityToken token = securityToken as X509SecurityToken;
    
            if (token == null)
            {
                return null;
            }
    
            X509Certificate2 tokenCertificate = token.Certificate;
    
            if (tokenCertificate == null)
            {
                return null;
            }
    
            for (Int32 index = 0; index < TrustedIssuers.Count; index++)
            {
                IssuerCertificateMapping mapping = TrustedIssuers[index];
    
                if (CertificateMatchesConfigurationItem(tokenCertificate, mapping))
                {
                    return mapping.IssuerName;
                }
            }
    
            return null;
        }
    
        private static Boolean CertificateMatchesConfigurationItem(X509Certificate2 tokenCertificate, IssuerCertificateMapping mapping)
        {
            String findValue = mapping.FindValue;
    
            switch (mapping.FindType)
            {
                case X509FindType.FindByThumbprint:
    
                    return tokenCertificate.Thumbprint == findValue;
    
                case X509FindType.FindBySubjectName:
    
                    return StripToCommonName(tokenCertificate.SubjectName) == findValue;
    
                case X509FindType.FindBySubjectDistinguishedName:
    
                    return tokenCertificate.Subject == findValue;
    
                case X509FindType.FindByIssuerName:
    
                    return StripToCommonName(tokenCertificate.IssuerName) == findValue;
    
                case X509FindType.FindByIssuerDistinguishedName:
    
                    return tokenCertificate.IssuerName.Name == findValue;
    
                case X509FindType.FindBySerialNumber:
    
                    return tokenCertificate.SerialNumber == findValue;
    
                    // case X509FindType.FindByTimeValid:
                    // break;
                    // case X509FindType.FindByTimeNotYetValid:
                    // break;
                    // case X509FindType.FindByTimeExpired:
                    // break;
                    // case X509FindType.FindByTemplateName:
                    // break;
                    // case X509FindType.FindByApplicationPolicy:
                    // break;
                    // case X509FindType.FindByCertificatePolicy:
                    // break;
                    // case X509FindType.FindByExtension:
                    // break;
                    // case X509FindType.FindByKeyUsage:
                    // break;
                    // case X509FindType.FindBySubjectKeyIdentifier:
                    // break;
                default:
                    throw new NotSupportedException();
            }
        }
    
        private static Nullable<IssuerCertificateMapping> ConvertNodeToCertificateConfiguration(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(&quot;node&quot;);
            }
    
            if (node.Attributes == null)
            {
                return null;
            }
    
            XmlNode findTypeNode = node.Attributes.GetNamedItem(&quot;findType&quot;);
    
            if (findTypeNode == null)
            {
                throw new ConfigurationErrorsException(Resources.ConfiguredCertificateIssuerNameRegistry_FindTypeNotConfigured);
            }
    
            String configuredFindType = findTypeNode.Value;
            X509FindType findType;
    
            if (Enum.TryParse(configuredFindType, out findType) == false)
            {
                String message = String.Format(
                    CultureInfo.CurrentCulture, Resources.ConfiguredCertificateIssuerNameRegistry_InvalidFindTypeConfigured, configuredFindType);
    
                throw new ConfigurationErrorsException(message);
            }
    
            XmlNode findValueNode = node.Attributes.GetNamedItem(&quot;findValue&quot;);
    
            if (findValueNode == null)
            {
                throw new ConfigurationErrorsException(Resources.ConfiguredCertificateIssuerNameRegistry_FindValueNotConfigured);
            }
    
            String findValue = findValueNode.Value;
    
            if (String.IsNullOrWhiteSpace(findValue))
            {
                throw new ConfigurationErrorsException(Resources.ConfiguredCertificateIssuerNameRegistry_FindValueConfigurationIsEmpty);
            }
    
            XmlNode nameNode = node.Attributes.GetNamedItem(&quot;name&quot;);
    
            if (nameNode == null)
            {
                throw new ConfigurationErrorsException(Resources.ConfiguredCertificateIssuerNameRegistry_NameNotConfigured);
            }
    
            String name = nameNode.Value;
    
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ConfigurationErrorsException(Resources.ConfiguredCertificateIssuerNameRegistry_NameConfigurationIsEmpty);
            }
    
            return new IssuerCertificateMapping
                    {
                        FindType = findType, 
                        FindValue = findValue, 
                        IssuerName = name
                    };
        }
    
        private static String StripToCommonName(X500DistinguishedName distinguishedName)
        {
            String name = distinguishedName.Name;
    
            if (String.IsNullOrWhiteSpace(name))
            {
                return null;
            }
    
            Int32 commonNameIndex = name.LastIndexOf(&quot;CN=&quot;);
    
            if (commonNameIndex < 0)
            {
                return null;
            }
    
            commonNameIndex += 3;
    
            if (commonNameIndex > name.Length)
            {
                return null;
            }
    
            return name.Substring(commonNameIndex);
        }
    
        private void LoadIssueConfiguration(XmlNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(&quot;node&quot;);
            }
    
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == &quot;clear&quot;)
                {
                    TrustedIssuers.Clear();
                }
                else
                {
                    Nullable<IssuerCertificateMapping> issuerCertificateConfiguration = ConvertNodeToCertificateConfiguration(childNode);
    
                    if (issuerCertificateConfiguration == null)
                    {
                        continue;
                    }
    
                    if (childNode.Name == &quot;add&quot;)
                    {
                        TrustedIssuers.Add(issuerCertificateConfiguration.Value);
                    }
                    else if (childNode.Name == &quot;remove&quot;)
                    {
                        TrustedIssuers.Remove(issuerCertificateConfiguration.Value);
                    }
                }
            }
        }
    
        public List<IssuerCertificateMapping> TrustedIssuers
        {
            get
            {
                return _trustedIssuers;
            }
        }
    }
}
```

The ConfiguredCertificateIssuerNameRegistry parses the RP configuration to search for add, remove and clear elements. The add and remove elements then need to define the findType (X509FindType), findValue and name attributes. The set of trusted issues becomes the reference point for mapping security token certificates to issuer names. 

This class allows for a more flexible mapping between issuer names and certificates. The example configuration above can then be modified to use the subject name instead of thumbprint.

```xml
<service name=&quot;Neovolve.Jabiru.Server.Service.ExchangeSession&quot;>
    <audienceUris>
        <add value=&quot;https://localhost/Jabiru/DataExchange.svc&quot; />
    </audienceUris>
    <issuerNameRegistry type=&quot;Neovolve.Jabiru.Server.Security.ConfiguredCertificateIssuerNameRegistry, Neovolve.Jabiru.Server.Security&quot;>
        <trustedIssuers>
            <add findType=&quot;FindBySubjectDistinguishedName&quot;
                findValue=&quot;CN=STSTestCert&quot;
                name=&quot;https://localhost/JabiruSts/Service.svc&quot; />
        </trustedIssuers>
    </issuerNameRegistry>
</service>
```

The RP can then correctly identify issuer certificates created with the same subject on different machines.

[0]: http://jabiru.codeplex.com
[1]: http://msdn.microsoft.com/en-us/library/system.security.cryptography.x509certificates.x509findtype.aspx
