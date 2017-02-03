---
title: Octopus Deploy certificate health check
categories: .Net
tags: 
date: 2017-02-03 11:50:00 +10:00
---

I [posted][0] the other day about trying to integrate Let's Encrypt with Octopus Deploy. Nathan Stohlmann had a [great idea][1] of using machine policy health checks as a way of triggering certificate renewal of Octopus Deploy. Here is the powershell to do just that.

<!--more-->

There were a couple of issues found when creating this script. 

Firstly, machine policy scripts don't support parameters so this had to be written as more of an inline script. 

Secondly, there is no automated way of resolving the https endpoint of the server. The script must be executed as part of a machine policy being evaluated against a tentacle. Originally the script got the configuration from ```Octopus.Server.exe show-configuration``` to identify the https endpoints. This didn't work because the tentacle does not have permissions to the Octopus database and this failed. The Octopus parameters provided to the machine policy script also don't provide any information about the server. Unfortunately this had to be manually set as a variable in the script.

**Note:** To use this script you will need to create a new machine policy specifically for a tentacle that is running on the Octopus Deploy server. We are running a health check against that tentacle as a way of indicating a problem with the server.

```powershell
function Check-DiskSpace([int] $threshold) {
    $freeDiskSpaceThreshold = 5GB
    Try {
	    Get-WmiObject win32_LogicalDisk -ErrorAction Stop  | ? { ($_.DriveType -eq 3) -and ($_.FreeSpace -ne $null)} |  % { CheckDriveCapacity @{Name =$_.DeviceId; FreeSpace=$_.FreeSpace} }
    } Catch [System.Runtime.InteropServices.COMException] {
	    Get-WmiObject win32_Volume | ? { ($_.DriveType -eq 3) -and ($_.FreeSpace -ne $null) -and ($_.DriveLetter -ne $null)} | % { CheckDriveCapacity @{Name =$_.DriveLetter; FreeSpace=$_.FreeSpace} }
	    Get-WmiObject Win32_MappedLogicalDisk | ? { ($_.FreeSpace -ne $null) -and ($_.DeviceId -ne $null)} | % { CheckDriveCapacity @{Name =$_.DeviceId; FreeSpace=$_.FreeSpace} }	
    }
}

function Get-RemainingCertificateDays([string] $uri) {

    $originalValidationCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback

    #disabling the cert validation check. This is what makes this whole thing work with invalid certs...
    [Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

    try {
        Write-Host Checking certificate validity period for $uri -f Green
        $req = [Net.HttpWebRequest]::Create($uri)
        $req.Timeout = $timeoutMilliseconds

        try {
            $req.GetResponse() | Out-Null
        } 
        catch {
            Write-Host Exception while checking URL $uri`: $_ -f Red
        }

        $certificate = $req.ServicePoint.Certificate
                
        rv req
        
        [datetime]$expiration = $certificate.GetExpirationDateString()
        [int]$remainingDays = ($expiration - $(get-date)).Days

        $certName = $certificate.GetName()
        $certPublicKeyString = $certificate.GetPublicKeyString()
        $certSerialNumber = $certificate.GetSerialNumberString()
        $certThumbprint = $certificate.GetCertHashString()
        $certEffectiveDate = $certificate.GetEffectiveDateString()
        $certIssuer = $certificate.GetIssuerName()
        
        Write-Host Certificate for site $uri expires in $remainingDays days [on $expiration]. `n`nCert name: $certName`nCert public key: $certPublicKeyString`nCert serial number: $certSerialNumber`nCert thumbprint: $certThumbprint`nCert effective date: $certEffectiveDate`nCert issuer: $certIssuer

        return $remainingDays
    }
    finally {
        [Net.ServicePointManager]::ServerCertificateValidationCallback = $originalValidationCallback
    }
}

[string]$Uri = "https://localhost"
[int]$MinimumCertAgeDays = 30

Check-DiskSpace

[int] $daysRemaining = Get-RemainingCertificateDays $Uri

if ($daysRemaining -lt 1) {
    Write-Error "Certificate for site $Uri has expired"
} 
elseif ($daysRemaining -gt $MinimumCertAgeDays) {
    Write-Host "Certificate for site $Uri expires in $daysRemaining days"
}
else {
    Write-Warning "Certificate for site $Uri expires in $daysRemaining days"
}
```

