# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Get",
                 "Delete",
                 "New",
                 "AddToTrusted",
                 "Create",
                 "Get-IISAdminCertificates",
                 "Get-LatestIISAdminCertificate",
                 "Is-IISAdminCertificate")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Name,
    
    [parameter()]
    [string]
    $Thumbprint,
    
    [parameter()]
    [System.Security.Cryptography.X509Certificates.X509Certificate]
    $Certificate,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]
    $Subject,

    [Parameter()]
    [string]
    $FriendlyName = "",

    [Parameter()]
    [string[]]
    $AlternativeNames = ""
)

# Retrieves an X509 certificate from the 'local machine\my' store.
# Name: Used to filter the certificate by Dns Name or Friendly Name
# Thumbprint: Used to filter the certificate by its thumbprint (hash).
function GetCert($_name, $_thumbprint)
{

	if ([System.String]::IsNullOrEmpty($_name) -and [System.String]::IsNullOrEmpty($_thumbprint)) {
		throw "Name or Thumpbrint required"
	}
    
    if (-not([System.String]::IsNullOrEmpty($_name))) {
        $certs = Get-ChildItem Cert:\LocalMachine\My | where {
                     ($_.DnsNameList -ne $null -and $_.DnsNameList.Contains("$_name")) -or
                     ($_.FriendlyName -eq $_name) -or
                     ($_.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::DnsFromAlternativeName, $false) -eq $_name)
                 }
    }
    else {
        $certs = Get-ChildItem Cert:\LocalMachine\My | where {$_.Thumbprint -eq $_thumbprint}
    }

    if ($certs.Length -gt 1) {
        return $certs[0]
    }
    return $certs
}

# Deletes an X509 certificate from the local machine.
# Name: Used to filter the certificate by Dns Name or Friendly Name
# Thumbprint: Used to filter the certificate by its thumbprint (hash).
function DeleteCert($_name, $_thumbprint)
{	
	if ([System.String]::IsNullOrEmpty($_name) -and [System.String]::IsNullOrEmpty($_thumbprint)) {
		throw "Name or Thumpbrint required"
	}    
    
    if (-not([System.String]::IsNullOrEmpty($_name))) {
        $files = Get-ChildItem -Recurse Cert:\LocalMachine | where {
                     ($_ -is [System.Security.Cryptography.X509Certificates.X509Certificate]) -and
                     (($_.DnsNameList -ne $null -and $_.DnsNameList.Contains("$_name")) -or
                     ($_.FriendlyName -eq $_name) -or
                     ($_.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::DnsFromAlternativeName, $false) -eq $_name))
                 }
    }
    else {
        $files = Get-ChildItem -Recurse Cert:\LocalMachine | where {($_ -is [System.Security.Cryptography.X509Certificates.X509Certificate]) -and ($_.Thumbprint -eq $_thumbprint)}
    }
    foreach ($file in $files){
        $store = Get-Item $file.PSParentPath
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
        $store.Remove($file)

        $closeMember = $store | Get-Member -Name "Close"
        $disposeMember = $store | Get-Member -Name "Dispose"
    
        # Close gone on Nano Server
        if ($closeMember -ne $null) {
            #Close the reference to the certificate store.
            $store.Close()
        }
        if ($disposeMember -ne $null) {
            $store.Dispose()
        }
    }
}

# Creates a new X509 certificate in the 'local machine\my' store.
# Name: The friendly name for the certificate. Also included in subject alternative names list.
function New($_name)
{
	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name cannot be null"
	}

    $dnsNames = @()
    $dnsNames += "localhost"
	$dnsNames += hostname
	$dnsNames += [System.Net.Dns]::GetHostByName($(hostname)).HostName
	$dnsNames += "$_name"
	return Create-SelfSignedCertificate "localhost" $_name $dnsNames -ErrorAction Stop
}

