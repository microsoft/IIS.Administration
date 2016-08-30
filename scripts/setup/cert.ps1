# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Get",
                 "Delete",
                 "New",
                 "AddToTrusted")]
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
    $Certificate
)

function GetCert($_name, $_thumbprint)
{

	if ([System.String]::IsNullOrEmpty($_name) -and [System.String]::IsNullOrEmpty($_thumbprint)) {
		throw "Name or Thumpbrint required"
	}
    
    if (-not([System.String]::IsNullOrEmpty($_name))) {
        $certs = Get-ChildItem Cert:\LocalMachine\My | where {$_.DnsNameList -ne $null -and $_.DnsNameList.Contains("$_name")}
    }
    else {
        $certs = Get-ChildItem Cert:\LocalMachine\My | where {$_.Thumbprint -eq $_thumbprint}
    }

    if ($certs.Length -gt 1) {
        return $certs[0]
    }
    return $certs
}

function DeleteCert($_name, $_thumbprint)
{	
	if ([System.String]::IsNullOrEmpty($_name) -and [System.String]::IsNullOrEmpty($_thumbprint)) {
		throw "Name or Thumpbrint required"
	}    
    
    if (-not([System.String]::IsNullOrEmpty($_name))) {
        $files = Get-ChildItem -Recurse cert:\LocalMachine | where {$_.DnsNameList -ne $null -and $_.DnsNameList.Contains("$_name")}
    }
    else {
        $files = Get-ChildItem -Recurse Cert:\LocalMachine | where {$_.Thumbprint -eq $_thumbprint}
    }
    foreach ($file in $files){
        remove-item $file.PSPath 
    }
}

function New($_name)
{
	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name cannot be null"
	}

    $dnsNames = @()
    $dnsNames += "localhost"
	$dnsNames += hostname
	$dnsNames += "$_name"
	return .\Create-SelfSignedCertificate.ps1 -subject "localhost" -AlternativeNames $dnsNames -FriendlyName $_name -ErrorAction Stop
}

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

switch ($Command)
{
    "Get"
    {
        return GetCert $Name $Thumbprint
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
    default
    {
        throw "Unknown command"
    }
}


