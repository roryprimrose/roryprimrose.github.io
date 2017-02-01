---
title: Octopus Deploy with Let's Encrypt via DNS
categories: .Net
tags: 
date: 2017-02-01 16:50:00 +10:00
---

I've been keen to configure an Octopus Deploy server with Let's Encrypt certificates. The ultimate goal would be for this to be fully automated so that the the server can maintain it's own certificates.

TL;DR - This post provides a collection of custom Octopus Deploy steps that can assist with DNS based domain validation to process Let's Encrypt certificates.

<!--more-->

There are several roadblocks for having Octopus Deploy manage it's own certificate with Let's Encrypt. 

## Triggering the process
The server would need to run this process each 60 days as Let's Encrypt certificates are only valid for 90 days. There is currently no support for this in Octopus Deploy. The server supports recurring maintainence tasks but these are not user configurable. You could use a scheduled release but releases can only be scheduled for up to two weeks in the future and you would have to constantly create a new release to trigger it. This kills the automation idea.

## Domain validation
Let's Encrypt must be able to validate that you own the domain for which you are requesting a certificate. The validation options are HTTP and DNS. 

HTTP works by the server responding to a challenge over HTTP for a specific URI. This doesn't work for Octopus Deploy natively because the server runs on NancyFx for which I doubt we can get it to respond to a specific URI with a challenge code. There is an alternative here were you could host another website on the same machine using a different port on HTTP instead of HTTPS which can respond to the request. This is likely to work but requires the set up of an additional website on the machine. This could be automated. One issue here is that you would not want to have the IIS website running all the time. Automation of this process could spin up this website, set the challenge response, then shutdown the website when the domain validation had been completed. So HTTP based validation without native support in Octopus Deploy could be possible.

HTTP by IIS is an option provided by the ACMESharp package. It seems to deal with IIS on your behalf to get the domain validation sorted out. This is probably a better alternative to rolling your own website to do HTTP validation as described above.

DNS validation requires a DNS CNAME entry with the challenge information provided by Let's Encrypt. This only works in an automation sense if your DNS provider has an API that you can hit via PowerShell.

## Restrictions
One Octopus Deploy server that I tried to set this up on came with too many restrictions for this to be viable. The server itself was on an internal network that the public internet could not connect to. This meant that HTTP domain validation was not possible. The DNS provider involved also did not provide an API so there is no possibility of complete automation of Let's Encrypt certificates for the Octopus Deploy server.

## Steps
Given the above restrictions, I created a series of deployment steps that can be manually executed by Octopus Deploy. This will allow for at least some level of automation for certificates event though the process would have to be run manually every couple of months.

These build steps assume that DNS validation will be used and this part is an out-of-band manual process. 

The pre-requisite for these build steps is that you can import modules from the PowerShell gallery. Importing the ACMESharp module may fail on a function conflict on Windows Server 2012 R2. If this is the case then you can specify the ```-AllowClobber``` option on the Install-Module call. This doesn't seem to be an issue on Windows Server 2016. 

### Create challenge
This is the first step in the process. It will create the ACME vault and registration as required. It will then create a new challenge request. If the ChallengeId parameter is not provided then the script will generate one. You will need to copy this value from the logs as it is used in the other steps.