# Added a specified x509certificate to the 'local machine\root' trusted store.
# Cert: The certificate to add to the trusted store
function AddToTrusted($cert)
{
    if ($cert -eq $null) {
        throw "Certificate cannot be null"
    }

    #Create a certificate store object
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store "Root","LocalMachine"

    #Open the certificate store to begin modfications.
    $store.Open("ReadWrite")

    #Store the certificate that we created.
    $store.Add($cert)

    $closeMember = $store | Get-Member -Name "Close"
    $disposeMember = $store | Get-Member -Name "Dispose"
    
    # Close gone on Nano Server
    if ($closeMember -ne $null) {
        #Close the reference to the certificate store.
        $store.Close()
    }
    if ($disposeMember -ne $null) {
        $store.Dispose()
    }
}

# Create a self signed certificate
# Subject: The Subject (DNS Name) for the certificate
# FriendlyName: The friendly name for the certificate
# AlternativeNames: A list of alternative names used to identify the certificate
function Create-SelfSignedCertificate($_subject, $_friendlyName, $_alternativeNames) {
    if ($_subject -eq $null) {
        throw "Subject required."
    }

    $subjectDn = new-object -com "X509Enrollment.CX500DistinguishedName"
    $subjectDn.Encode( "CN=" + $_subject, 0)
    $issuer = $_subject
    $issuerDn = new-object -com "X509Enrollment.CX500DistinguishedName"
    $issuerDn.Encode("CN=" + $issuer, 0)

    #
    # Create a new Private Key
    $key = new-object -com "X509Enrollment.CX509PrivateKey"
    $key.ProviderName =  "Microsoft RSA SChannel Cryptographic Provider"    
    # XCN_AT_SIGNATURE, The key can be used for signing
    $key.Length = 2048
    # MachineContext 0: Current User, 1: Local Machine
    $key.MachineContext = 1
    $key.Create() 

    $cert = new-object -com "X509Enrollment.CX509CertificateRequestCertificate"
    $cert.InitializeFromPrivateKey(2, $key, "")
    $cert.Subject = $subjectDn
    $cert.Issuer = $issuerDn
    $cert.NotBefore = (get-date).ToUniversalTime().AddMinutes(-10)
    $cert.NotAfter = $cert.NotBefore.AddYears(2)
    #Use Sha256
    $hashAlgorithm = New-Object -ComObject X509Enrollment.CObjectId
    $hashAlgorithm.InitializeFromAlgorithmName(1,0,0,"SHA256")
    $cert.HashAlgorithm = $hashAlgorithm    
	 
    #
    # Extended key usage
    $clientAuthOid = New-Object -ComObject "X509Enrollment.CObjectId"
    $clientAuthOid.InitializeFromValue("1.3.6.1.5.5.7.3.2")
    $serverAuthOid = new-object -com "X509Enrollment.CObjectId"
    $serverAuthOid.InitializeFromValue("1.3.6.1.5.5.7.3.1")
    $ekuOids = new-object -com "X509Enrollment.CObjectIds.1"
    $ekuOids.add($clientAuthOid)
    $ekuOids.add($serverAuthOid)
    $ekuExt = new-object -com "X509Enrollment.CX509ExtensionEnhancedKeyUsage"
    $ekuExt.InitializeEncode($ekuOids)
    $cert.X509Extensions.Add($ekuext)
	
    #
    # Key usage
    $keyUsage = New-Object -com "X509Enrollment.cx509extensionkeyusage"
    # XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE
    $flags = 0x20
    # XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE
    $flags = $flags -bor 0x80
    $keyUsage.InitializeEncode($flags)
    $cert.X509Extensions.Add($keyUsage)

    #
    # Subject alternative names
    if ($_alternativeNames -ne $null) {
        $names =  new-object -com "X509Enrollment.CAlternativeNames"
        $altNames = new-object -com "X509Enrollment.CX509ExtensionAlternativeNames"
        foreach ($n in $_alternativeNames) {
            $name = new-object -com "X509Enrollment.CAlternativeName"
            # Dns Alternative Name
            $name.InitializeFromString(3, $n)
            $names.Add($name)
        }
        $altNames.InitializeEncode($names)
        $cert.X509Extensions.Add($altNames)
    }

    $cert.Encode()

    $locator = $_friendlyName
    if ($locator -eq $null) {
        $locator = $(New-Object "System.Guid").ToString()
    }
    $enrollment = new-object -com "X509Enrollment.CX509Enrollment"
    $enrollment.CertificateFriendlyName = $locator
    $enrollment.InitializeFromRequest($cert)
    $certdata = $enrollment.CreateRequest(0)
    $enrollment.InstallResponse(2, $certdata, 0, "")

    # Wait for certificate to be populated
    $end = $(Get-Date).AddSeconds(1)
    do {
        $CACertificate = (Get-ChildItem Cert:\LocalMachine\My | Where-Object {$_.FriendlyName -eq $locator })
    } while ($CACertificate -eq $null -and $(Get-Date) -lt $end)

    if ($CACertificate.Length -ne $null) {
        $dates = $CACertificate | %{$_.NotBefore} | Sort-Object
        $CACertificate = $CACertificate | where {$_.NotBefore -eq $dates[$dates.length - 1]}
    }

    return $CACertificate 
}

