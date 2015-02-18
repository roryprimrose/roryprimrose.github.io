---
title: Low impact WCF integration testing with SSL
categories : .Net, Personal
tags : Unit Testing, WCF
date: 2008-11-21 00:18:00 +10:00
---

<p>I have been writing lots of tests for services and framework/toolkit type components over the last year. In both of these areas of development, I have written unit and integration tests that use WCF calls over SSL. Using SSL means that certificates are required. While an x509 certificate can be loaded from a file path, most implementations that use certificates rely on the certificate already being imported into a certificate store on the local machine. This means that the environment in which the tests are executed needs to be configured prior to running the tests. </p>  <p>This scenario limits flexibility for two reasons. Firstly, another developer can't get the solution from source control and simply run the tests as they won't have the required certificate installed. Secondly, a continuous build process will not be able to build the solution on a build machine that hasn't already been configured.</p>  <p>In trying to address this environment configuration issue, we did previously come up with <a href="/post/2008/02/28/wcf-ssl-and-localhost.aspx">a solution</a> after discussions with some colleagues. Creating localhost certificates meant that developer and team build machines would be able to run unit tests without configuration changes to service addresses between those machines. It did however mean that each machine still needed manual configuration in order to install localhost certificates.</p>  <p>Recently, I have been migrating my <a href="/post/2008/04/07/wcf-security-getting-the-password-of-the-user.aspx">custom username password service credential solution</a> into the <a href="http://www.codeplex.com/Neovolve" target="_blank">Neovolve.Toolkit</a> project on Codeplex. I want to create unit and integration tests that are portable to multiple machines without any prior configuration of those machines before running the tests or any assumptions about software installation beyond Visual Studio and the .Net framework. This means that I need a good certificate solution for SSL support. I also want to avoid IIS and Cassini hosting of the services and to support running the tests from both XP and Vista. XP is easy to please, but Vista comes with some security considerations. The intention is to have a solution with unit and integration tests that will work 'out of the box'.</p>  <p><strong>The answer</strong></p>  <p>The answer is to use a NetTcpBinding, a pfx certificate with a private key, runtime installation/uninstallation of the certificate using the current user certificate store and specific configuration of the WCF service and client.</p>  <p>Major kudos goes to <a href="http://blogs.msdn.com/ploeh/" target="_blank">Mark Seemann</a> for <a href="http://blogs.msdn.com/ploeh/archive/2006/12/20/IntegrationTestingWithCertificates.aspx" target="_blank">this post</a> which describes the idea of using pfx certificates and provides the bulk of the code to install and uninstall the certificate before and after the tests.</p>  <p>This post touches on Mark's guide for pfx certificates and working with the certificate store. It will then describe in more detail how this scenario can be used to cleanly provide an SSL solution for testing WCF services under source control.</p>  <p><strong>Generating the certificate</strong></p>  <p>Firstly, the certificate needs to be created. Using makecert.exe, a certificate can be generated and obtained from the certificate store. The only thing that needs to be modified for your own scenario is the subject name of the certificate.</p>  <p>To create this certificate, open up the Visual Studio command prompt (elevated privileges required on Vista) and enter:</p>  <blockquote>   <p>makecert -sr LocalMachine -ss My -a sha1 -n CN=<strong>Neovolve.Toolkit</strong> -sky exchange -pe</p> </blockquote>  <p><img style="border-right-width: 0px; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px" border="0" alt="MakeCert" src="//blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/MakeCert_3.jpg" width="673" height="342" /> </p>  <p>The 'Neovolve.Toolkit' is the subject name I have used in this case. You will probably want to use your own identifier.</p>  <p>Open up <em>MMC</em> and add the <em>Certificates -&gt; Computer account -&gt; Local computer</em> snap in.</p>  <p><a href="//blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/ComputerCertificates_2.jpg"><img style="border-right-width: 0px; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px" border="0" alt="ComputerCertificates" src="//blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/ComputerCertificates_thumb.jpg" width="644" height="479" /></a> </p>  <p>Go to <em>Personal Certificates</em> and locate the certificate that you created with makecert.exe. Right click this certificate and select <em>All tasks -&gt; Export</em>. Continue through the wizard to export the pfx file with its private key. </p>  <p>I used the subject name as the password and the export file name so that it is easy to remember.</p>  <p><strong>Solution configuration</strong></p>  <p>The first thing to do is to add this pfx certificate file to your solution. If there are multiple projects that will use the certificate, the best way to manage the certificate is to add it as a Solution Item and link it to each project that will use it. Alternatively, if there is just one project that uses the certificate, add it directly to that test project. This is the easiest way to ensure that the pfx certificate is under source control and available to everyone who references the solution.</p>  <p><img style="border-right-width: 0px; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px" border="0" alt="image" src="//blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/image_3.png" width="354" height="649" /> </p>  <p>Next, we need to ensure that the certificate is copied to the target directory for the project. Go into the properties of the certificate and set the <em>Copy to Output Directory</em> setting to <em>Copy if newer</em>.</p>  <p><img style="border-right-width: 0px; border-top-width: 0px; border-bottom-width: 0px; border-left-width: 0px" border="0" alt="image" src="//blogfiles/WindowsLiveWriter/LowimpactintegrationtestingwithWCFandSSL_139DD/image_6.png" width="487" height="181" /> </p>  <p>Given that this certificate is used for unit testing, MSTest doesn't know that this file in the output directory is required for the test run. We need to explicitly tell it to take this file when it copies the binaries to its test directory. </p>  <p>The best way to do this is to add a DeploymentItem attribute to the test method that uses it. I prefer this method instead of using deployment configuration in testrunconfig files. If the same test project is used in multiple solutions, each solution may use a different testrunconfig file which may not have the same deployment settings. Solutions may also use multiple testrunconfig files which presents the same problem. Using the DeploymentItem attribute puts the deployment responsibility as close to the test as possible and eliminates deployment/configuration problems for running the test.</p>  <p>For example:</p>  <div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:0f00e480-9abc-4bd9-8a74-d97faafd8f79" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}[TestMethod]
[DeploymentItem(@"Communication\Security\Neovolve.Toolkit.pfx")]
public void PasswordSecurityOnThreadTest()
{
}
{% endhighlight %}</div>