```powershell
param(
    [string]$Domain,
    [string]$ContactEmail,
    [string]$ChallengeId
)

function Get-Param($Name, [switch]$Required, $Default) {
    $result = $null
    
    if ($OctopusParameters -ne $null) {
        $result = $OctopusParameters[$Name]
    }

    if ($result -eq $null) {
        $variable = Get-Variable $Name -EA SilentlyContinue    
        if ($variable -ne $null) {
            $result = $variable.Value
        }
    }

    $testResult = [string]$result

    if ([string]::IsNullOrEmpty($testResult) -eq $true) {
        if ($Required) {
            throw "Missing parameter value $Name"
        } else {
            $result = $Default
        }
    }

    return $result
}

function Strip-Protcol($value){
    # Strip any protocol from the value
    if ($value.IndexOf("://") -gt -1) {
        return $value.Substring($value.IndexOf("://") + 3)
    }
    
    return $value
}

function Generate-ChallengeId([string]$domain) {
    $DelimiterIndex = $domain.IndexOf(".")
    $Prefix = $domain.Substring(0, $DelimiterIndex)
    $Marker = [DateTime]::UtcNow.ToString("yyyyMMdd-hhmmss")
    
    return $Prefix + "-" + $Marker
}

function Ensure-VaultExists() {
    if (-not (Get-ACMEVault)) {
        Write-Host "Creating ACME vault"
        Initialize-ACMEVault
    }
}

function Ensure-Registered([string]$email) {
    $mailTo = "mailto:$email"

    try {
        Get-ACMERegistration | ? { $_.Contacts.Contains($mailTo) }   
    }
    catch {
        Write-Host "Creating registration for $email"
        New-ACMERegistration -Contacts $mailTo -AcceptTos
    }
}

function Create-Challenge([string]$domain, [string]$challengeId){
    Write-Host "Checking for challenge on $domain with Challenge Id $challengeId"
    
    $Identifier = $null
    
    try {
        $Identifier = Get-ACMEIdentifier | ? { $_.Alias -eq $challengeId }
    }
    catch {
    }
    
    if ($Identifier -ne $null) {
        throw "An identifier already exists on $domain with Challenge Id $challengeId"
    }
    
    Write-Host "Creating identifier"
    New-ACMEIdentifier -Dns $domain -Alias $challengeId

    Write-Host "Creating a new challenge"
    Complete-ACMEChallenge $challengeId -ChallengeType dns-01 -Handler manual
}

& {
    param(
        [string]$Domain,
        [string]$ContactEmail,
        [string]$ChallengeId
    ) 

    $ErrorActionPreference = "Stop" 

    if (-not (Get-Module -ListAvailable -Name ACMESharp)) {
        Write-Host "Installing ACMESharp module"
        Install-Module -Name ACMESharp #-AllowClobber
    }

    Import-Module ACMESharp

    $Domain = Strip-Protcol $Domain
    
    if ([string]::IsNullOrWhiteSpace($ChallengeId)) {
        $ChallengeId = Generate-ChallengeId $Domain
    }
    
    Ensure-VaultExists
    Ensure-Registered $ContactEmail
    Create-Challenge $Domain $ChallengeId

 } `
 (Get-Param 'Domain' -Required) `
 (Get-Param 'ContactEmail' -Required) `
 (Get-Param 'ChallengeId')
```

### Submit Challenge
Once you have made the DNS change as per the logs from the Create Challenge step, you can then submit the challenge to Let's Encrypt. Again, you need to provide the same ChallengeId value here.

```powershell
param(
    [string]$ChallengeId
)

function Get-Param($Name, [switch]$Required, $Default) {
    $result = $null
    
    if ($OctopusParameters -ne $null) {
        $result = $OctopusParameters[$Name]
    }

    if ($result -eq $null) {
        $variable = Get-Variable $Name -EA SilentlyContinue    
        if ($variable -ne $null) {
            $result = $variable.Value
        }
    }

    $testResult = [string]$result

    if ([string]::IsNullOrEmpty($testResult) -eq $true) {
        if ($Required) {
            throw "Missing parameter value $Name"
        } else {
            $result = $Default
        }
    }

    return $result
}

function Submit-Request([string]$challengeId){

    Submit-ACMEChallenge $challengeId -ChallengeType dns-01

    $Identifier = $null

    try {
        $Identifier = Get-ACMEIdentifier | ? { $_.Alias -eq $challengeId }  
    }
    catch {
        Write-Host "No identifier found for Challenge Id $challengeId"
    }

    if ($Identifier -ne $null) {
        $UpdatedIdentifier = (Update-ACMEIdentifier $challengeId -ChallengeType dns-01)
        
        $domain = $UpdatedIdentifier.Identifier
        
        Write-Host "Current status of $domain is $($UpdatedIdentifier.Status)"
        
        ConvertTo-Json $UpdatedIdentifier
        
        if ($UpdatedIdentifier.Status -eq "pending") {
            Write-Host "Found pending challenges"
            $Challenges = $UpdatedIdentifier.Challenges | Where-Object {$_.Type -eq "dns-01"}
            
            ConvertTo-Json $Challenges
        }
    }
}

& {
    param(
        [string]$ChallengeId
    ) 

    $ErrorActionPreference = "Stop" 

    Import-Module ACMESharp

    Submit-Request $ChallengeId

 } `
 (Get-Param 'ChallengeId' -Required)
```

### Check Challenge
You can check on the status of the DNS challenge using this script. You must provide the ChallengeId from the Create Challenge step.