function Get-IISAdminCerts {
    $certName = .\globals.ps1 CERT_NAME
    $adminCerts = @()

    $allCerts = Get-ChildItem Cert:\LocalMachine\My

    foreach ($cert in $allCerts) {
        # Handle ps 2.0 empty enumerable behavior
        if ($cert -eq $null) {
            continue
        }

        if ($cert.DnsNameList -ne $null) {
            $entryLength = $adminCerts.Length
            foreach ($dnsName in $cert.DnsNameList) {
                if ($dnsName.ToString().ToLower().StartsWith($certName.ToLower())) {
                    $adminCerts += $cert
                    break
                }
            }

            if ($adminCerts.Length -eq ($entryLength + 1)) {
                continue
            }
        }

        if ($cert.FriendlyName -ne $null -and $cert.FriendlyName.ToLower().StartsWith($certName.ToLower())) {
            $adminCerts += $cert
            continue
        }

        $dnsName = $cert.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::DnsFromAlternativeName, $false)
        if ($dnsName -ne $null  -and $dnsName.ToString().ToLower().StartsWith($certName.ToLower())) {
            $adminCerts += $cert
            continue
        }
    }
    
    $adminCerts
}

# Tests whether a provided certificate is an IIS Administration generated certificate.
# Thumbprint: Used to filter the certificate by its thumbprint (hash).
function Is-IISAdminCert($_thumbprint) {

    $ret = $false

    $certs = Get-IISAdminCerts
    
    foreach ($cert in $certs) {

      if ($cert.Thumbprint -eq $_thumbprint) {
          
          $ret = $true

          break
      }
    }

    $ret
}

function Get-LatestIISAdminCert {
    $cert = $null
    $certs = Get-IISAdminCerts

    if ($certs -ne $null -and $certs.length -ne $null) {
        $expirationDates = $certs | %{$_.NotAfter} | Sort-Object
        $cert = $certs | where {$_.NotAfter -eq $expirationDates[$expirationDates.length - 1]}
    }
    else {
        $cert = $certs
    }

    $cert
}

switch ($Command)
{
    "Get"
    {
        return GetCert $Name $Thumbprint
    }
    "Get-IISAdminCertificates"
    {
        return Get-IISAdminCerts
    }
    "Get-LatestIISAdminCertificate"
    {
        return Get-LatestIISAdminCert
    }
    "Is-IISAdminCertificate"
    {
        return Is-IISAdminCert $Thumbprint
    }
    "Delete"
    {
        return DeleteCert $Name $Thumbprint
    }
    "New"
    {
        return New $Name
    }
    "AddToTrusted"
    {
        return AddToTrusted $Certificate
    }
    "Create"
    {
        return Create-SelfSignedCertificate $Subject $FriendlyName $AlternativeNames
    }
    default
    {
        throw "Unknown command"
    }
}


