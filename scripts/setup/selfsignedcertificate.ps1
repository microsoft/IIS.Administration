# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Create")]
    [string]
    $Command,

    [Parameter(Mandatory=$True)]
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

function Create-SelfSignedCertificate($_subject, $_friendlyName, $_alternativeNames) {
    $subjectDn = new-object -com "X509Enrollment.CX500DistinguishedName"
    $subjectDn.Encode( "CN=" + $_subject, $subjectDn.X500NameFlags.X500NameFlags.XCN_CERT_NAME_STR_NONE)
    $issuer = $_subject
    $issuerDn = new-object -com "X509Enrollment.CX500DistinguishedName"
    $issuerDn.Encode("CN=" + $issuer, $subjectDn.X500NameFlags.X500NameFlags.XCN_CERT_NAME_STR_NONE)

    #
    # Create a new Private Key
    $key = new-object -com "X509Enrollment.CX509PrivateKey"
    $key.ProviderName =  "Microsoft Enhanced RSA and AES Cryptographic Provider"    
    # XCN_AT_SIGNATURE, The key can be used for signing
    $key.KeySpec = 2
    $key.Length = 2048
    # MachineContext 0: Current User, 1: Local Machine
    $key.MachineContext = 1
    $key.Create() 

    $cert = new-object -com "X509Enrollment.CX509CertificateRequestCertificate"
    $cert.InitializeFromPrivateKey(2, $key, "")
    $cert.Subject = $subjectDn
    $cert.Issuer = $issuerDn
    $cert.NotBefore = (get-date).AddMinutes(-10)
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

    $locator = $(New-Object "System.Guid").ToString()
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
    $CACertificate.FriendlyName = $_friendlyName

    return $CACertificate 
}

switch ($Command)
{
    "Create"
    {
        return Create-SelfSignedCertificate $Subject $FriendlyName $AlternativeNames
    }
    default
    {
        throw "Unknown command"
    }
}

