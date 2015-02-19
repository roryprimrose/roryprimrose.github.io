---
title: WCF Security: Getting the password of the user
categories : .Net, My Software
tags : Extensibility, WCF
date: 2008-04-07 20:41:00 +10:00
---

A common problem with service security is that username/password security is needed for authentication and authorization at the service boundary, but those same credentials are also required to consume other resources such as a database or underlying service. By default, username/password security will run the authentication and authorization of the credentials but only the username is available to the executing service code. This is typically made available through Thread.CurrentPrincipal.Identity.Name. 

Storing username password credentials in a custom principal and identity against Thread.CurrentPrincipal is a really nice way of going. Thread.CurrentPrincipal returns IPrincipal which is a common framework type that will be available to all layers of a service executed by the thread. If Thread.CurrentPrincipal.Identity can return a custom IIdentity, then this is where the password can be made available. Using Thread.CurrentPrincipal frees up business and data access layers from relying on any security design that is tied up with a specific service implementation. The trick is how to get username password information into the thread that executes the service code. 

One place that both the username and password is available is in the UserNamePasswordValidator.Validate() method. You could write a custom validator and hook it up in your service configuration, but this doesn't help you. There is no native facility to store the password from the validator. You can't store any credentials against Thread.CurrentPrincipal as the validator gets evaluated by WCF on a different thread than the thread that executes the service code. You could manually put it somewhere like in a static collection, but this would have potential security risks as your code will be handling the safety and security of multiple sets of credentials for users. This also means that somewhere else in the service implementation would require the credentials to be pulled out of the static and put into Thread.CurrentPrincipal. Using validators for this purpose is not the answer. Validators are for validating credentials, not storing them for later use. 

Thankfully, there is a much more elegant way of passing these credentials around compared to the validator based solution. There is another place in WCF where there is access to both the username and the password and the security context of the service. Leveraging WCF extensibility is the answer. 

Setting up the security context of the service is done using [IAuthorizationPolicy][0] implementations. The place in WCF where there is access to the username, password and IAuthorizationPolicy configuration is [CustomUserNameSecurityTokenAuthenticator.ValidateUserNamePasswordCore][1]. The ValidateUserNamePasswordCore method is passed the username and password parameters and returns a readonly collection of IAuthorizationPolicy objects. SecurityTokenAuthenticator, from which CustomUserNameSecurityTokenAuthenticator ultimately inherits from, is not configurable in WCF itself. Using [Reflector] to follow the calls against SecurityTokenAuthenticator, the place that is extensible in WCF such that a custom SecurityTokenAuthenticator can be used is [ServiceCredentials][2]. Creating a custom ServiceCredentials object, using custom objects between the ServiceCredentials and SecurityTokenAuthenticator call stack is the answer. 

**PasswordServiceCredentials**

The custom ServiceCredentials class implementation returns a custom SecurityTokenManager if custom username password validation is enabled. 

    {% highlight csharp linenos %}
    public class PasswordServiceCredentials : ServiceCredentials
    {
        public PasswordServiceCredentials()
        {
        }
    
        private PasswordServiceCredentials(PasswordServiceCredentials clone)
            : base(clone)
        {
        }
    
        protected override ServiceCredentials CloneCore()
        {
            return new PasswordServiceCredentials(this);
        }
    
        public override SecurityTokenManager CreateSecurityTokenManager()
        {
            // Check if the current validation mode is for custom username password validation
            if (UserNameAuthentication.UserNamePasswordValidationMode == UserNamePasswordValidationMode.Custom)
            {
                return new PasswordSecurityTokenManager(this);
            }
    
            Trace.TraceWarning(Resources.CustomUserNamePasswordValidationNotEnabled);
    
            return base.CreateSecurityTokenManager();
        }
    }
    {% endhighlight %}

**PasswordSecurityTokenManager**

The custom SecurityTokenManager returns a custom SecurityTokenAuthenticator when it finds a SecurityTokenRequirement for username security. It also ensures that a default validator is available if one is not configured. 

    {% highlight csharp linenos %}
    internal class PasswordSecurityTokenManager : ServiceCredentialsSecurityTokenManager
    {
        public PasswordSecurityTokenManager(PasswordServiceCredentials credentials)
            : base(credentials)
        {
        }
    
        public override SecurityTokenAuthenticator CreateSecurityTokenAuthenticator(SecurityTokenRequirement tokenRequirement, out SecurityTokenResolver outOfBandTokenResolver)
        {
            if (tokenRequirement.TokenType == SecurityTokenTypes.UserName)
            {
                outOfBandTokenResolver = null;
    
                // Get the current validator
                UserNamePasswordValidator validator =
                    ServiceCredentials.UserNameAuthentication.CustomUserNamePasswordValidator;
    
                // Ensure that a validator exists
                if (validator == null)
                {
                    Trace.TraceWarning(Resources.NoCustomUserNamePasswordValidatorConfigured);
                    validator = new DefaultPasswordValidator();
                }
    
                return new PasswordSecurityTokenAuthenticator(validator);
            }
    
            return base.CreateSecurityTokenAuthenticator(tokenRequirement, out outOfBandTokenResolver);
        }
    }
    {% endhighlight %}