Running this in a machine policy will now provide an error if the certificate has expired, a warning if it will expire within 30 days or a successful outcome if the certificate expires in more than 30 days.

## Update

I started seeing immediate timeout responses using HttpWebRequest. Here is an alternative script which requires an IP address instead of a hostname.

```powershell
function Check-DiskSpace([int] $threshold) {
    $freeDiskSpaceThreshold = 5GB
    Try {
	    Get-WmiObject win32_LogicalDisk -ErrorAction Stop  | ? { ($_.DriveType -eq 3) -and ($_.FreeSpace -ne $null)} |  % { CheckDriveCapacity @{Name =$_.DeviceId; FreeSpace=$_.FreeSpace} }
    } Catch [System.Runtime.InteropServices.COMException] {
	    Get-WmiObject win32_Volume | ? { ($_.DriveType -eq 3) -and ($_.FreeSpace -ne $null) -and ($_.DriveLetter -ne $null)} | % { CheckDriveCapacity @{Name =$_.DriveLetter; FreeSpace=$_.FreeSpace} }
	    Get-WmiObject Win32_MappedLogicalDisk | ? { ($_.FreeSpace -ne $null) -and ($_.DeviceId -ne $null)} | % { CheckDriveCapacity @{Name =$_.DeviceId; FreeSpace=$_.FreeSpace} }	
    }
}

function Get-RemoteCertificate([string]$ip,[int] $Port)
{
    $TCPClient = New-Object -TypeName System.Net.Sockets.TCPClient

    try
    {
        write-host $($entries | Out-String)
        $TcpSocket = New-Object Net.Sockets.TcpClient($ip,$port)
        $tcpstream = $TcpSocket.GetStream()
        $Callback = {param($sender,$cert,$chain,$errors) return $true}
        $SSLStream = New-Object -TypeName System.Net.Security.SSLStream -ArgumentList @($tcpstream, $True, $Callback)

        try
        {
            $SSLStream.AuthenticateAsClient($uri)
            $Certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($SSLStream.RemoteCertificate)
        }
        finally
        {
            $SSLStream.Dispose()
        }
    }
    finally
    {
        $TCPClient.Dispose()
    }

    return $Certificate
}

function Get-RemainingCertificateDays([string] $uri) {

    $originalValidationCallback = [Net.ServicePointManager]::ServerCertificateValidationCallback

    #disabling the cert validation check. This is what makes this whole thing work with invalid certs...
    [Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

    try {
        Write-Host Checking certificate validity period for $uri -f Green

        $certificate = Get-RemoteCertificate $uri 443
        
        [datetime]$expiration = $certificate.GetExpirationDateString()
        [int]$remainingDays = ($expiration - $(get-date)).Days

        $certName = $certificate.GetName()
        $certPublicKeyString = $certificate.GetPublicKeyString()
        $certSerialNumber = $certificate.GetSerialNumberString()
        $certThumbprint = $certificate.GetCertHashString()
        $certEffectiveDate = $certificate.GetEffectiveDateString()
        $certIssuer = $certificate.GetIssuerName()
        
        Write-Host Certificate for site $uri expires in $remainingDays days [on $expiration]. `n`nCert name: $certName`nCert public key: $certPublicKeyString`nCert serial number: $certSerialNumber`nCert thumbprint: $certThumbprint`nCert effective date: $certEffectiveDate`nCert issuer: $certIssuer

        return $remainingDays
    }
    finally {
        [Net.ServicePointManager]::ServerCertificateValidationCallback = $originalValidationCallback
    }
}

[string]$Uri = "127.0.0.1"
[int]$MinimumCertAgeDays = 30

Check-DiskSpace

[int] $daysRemaining = Get-RemainingCertificateDays $Uri

if ($daysRemaining -lt 1) {
    Write-Error "Certificate for site $Uri has expired"
} 
elseif ($daysRemaining -gt $MinimumCertAgeDays) {
    Write-Host "Certificate for site $Uri expires in $daysRemaining days"
}
else {
    Write-Warning "Certificate for site $Uri expires in $daysRemaining days"
}
```

[0]: /2017/02/01/octopus-deploy-lets-encrypt-dns/
[1]: http://disq.us/p/1ftrgzz