<p>Note that in this solution, the pfx certificate is in a child directory structure which needs to be indicated in the attribute. This is required because the copy instruction on the properties of the file causes the same directory structure relative to the project file to be used when the file is copied to the output directory by the compiler. When the DeploymentItem attribute is interpreted, the file is copied to the same location as the binaries as no output path has been specified in the attribute.</p>

<p><strong>Runtime certificate management</strong></p>

<p>In order for this certificate to be used in a test that requires SSL, it must be installed into a certificate store on the local machine. Because we don't want to leave the machine in an inconsistent state, the certificate needs to be uninstalled when the tests have completed.</p>

<p>The CertificateDetails struct below contains the information required to install and uninstall the certificate.</p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:8e8f5af3-13fd-4124-9346-8bd14a427d62" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}using System;
using System.Security.Cryptography.X509Certificates;
 
namespace Neovolve.Toolkit.UnitTests.Communication.Security
{
    /// &lt;summary&gt;
    /// The &lt;see cref="CertificateDetails"/&gt;
    /// struct is used to store information about a certificate for installing and uninstalling a certificate in a certificate store.
    /// &lt;/summary&gt;
    internal struct CertificateDetails
    {
        /// &lt;summary&gt;
        /// Stores the Filepath of the certificate.
        /// &lt;/summary&gt;
        public String Filepath;
 
        /// &lt;summary&gt;
        /// Stores the password of the certificate.
        /// &lt;/summary&gt;
        public String Password;
 
        /// &lt;summary&gt;
        /// Stores the store location of the certificate when the certificate is installed.
        /// &lt;/summary&gt;
        public StoreLocation StoreLocation;
 
        /// &lt;summary&gt;
        /// Stores the store name of the certificate when the certificate is installed.
        /// &lt;/summary&gt;
        public StoreName StoreName;
 
        /// &lt;summary&gt;
        /// Stores the subject of the certificate.
        /// &lt;/summary&gt;
        public String Subject;
  
        /// &lt;summary&gt;
        /// Initializes a new instance of the &lt;see cref="CertificateDetails"/&gt; struct.
        /// &lt;/summary&gt;
        /// &lt;param name="filePath"&gt;The file path.&lt;/param&gt;
        /// &lt;param name="subject"&gt;The subject.&lt;/param&gt;
        /// &lt;param name="password"&gt;The password.&lt;/param&gt;
        /// &lt;param name="storeName"&gt;Name of the store.&lt;/param&gt;
        /// &lt;param name="storeLocation"&gt;The store location.&lt;/param&gt;
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
}{% endhighlight %}</div>

<p>Certificates can be installed and uninstalled using the following CertificateManager class. As Mark mentions in his post, keep in mind that if there is collision of subject names with existing certificates, those certificates will also be uninstalled. To avoid this, use a subject name that is unique enough to identify the purpose of the certificate without colliding with existing certificates in the store.</p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:ffc1a709-80cc-46cd-b90e-85395efd7f6b" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
 
namespace Neovolve.Toolkit.UnitTests.Communication.Security
{
    /// &lt;summary&gt;
    /// The &lt;see cref="CertificateManager"/&gt;
    /// class is used to install and uninstall certificates in the certificate store.
    /// &lt;/summary&gt;
    internal class CertificateManager
    {
        /// &lt;summary&gt;
        /// Initializes a new instance of the &lt;see cref="CertificateManager"/&gt; class.
        /// &lt;/summary&gt;
        public CertificateManager()
        {
            Certificates = new List&lt;CertificateDetails&gt;();
        }
 
        /// &lt;summary&gt;
        /// Installs the certificates.
        /// &lt;/summary&gt;
        public void InstallCertificates()
        {
            // Loop through each configured certificate
            for (Int32 index = 0; index &lt; Certificates.Count; index++)
            {
                CertificateDetails certificate = Certificates[index];
 
                // Install this certificate
                InstallCertificate(certificate);
            }
        }
 
