---
title: Low impact WCF integration testing with SSL
categories : .Net, Personal
tags : Unit Testing, WCF
date: 2008-11-21 00:18:00 +10:00
---


I have been writing lots of tests for services and framework/toolkit type components over the last year. In both of these areas of development, I have written unit and integration tests that use WCF calls over SSL. Using SSL means that certificates are required. While an x509 certificate can be loaded from a file path, most implementations that use certificates rely on the certificate already being imported into a certificate store on the local machine. This means that the environment in which the tests are executed needs to be configured prior to running the tests.

This scenario limits flexibility for two reasons. Firstly, another developer can't get the solution from source control and simply run the tests as they won't have the required certificate installed. Secondly, a continuous build process will not be able to build the solution on a build machine that hasn't already been configured.

In trying to address this environment configuration issue, we did previously come up with [a solution][0] after discussions with some colleagues. Creating localhost certificates meant that developer and team build machines would be able to run unit tests without configuration changes to service addresses between those machines. It did however mean that each machine still needed manual configuration in order to install localhost certificates.

Recently, I have been migrating my [custom username password service credential solution][1] into the [Neovolve.Toolkit][2] project on Codeplex. I want to create unit and integration tests that are portable to multiple machines without any prior configuration of those machines before running the tests or any assumptions about software installation beyond Visual Studio and the .Net framework. This means that I need a good certificate solution for SSL support. I also want to avoid IIS and Cassini hosting of the services and to support running the tests from both XP and Vista. XP is easy to please, but Vista comes with some security considerations. The intention is to have a solution with unit and integration tests that will work 'out of the box'.

**The answer**

The answer is to use a NetTcpBinding, a pfx certificate with a private key, runtime installation/uninstallation of the certificate using the current user certificate store and specific configuration of the WCF service and client.

Major kudos goes to [Mark Seemann][3] for [this post][4] which describes the idea of using pfx certificates and provides the bulk of the code to install and uninstall the certificate before and after the tests.

This post touches on Mark's guide for pfx certificates and working with the certificate store. It will then describe in more detail how this scenario can be used to cleanly provide an SSL solution for testing WCF services under source control.

**Generating the certificate**

Firstly, the certificate needs to be created. Using makecert.exe, a certificate can be generated and obtained from the certificate store. The only thing that needs to be modified for your own scenario is the subject name of the certificate.

To create this certificate, open up the Visual Studio command prompt (elevated privileges required on Vista) and enter:

> makecert -sr LocalMachine -ss My -a sha1 -n CN=**Neovolve.Toolkit**-sky exchange -pe

![MakeCert][5]

The 'Neovolve.Toolkit' is the subject name I have used in this case. You will probably want to use your own identifier.

Open up _MMC_ and add the _Certificates -&gt; Computer account -&gt; Local computer_ snap in.

[![ComputerCertificates][7]][6]

Go to _Personal Certificates_ and locate the certificate that you created with makecert.exe. Right click this certificate and select _All tasks -&gt; Export_. Continue through the wizard to export the pfx file with its private key.

I used the subject name as the password and the export file name so that it is easy to remember.

**Solution configuration**

The first thing to do is to add this pfx certificate file to your solution. If there are multiple projects that will use the certificate, the best way to manage the certificate is to add it as a Solution Item and link it to each project that will use it. Alternatively, if there is just one project that uses the certificate, add it directly to that test project. This is the easiest way to ensure that the pfx certificate is under source control and available to everyone who references the solution.

![image][8]

Next, we need to ensure that the certificate is copied to the target directory for the project. Go into the properties of the certificate and set the _Copy to Output Directory_ setting to _Copy if newer_.

![image][9]

Given that this certificate is used for unit testing, MSTest doesn't know that this file in the output directory is required for the test run. We need to explicitly tell it to take this file when it copies the binaries to its test directory.

The best way to do this is to add a DeploymentItem attribute to the test method that uses it. I prefer this method instead of using deployment configuration in testrunconfig files. If the same test project is used in multiple solutions, each solution may use a different testrunconfig file which may not have the same deployment settings. Solutions may also use multiple testrunconfig files which presents the same problem. Using the DeploymentItem attribute puts the deployment responsibility as close to the test as possible and eliminates deployment/configuration problems for running the test.

For example:

{% highlight csharp linenos %}
[TestMethod]
[DeploymentItem(@"Communication\Security\Neovolve.Toolkit.pfx")]
public void PasswordSecurityOnThreadTest()
{
}

{% endhighlight %}

Note that in this solution, the pfx certificate is in a child directory structure which needs to be indicated in the attribute. This is required because the copy instruction on the properties of the file causes the same directory structure relative to the project file to be used when the file is copied to the output directory by the compiler. When the DeploymentItem attribute is interpreted, the file is copied to the same location as the binaries as no output path has been specified in the attribute.

**Runtime certificate management**

In order for this certificate to be used in a test that requires SSL, it must be installed into a certificate store on the local machine. Because we don't want to leave the machine in an inconsistent state, the certificate needs to be uninstalled when the tests have completed.

The CertificateDetails struct below contains the information required to install and uninstall the certificate.