```powershell
param(
    [string]$ChallengeId
)

function Get-Param($Name, [switch]$Required, $Default) {
    $result = $null
    
    if ($OctopusParameters -ne $null) {
        $result = $OctopusParameters[$Name]
    }

    if ($result -eq $null) {
        $variable = Get-Variable $Name -EA SilentlyContinue    
        if ($variable -ne $null) {
            $result = $variable.Value
        }
    }

    $testResult = [string]$result

    if ([string]::IsNullOrEmpty($testResult) -eq $true) {
        if ($Required) {
            throw "Missing parameter value $Name"
        } else {
            $result = $Default
        }
    }

    return $result
}

function Check-Challenge([string]$challengeId){
    $Identifier = $null

    try {
        $Identifier = Get-ACMEIdentifier | ? { $_.Alias -eq $challengeId }  
    }
    catch {
        Write-Host "No identifier found for Challenge Id $challengeId"
    }

    if ($Identifier -ne $null) {
        $UpdatedIdentifier = (Update-ACMEIdentifier $challengeId -ChallengeType dns-01)
        
        $domain = $UpdatedIdentifier.Identifier
        
        Write-Host "Current status of $domain is $($UpdatedIdentifier.Status)"
        
        ConvertTo-Json $UpdatedIdentifier
        
        if ($UpdatedIdentifier.Status -eq "pending") {
            Write-Host "Found pending challenges"
            $Challenges = $UpdatedIdentifier.Challenges | Where-Object {$_.Type -eq "dns-01"}
            
            ConvertTo-Json $Challenges
        }
    }
}

& {
    param(
        [string]$ChallengeId
    ) 

    $ErrorActionPreference = "Stop" 

    Import-Module ACMESharp

    Check-Challenge $ChallengeId

 } `
 (Get-Param 'ChallengeId' -Required)
```

### Request Certificate
You can now request a certificate if the domain validation challenge has been successful.

```powershell
param(
    [string]$ChallengeId
)

function Get-Param($Name, [switch]$Required, $Default) {
    $result = $null
    
    if ($OctopusParameters -ne $null) {
        $result = $OctopusParameters[$Name]
    }

    if ($result -eq $null) {
        $variable = Get-Variable $Name -EA SilentlyContinue    
        if ($variable -ne $null) {
            $result = $variable.Value
        }
    }

    $testResult = [string]$result

    if ([string]::IsNullOrEmpty($testResult) -eq $true) {
        if ($Required) {
            throw "Missing parameter value $Name"
        } else {
            $result = $Default
        }
    }

    return $result
}

function Retrieve-Certificate([string]$challengeId){

    New-ACMECertificate $challengeId -Generate -Alias $challengeId
    Submit-ACMECertificate $challengeId

    $Identifier = $null

    try {
        $Identifier = Update-ACMECertificate $challengeId
    }
    catch {
        Write-Host "No certificate found for Challenge Id $challengeId"
    }

    if ($Identifier -ne $null) {
        if ([string]::IsNullOrEmpty($Identifier.SerialNumber)) {
            Write-Host "The certificate has not yet been issued."
        }
        else {
            Write-Host "The certificate has been issued with serial number $($Identifier.SerialNumber)"
        }
    }
}

& {
    param(
        [string]$ChallengeId
    ) 

    $ErrorActionPreference = "Stop" 

    Import-Module ACMESharp

    Retrieve-Certificate $ChallengeId

 } `
 (Get-Param 'ChallengeId' -Required)
```

### Check Certificate Status
The certificate may not be immediately issued by Let's Encrypt. This step will check on the status of the certificate.

```powershell
param(
    [string]$ChallengeId
)

function Get-Param($Name, [switch]$Required, $Default) {
    $result = $null
    
    if ($OctopusParameters -ne $null) {
        $result = $OctopusParameters[$Name]
    }

    if ($result -eq $null) {
        $variable = Get-Variable $Name -EA SilentlyContinue    
        if ($variable -ne $null) {
            $result = $variable.Value
        }
    }

    $testResult = [string]$result

    if ([string]::IsNullOrEmpty($testResult) -eq $true) {
        if ($Required) {
            throw "Missing parameter value $Name"
        } else {
            $result = $Default
        }
    }

    return $result
}

function Retrieve-Certificate([string]$challengeId){

    $Identifier = $null

    try {
        $Identifier = Update-ACMECertificate $challengeId
    }
    catch {
        Write-Host "No certificate found for Challenge Id $challengeId"
    }

    if ($Identifier -ne $null) {
        if ([string]::IsNullOrEmpty($Identifier.SerialNumber)) {
            Write-Host "The certificate has not yet been issued."
        }
        else {
            Write-Host "The certificate has been issued with serial number $($Identifier.SerialNumber)"
        }
    }
}

& {
    param(
        [string]$ChallengeId
    ) 

    $ErrorActionPreference = "Stop" 

    Import-Module ACMESharp

    Retrieve-Certificate $ChallengeId

 } `
 (Get-Param 'ChallengeId' -Required)