        /// &lt;summary&gt;
        /// Uninstalls the certificates.
        /// &lt;/summary&gt;
        public void UninstallCertificates()
        {
            // Loop through each configured certificate
            for (Int32 index = 0; index &lt; Certificates.Count; index++)
            {
                CertificateDetails certificate = Certificates[index];
 
                // Uninstall this certificate
                UninstallCertificate(certificate);
            }
        }
 
        /// &lt;summary&gt;
        /// Installs the certificate.
        /// &lt;/summary&gt;
        /// &lt;param name="certificateDetails"&gt;The certificate information.&lt;/param&gt;
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
 
        /// &lt;summary&gt;
        /// Uninstalls the certificate.
        /// &lt;/summary&gt;
        /// &lt;param name="certificateDetails"&gt;The certificate information.&lt;/param&gt;
        private static void UninstallCertificate(CertificateDetails certificateDetails)
        {
            X509Store store = new X509Store(certificateDetails.StoreName, certificateDetails.StoreLocation);
 
            try
            {
                store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
 
                // Loop through each certificate in the store in reverse order
                for (Int32 index = store.Certificates.Count - 1; index &gt;= 0; index--)
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
 
        /// &lt;summary&gt;
        /// Gets the certificates.
        /// &lt;/summary&gt;
        /// &lt;value&gt;The certificates.&lt;/value&gt;
        public List&lt;CertificateDetails&gt; Certificates
        {
            get;
            private set;
        }
    }
}{% endhighlight %}</div>

<p>The next part of the process is to configure the certificates so they can be installed at the beginning of the tests and uninstalled at the end. For example:</p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:45644452-1d57-4b0f-9193-d83403ab5000" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neovolve.Toolkit.Communication.Security;
 
namespace Neovolve.Toolkit.UnitTests.Communication.Security
{
    /// &lt;summary&gt;
    /// The &lt;see cref="PasswordServiceCredentialsTests"/&gt;
    /// class contains unit tests for the 
    /// &lt;see cref="PasswordServiceCredentials"/&gt; implementation.
    /// &lt;/summary&gt;
    [TestClass]
    public class PasswordServiceCredentialsTests
    {
        /// &lt;summary&gt;
        /// Stores the certificate manager.
        /// &lt;/summary&gt;
        private static readonly CertificateManager certificateManager = new CertificateManager();
 
        #region Setup/Teardown
 
        /// &lt;summary&gt;
        /// Initializes the class.
        /// &lt;/summary&gt;
        /// &lt;param name="context"&gt;The context.&lt;/param&gt;
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
 
        /// &lt;summary&gt;
        /// Cleans up the class.
        /// &lt;/summary&gt;
        [ClassCleanup]
        public static void CleanupClass()
        {
            certificateManager.UninstallCertificates();
        }
 
        #endregion
    }
}{% endhighlight %}</div>

<p>This code identifies the pfx certificate using a relative path, the subject name used for the certificate and the password. The store location to use is <em>CurrentUser</em>. Using <em>LocalComputer</em> for the store would require elevated privileges in Vista. Because the certificate is being installed, used for testing and then uninstalled by the account running the tests, the <em>CurrentUser</em> store is still appropriate and avoids access denied errors.</p>

<p><strong>The service host and client</strong></p>

<p>My unit test is written with a self-hosted service and client that are both configured at runtime to avoid a reliance on IIS or Cassini. The following code is the initial code fragment of the unit test that sets up the host and client channel.</p>

<div style="padding-bottom: 0px; margin: 0px; padding-left: 0px; padding-right: 0px; display: inline; float: none; padding-top: 0px" id="scid:812469c5-0cb0-4c63-8c15-c81123a09de7:f9d854bf-c9e4-4c06-bb8f-d4fdfd35cad7" class="wlWriterEditableSmartContent">{% highlight csharp linenos %}const String ServiceAddress = "net.tcp://localhost/PasswordServiceCredentialsTests";

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

        IPasswordService channel = factory.CreateChannel();{% endhighlight %}</div>

<p>The things to note about this code fragment are:</p>

<ul>
  <li>The NetTcpBinding is used to avoid the security restrictions of http addresses in Vista </li>

  <li>The service certificate is identified by its subject </li>

  <li>The service certificate is found in the <em>CurrentUser</em> store to avoid security restrictions on the <em>LocalComputer</em> store in Vista </li>

  <li>The EndpointAddress used for the client channel is assigned an EndpointIdentity that identifies the subject of the certificate as a valid address for the service. This is important so that a certificate for <em>Neovolve.Toolkit</em> can be used with an address of <em>localhost</em> </li>

  <li>Certificate validation is disabled for the client because the pfx certificate doesn't have a chain of trust that can be validated </li>
</ul>

<p>Using this solution, a certificate can be under source control, be used to provide SSL support to test WCF services on both XP and Vista and not require initial configuration of IIS, Cassini or certificates.</p>