{% highlight csharp linenos %}
using System;
using System.Security.Cryptography.X509Certificates;
 
namespace Neovolve.Toolkit.UnitTests.Communication.Security
{
    /// <summary>
    /// The <see cref="CertificateDetails"/>
    /// struct is used to store information about a certificate for installing and uninstalling a certificate in a certificate store.
    /// </summary>
    internal struct CertificateDetails
    {
        /// <summary>
        /// Stores the Filepath of the certificate.
        /// </summary>
        public String Filepath;
 
        /// <summary>
        /// Stores the password of the certificate.
        /// </summary>
        public String Password;
 
        /// <summary>
        /// Stores the store location of the certificate when the certificate is installed.
        /// </summary>
        public StoreLocation StoreLocation;
 
        /// <summary>
        /// Stores the store name of the certificate when the certificate is installed.
        /// </summary>
        public StoreName StoreName;
 
        /// <summary>
        /// Stores the subject of the certificate.
        /// </summary>
        public String Subject;
  
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateDetails"/> struct.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="password">The password.</param>
        /// <param name="storeName">Name of the store.</param>
        /// <param name="storeLocation">The store location.</param>
        public CertificateDetails(
            String filePath, String subject, String password, StoreName storeName, StoreLocation storeLocation)
        {
            Filepath = filePath;
            Subject = subject;
            Password = password;
            StoreName = storeName;
            StoreLocation = storeLocation;
        }
    }
}
{% endhighlight %}

Certificates can be installed and uninstalled using the following CertificateManager class. As Mark mentions in his post, keep in mind that if there is collision of subject names with existing certificates, those certificates will also be uninstalled. To avoid this, use a subject name that is unique enough to identify the purpose of the certificate without colliding with existing certificates in the store.

{% highlight csharp linenos %}
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
 
namespace Neovolve.Toolkit.UnitTests.Communication.Security
{
    /// <summary>
    /// The <see cref="CertificateManager"/>
    /// class is used to install and uninstall certificates in the certificate store.
    /// </summary>
    internal class CertificateManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateManager"/> class.
        /// </summary>
        public CertificateManager()
        {
            Certificates = new List<CertificateDetails>();
        }
 
        /// <summary>
        /// Installs the certificates.
        /// </summary>
        public void InstallCertificates()
        {
            // Loop through each configured certificate
            for (Int32 index = 0; index < Certificates.Count; index++)
            {
                CertificateDetails certificate = Certificates[index];
 
                // Install this certificate
                InstallCertificate(certificate);
            }
        }
 
        /// <summary>
        /// Uninstalls the certificates.
        /// </summary>
        public void UninstallCertificates()
        {
            // Loop through each configured certificate
            for (Int32 index = 0; index < Certificates.Count; index++)
            {
                CertificateDetails certificate = Certificates[index];
 
                // Uninstall this certificate
                UninstallCertificate(certificate);
            }
        }
 
        /// <summary>
        /// Installs the certificate.
        /// </summary>
        /// <param name="certificateDetails">The certificate information.</param>
        private static void InstallCertificate(CertificateDetails certificateDetails)
        {
            X509Certificate2 certificate = new X509Certificate2(
                certificateDetails.Filepath, certificateDetails.Password, X509KeyStorageFlags.PersistKeySet);
            X509Store store = new X509Store(certificateDetails.StoreName, certificateDetails.StoreLocation);
 
            try
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
 
                store.Add(certificate);
            }
            finally
            {
                store.Close();
            }
        }
 
        /// <summary>
        /// Uninstalls the certificate.
        /// </summary>
        /// <param name="certificateDetails">The certificate information.</param>
        private static void UninstallCertificate(CertificateDetails certificateDetails)
        {
            X509Store store = new X509Store(certificateDetails.StoreName, certificateDetails.StoreLocation);
 
            try
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
 
                // Loop through each certificate in the store in reverse order
                for (Int32 index = store.Certificates.Count - 1; index >= 0; index--)
                {
                    X509Certificate2 certificate = store.Certificates[index];
 
                    // Check if the subject names match
                    if (certificate.Subject
                        == certificateDetails.Subject)
                    {
                        store.Remove(certificate);
                    }
                }
            }
            finally
            {
                store.Close();
            }
        }
 
        /// <summary>
        /// Gets the certificates.
        /// </summary>
        /// <value>The certificates.</value>
        public List<CertificateDetails> Certificates
        {
            get;
            private set;
        }
    }
}
{% endhighlight %}

The next part of the process is to configure the certificates so they can be installed at the beginning of the tests and uninstalled at the end. For example:

{% highlight csharp linenos %}
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neovolve.Toolkit.Communication.Security;
 
namespace Neovolve.Toolkit.UnitTests.Communication.Security
{
    /// <summary>
    /// The <see cref="PasswordServiceCredentialsTests"/>
    /// class contains unit tests for the 
    /// <see cref="PasswordServiceCredentials"/> implementation.
    /// </summary>
    [TestClass]
    public class PasswordServiceCredentialsTests
    {
        /// <summary>
        /// Stores the certificate manager.
        /// </summary>
        private static readonly CertificateManager certificateManager = new CertificateManager();
 