```

### Install Certificate
If all has gone well, the server now has a Let's Encrypt certificate in its ACME certificate vault. This step script optionally installs the certificate thumbprint as the RDP session certificate, then configures the Octopus Deploy server to use the new certificate for hosting itself over HTTPS. 

I never got to test this script as I did this part manually. This might fail miserably. Shout out in the comments if you manage to run this.

```powershell
param(
    [string]$ChallengeId,
    [bool]$UseForRDP
)

function Get-Param($Name, [switch]$Required, $Default) {
    $result = $null
    
    if ($OctopusParameters -ne $null) {
        $result = $OctopusParameters[$Name]
    }

    if ($result -eq $null) {
        $variable = Get-Variable $Name -EA SilentlyContinue    
        if ($variable -ne $null) {
            $result = $variable.Value
        }
    }

    $testResult = [string]$result

    if ([string]::IsNullOrEmpty($testResult) -eq $true) {
        if ($Required) {
            throw "Missing parameter value $Name"
        } else {
            $result = $Default
        }
    }

    return $result
}

function Build-Password([int]$length=20) {
    $sourcedata = $null
    
    for ($a=33; $a –le 126; $a++) {
        $sourcedata+=,[char][byte]$a 
    }

    $password = ""

    for ($loop=1; $loop –le $length; $loop++) {
        $password += ($sourcedata | GET-RANDOM)
    }

    return $password
}

function Install-Certificate([string]$challengeId, [bool]$useForRDP){

    $password = Build-Password
    $securePassword = ConvertTo-SecureString $password -AsPlainText -Force
    $certificatePath = [System.IO.Path]::GetTempPath() + "\$challengeId.pfx";

    Write-Host "Exporting certificate to $certificatePath"
    $certificate = Get-ACMECertificate $challengeId -ExportPkcs12 $certificatePath -CertificatePassword $password

    Write-Host "Importing certificate into computer certificate store"
    Import-PfxCertificate $certificatePath cert:\localMachine\my -Password $securePassword

    if ($useForRDP -eq $true) {
        Write-Host "Setting RDP thumbprint to $($certificate.Thumbprint)"
        $path = (Get-WmiObject -class "Win32_TSGeneralSetting" -Namespace root\cimv2\terminalservices -Filter "TerminalName='RDP-tcp'").__path
        Set-WmiInstance -Path $path -argument @{SSLCertificateSHA1Hash=$certificate.Thumbprint}
    }
    
    Write-Host "Configuring certificate with thumbprint $($certificate.Thumbprint) against http.sys"
    &netsh @("http", "delete", "sslcert", "ipport=0.0.0.0:443")
    &netsh @("http", "add", "sslcert", "ipport=0.0.0.0:443", "certhash=$($certificate.Thumbprint)", "certstorename=My")

    Write-Host "Configuring certificate with thumbprint $($certificate.Thumbprint) against Octopus Deploy server"
    $server = "C:\Program Files\Octopus Deploy\Octopus\Octopus.Server.exe"
    &$server @("configure", "--instance", "OctopusServer", "--webForceSSL", "False")
    &$server @("configure", "--instance", "OctopusServer", "--webListenPrefixes", "https://localhost/")
    &$server @("service", "--instance", "OctopusServer", "--stop", "--start")
}

& {
    param(
        [string]$ChallengeId,
        [bool]$UseForRDP
    ) 

    $ErrorActionPreference = "Stop" 

    Import-Module ACMESharp

    Install-Certificate $ChallengeId, $UserForRDP

 } `
 (Get-Param 'ChallengeId' -Required) `
 (Get-Param 'UseForRDP' -Default $false)
```

## Future Plans
I have another Octopus Deploy server that is publically available over HTTPS. I would like to join all these scripts together to do all of these in one powershell script using HTTP validation via IIS using ACMESharp. This may not even be an Octopus Deploy step. Instead full automation could be achieved by executing the powershell using a Windows scheduled task.