**PasswordSecurityTokenAuthenticator**

The custom SecurityTokenAuthenticator is where the half of the magic happens. Here there is the opportunity to return custom a IAuthorizationPolicy implementation that will allow us to inject a custom IPrincipal and IIdentity on the thread the executes the service code. 

    {% highlight csharp linenos %}
    internal class PasswordSecurityTokenAuthenticator : CustomUserNameSecurityTokenAuthenticator
    {
        public PasswordSecurityTokenAuthenticator(UserNamePasswordValidator validator)
            : base(validator)
        {
        }
    
        protected override ReadOnlyCollection<IAuthorizationPolicy&gt; ValidateUserNamePasswordCore(String userName, String password)
        {
            ReadOnlyCollection<IAuthorizationPolicy&gt; currentPolicies = base.ValidateUserNamePasswordCore(
                userName, password);
            List<IAuthorizationPolicy&gt; newPolicies = new List<IAuthorizationPolicy&gt;(currentPolicies);
    
            newPolicies.Add(new PasswordAuthorizationPolicy(userName, password));
    
            return newPolicies.AsReadOnly();
        }
    }
    {% endhighlight %}

**PasswordAuthorizationPolicy**

The IAuthorizationPolicy implementation is where the other half of the magic happens. This policy gets passed the username and password so that it can store the password in the security context. The policy will return false until it finds a GenericIdentity in the evaluation context that has the same username as the one provided to the policy. It then creates a custom IIdentity that exposes both the username and password and stores it back into the collection of identities in the evaluation context and also stores it against the PrimaryIdentity property. PrimaryIdentity is then exposed through [ServiceSecurityContext.PrimaryIdentity][3] in the service implementation. A custom principal is then created (without roles) and stores it against the Principal property of the context. Principal is then injected into Thread.CurrentPrincipal by WCF (depending on configuration). 

    {% highlight csharp linenos %}
    using System; 
    using System.Collections.Generic; 
    using System.IdentityModel.Claims; 
    using System.IdentityModel.Policy; 
    using System.Security.Principal; 
    using System.ServiceModel; 
    using System.Threading; 
    
    namespace Neovolve.Framework.Communication.Security 
    { 
        internal class PasswordAuthorizationPolicy : IAuthorizationPolicy 
        { 
            public PasswordAuthorizationPolicy(String userName, String password) 
            { 
                const String UserNameParameterName = "userName"; 
    
                if (String.IsNullOrEmpty(userName)) 
                { 
                    throw new ArgumentNullException(UserNameParameterName); 
                } 
      
                Id = Guid.NewGuid().ToString(); 
                Issuer = ClaimSet.System; 
                UserName = userName; 
                Password = password; 
            } 
      
            public bool Evaluate(EvaluationContext evaluationContext, ref object state) 
            { 
                const String IdentitiesKey = "Identities"; 
      
                // Check if the properties of the context has the identities list 
                if (evaluationContext.Properties.Count == 0 
                    || evaluationContext.Properties.ContainsKey(IdentitiesKey) == false 
                    || evaluationContext.Properties[IdentitiesKey] == null) 
                { 
                    return false; 
                } 
      
                // Get the identities list 
                List<IIdentity&gt; identities = evaluationContext.Properties[IdentitiesKey] as List<IIdentity&gt;; 
      
                // Validate that the identities list is valid 
                if (identities == null) 
                { 
                    return false; 
                } 
      
                // Get the current identity 
                IIdentity currentIdentity = 
                    identities.Find( 
                        identityMatch =&gt; 
                        identityMatch is GenericIdentity 
                        && String.Equals(identityMatch.Name, UserName, StringComparison.OrdinalIgnoreCase)); 
      
                // Check if an identity was found 
                if (currentIdentity == null) 
                { 
                    return false; 
                } 
      
                // Create new identity 
                PasswordIdentity newIdentity = new PasswordIdentity( 
                    UserName, Password, currentIdentity.IsAuthenticated, currentIdentity.AuthenticationType); 
                const String PrimaryIdentityKey = "PrimaryIdentity"; 
      
                // Update the list and the context with the new identity 
                identities.Remove(currentIdentity); 
                identities.Add(newIdentity); 
                evaluationContext.Properties[PrimaryIdentityKey] = newIdentity; 
      
                // Create a new principal for this identity 
                PasswordPrincipal newPrincipal = new PasswordPrincipal(newIdentity, null); 
                const String PrincipalKey = "Principal"; 
      
                // Store the new principal in the context 
                evaluationContext.Properties[PrincipalKey] = newPrincipal; 
      
                // This policy has successfully been evaluated and doesn't need to be called again 
                return true; 
            } 
    
            public String Id 
            {
                get;
                private set;
            } 
    
    
            public ClaimSet Issuer 
            { 
                get; 
                private set; 
            } 
      
            private String Password 
            { 
                get; 
                set; 
            } 
      
            private String UserName 
            { 
                get; 
                set; 
            } 
        } 
    } 
    
    {% endhighlight %}