        #region Setup/Teardown
 
        /// <summary>
        /// Initializes the class.
        /// </summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            certificateManager.Certificates.Add(
                new CertificateDetails(
                    "Neovolve.Toolkit.pfx",
                    "CN=Neovolve.Toolkit",
                    "Neovolve.Toolkit",
                    StoreName.My,
                    StoreLocation.CurrentUser));
 
            certificateManager.UninstallCertificates();
            certificateManager.InstallCertificates();
        }
 
        /// <summary>
        /// Cleans up the class.
        /// </summary>
        [ClassCleanup]
        public static void CleanupClass()
        {
            certificateManager.UninstallCertificates();
        }
 
        #endregion
    }
}
{% endhighlight %}

This code identifies the pfx certificate using a relative path, the subject name used for the certificate and the password. The store location to use is _CurrentUser_. Using _LocalComputer_ for the store would require elevated privileges in Vista. Because the certificate is being installed, used for testing and then uninstalled by the account running the tests, the _CurrentUser_ store is still appropriate and avoids access denied errors.
    
**The service host and client**
    
My unit test is written with a self-hosted service and client that are both configured at runtime to avoid a reliance on IIS or Cassini. The following code is the initial code fragment of the unit test that sets up the host and client channel.

{% highlight csharp linenos %}
const String ServiceAddress = "net.tcp://localhost/PasswordServiceCredentialsTests";

Uri address = new Uri(ServiceAddress);
NetTcpBinding binding = new NetTcpBinding();

binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

ServiceHost host = new ServiceHost(typeof(PasswordService));

// Add debug support to the host
ServiceDebugBehavior debugBehaviour = new ServiceDebugBehavior();

debugBehaviour.IncludeExceptionDetailInFaults = true;
host.Description.Behaviors.Remove&lt;ServiceDebugBehavior&gt;();
host.Description.Behaviors.Add(debugBehaviour);

// Create the service credentials
PasswordServiceCredentials credentials = new PasswordServiceCredentials();

credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;

// Assign the certificate from the CurrentUser store using the subject name of the certificate
credentials.ServiceCertificate.SetCertificate(
    StoreLocation.CurrentUser, StoreName.My, X509FindType.FindBySubjectName, "Neovolve.Toolkit");

host.Description.Behaviors.Remove&lt;ServiceCredentials&gt;();
host.Description.Behaviors.Add(credentials);

ServiceAuthorizationBehavior authorization = new ServiceAuthorizationBehavior();

authorization.PrincipalPermissionMode = PrincipalPermissionMode.Custom;
host.Description.Behaviors.Remove&lt;ServiceAuthorizationBehavior&gt;();
host.Description.Behaviors.Add(authorization);

host.AddServiceEndpoint(typeof(IPasswordService), binding, address);

try
{
    host.Open();

    // We need to identify that the identity of the certificate is valid for the address of the service
    EndpointAddress endpointAddress = new EndpointAddress(
        address, EndpointIdentity.CreateDnsIdentity("Neovolve.Toolkit"));

    ChannelFactory&lt;IPasswordService&gt; factory = new ChannelFactory&lt;IPasswordService&gt;(
        binding, endpointAddress);

    try
    {
        // We need to ignore certificate validation errors
        // Because the certificate was created with makecert, there is no chain of trust on this certificate
        factory.Credentials.ServiceCertificate.Authentication.CertificateValidationMode =
            X509CertificateValidationMode.None;

        String userName = Guid.NewGuid().ToString();
        String password = Guid.NewGuid().ToString();

        factory.Credentials.UserName.UserName = userName;
        factory.Credentials.UserName.Password = password;

        factory.Open();

        IPasswordService channel = factory.CreateChannel();
{% endhighlight %}

The things to note about this code fragment are:

-The NetTcpBinding is used to avoid the security restrictions of http addresses in Vista
-The service certificate is identified by its subject
-The service certificate is found in the _CurrentUser_ store to avoid security restrictions on the _LocalComputer_ store in Vista
-The EndpointAddress used for the client channel is assigned an EndpointIdentity that identifies the subject of the certificate as a valid address for the service. This is important so that a certificate for _Neovolve.Toolkit_ can be used with an address of _localhost_ 
-Certificate validation is disabled for the client because the pfx certificate doesn't have a chain of trust that can be validated

Using this solution, a certificate can be under source control, be used to provide SSL support to test WCF services on both XP and Vista and not require initial configuration of IIS, Cassini or certificates.

[0]: /2008/02/28/wcf-ssl-and-localhost/
[1]: /2008/04/07/wcf-security-getting-the-password-of-the-user/
[2]: http://www.codeplex.com/Neovolve
[3]: http://blogs.msdn.com/ploeh/
[4]: http://blogs.msdn.com/ploeh/archive/2006/12/20/IntegrationTestingWithCertificates.aspx
[5]: /blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/MakeCert_3.jpg
[6]: /blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/ComputerCertificates_2.jpg
[7]: /blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/ComputerCertificates_thumb.jpg
[8]: /blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/image_3.png
[9]: /blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/image_6.png