**Configuration**

As mentioned above, the custom ServiceCredentials needs to be configured so that the custom IAuthorizationPolicy is evaluated. Under serviceBehaviors, the serviceCredentials element allows a custom type to be defined. userNameAuthentication needs to be set to Custom, otherwise Windows authentication of the username password credentials will be used by default. Lastly, serviceAuthorization needs to set userNamePasswordValidationMode to custom. Without the validation mode being custom, the custom IPrincipal will not be assigned against Thread.CurrentPrincipal. 

The other thing to note is that because username password credentials are being passed over to the wire to the service, transport security is required to protect the credentials. The security mode should be set to TransportWIthMessageCredentials with a message client credential type as UserName. 

    {% highlight csharp linenos %}
    <?xml version="1.0" encoding="utf-8" ?> 
    <configuration> 
      <system.web> 
        <compilation debug="true" /> 
      </system.web> 
      <system.serviceModel> 
        <bindings> 
          <netTcpBinding> 
            <binding name="netTcpBindingConfig"> 
              <security mode="TransportWithMessageCredential"> 
                <message clientCredentialType="UserName" /> 
              </security> 
            </binding> 
          </netTcpBinding> 
        </bindings> 
        <services> 
          <service behaviorConfiguration="Neovolve.Framework.Communication.SecurityTest.Service1Behavior" 
            name="Neovolve.Framework.Communication.SecurityHost.Service1"> 
            <endpoint address="net.tcp://localhost:8792/PasswordSecurityTest" 
              binding="netTcpBinding" bindingConfiguration="netTcpBindingConfig" 
              contract="Neovolve.Framework.Communication.SecurityHost.IService1" /> 
          </service> 
        </services> 
        <behaviors> 
          <serviceBehaviors> 
            <behavior name="Neovolve.Framework.Communication.SecurityTest.Service1Behavior"> 
              <serviceDebug includeExceptionDetailInFaults="true" /> 
              <serviceCredentials type="Neovolve.Framework.Communication.Security.PasswordServiceCredentials, Neovolve.Framework.Communication.Security"> 
                <serviceCertificate findValue="localhost" x509FindType="FindBySubjectName" /> 
                <userNameAuthentication userNamePasswordValidationMode="Custom" /> 
              </serviceCredentials> 
              <serviceAuthorization principalPermissionMode="Custom" /> 
            </behavior> 
          </serviceBehaviors> 
        </behaviors> 
      </system.serviceModel> 
    </configuration> 
    {% endhighlight %}

Download: [Neovolve.Framework.Communication.Security.zip (78.91 kb)][4]

**Note:** the unit test in the solution must be run in debug rather than normal unit test running because of the use of the WcfServiceHost.exe. With the configuration defined, an x509 certificate is required on the machine with localhost as the subject name. 

[0]: http://msdn2.microsoft.com/en-us/library/system.identitymodel.policy.iauthorizationpolicy.aspx
[1]: http://msdn2.microsoft.com/en-us/library/system.identitymodel.selectors.customusernamesecuritytokenauthenticator.validateusernamepasswordcore.aspx
[2]: http://msdn2.microsoft.com/en-us/library/system.servicemodel.description.servicecredentials.aspx
[3]: http://msdn2.microsoft.com/en-us/library/system.servicemodel.servicesecuritycontext.primaryidentity.aspx
[4]: /files/2008%2f9%2fNeovolve.Framework.Communication.Security.zip
