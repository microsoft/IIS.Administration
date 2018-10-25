# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("IsAvailable",
                 "GetAvailable",
                 "HasSslBinding",
                 "GetSslBindingInfo",
                 "BindCert",
                 "DeleteSslBinding",
                 "CopySslBindingInfo")]
    [string]
    $Command,
    
    [parameter()]
    [int]
    $Port,
    
    [parameter()]
    [string]
    $Hash,
    
    [parameter()]
    [string]
    $AppId,
    
    [parameter()]
    [int]
    $SourcePort,
    
    [parameter()]
    [int]
    $DestinationPort
)

$MAX_PORT = 65535
$MIN_PORT = 1

function ValidatePort($portNo) {
    if ($portNo -lt $MIN_PORT -or $portNo -gt $MAX_PORT) {
        throw "Please specify a valid port. ($MIN_PORT - $MAX_PORT)"
    }
}

# Tests whether the specified port is available.
# Port: The port to test.
function PortAvailable($portNo)
{
    ValidatePort($portNo)
    $listener = ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()).GetActiveTcpListeners() | where {
        $_.Port -eq "$portNo"
    }
    return $listener -eq $null
}

# Retrieves the first available port at or after the provided start port.
# Port: The port to start scanning on.
function GetAvailablePort($startPort) {

    ValidatePort($startPort)

	$available = PortAvailable $startPort

	while ($startPort -le 65535) {

		if ($available) {
			return $startPort
		}

		$startPort = $startPort + 1
		$available = PortAvailable $startPort
	}

	throw "No available port found"
}

# Tests whether an SSL binding exists for the specified port. The binding is assumed to listen on the broadcast IP Address 0.0.0.0
# Port: The port to test.
function SslBindingExists($portNo)
{
    ValidatePort $portno

    $ipEndpoint = New-Object "System.Net.IPEndPoint" -ArgumentList ([System.Net.IPAddress]::Any, $portNo)
    $binding = .\netsh.ps1 Get-SslBinding -IpEndpoint $ipEndpoint

    return $binding -ne $null
}

# Gets the binding info that is used for a given port.
# Port: The port to retrieve info for.
function GetBoundCertificateInfo($portNo) {

    ValidatePort($portNo)

    $ipEndpoint = New-Object "System.Net.IPEndPoint" -ArgumentList ([System.Net.IPAddress]::Any, $portNo)
    return .\netsh.ps1 Get-SslBinding -IpEndpoint $ipEndpoint
}

# Binds a certificate to a specified port in HTTP.Sys.
# Hash: The thumbprint (hash) of the certificate to bind.
# Port: The port to bind to.
# AppId: The unique application id used to bind the certificate.
function BindCertificate($_hash, $portNo, $_appId)
{
    ValidatePort($portNo)
    
    $ipEndpoint = New-Object "System.Net.IPEndPoint" -ArgumentList ([System.Net.IPAddress]::Any, $portNo)
    $certificate = Get-Item "Cert:\LocalMachine\my\$_hash"

    .\netsh.ps1 Add-SslBinding -IpEndpoint $ipEndpoint -Certificate $certificate -AppId $_appId
}

# Deletes an HTTP.Sys binding for the specified port.
# Port: The target port.
function DeleteHttpsBinding($portNo)
{
    ValidatePort($portNo)

    if(SslBindingExists $portNo) {
        $ipEndpoint = New-Object "System.Net.IPEndPoint" -ArgumentList ([System.Net.IPAddress]::Any, $portNo)
        .\netsh.ps1 Delete-SslBinding -IpEndpoint $ipEndpoint
    }
    else {
        Write-Verbose "No HTTP.Sys binding exists for port $portNo"
    }
}

# Migrates the settings for an HTTP.Sys binding from one port to another
# SourcePort: The port which contains the HTTP.Sys binding
# DestinationPort: The port to migrate the binding settings to
function Copy-SslBindingInfo($sourcePort, $destPort) {
    ValidatePort($sourcePort)
    ValidatePort($destPort)

    if ($sourcePort -ne $destPort) {
        $sourceInfo = GetBoundCertificateInfo $sourcePort

        if ($sourceInfo -eq $null -or $sourceInfo.CertificateHash -eq $null) {
            throw "Source binding info not found"
        }

        $sourceCert = Get-Item "Cert:\LocalMachine\my\$($sourceInfo.CertificateHash)"

        if ($sourceCert -eq $null) {
            throw "Source binding certificate not found"
        }

        DeleteHttpsBinding $destPort
        BindCertificate -_hash $sourceCert.ThumbPrint -portNo $destPort -_appId $sourceInfo.AppId
    }

    GetBoundCertificateInfo $destPort
}

switch($Command)
{
    "IsAvailable"
    {
        return PortAvailable $Port
    }
    "GetAvailable"
    {
        return GetAvailablePort $Port
    }
    "HasSslBinding"
    {
        return SslBindingExists $Port
    }
    "GetSslBindingInfo"
    {
        return GetBoundCertificateInfo $Port
    }
    "CopySslBindingInfo"
    {
        return Copy-SslBindingInfo $SourcePort $DestinationPort
    }
    "BindCert"
    {
        return BindCertificate $Hash $Port $AppId
    }
    "DeleteSslBinding"
    {
        return DeleteHttpsBinding $Port
    }
    default
    {
        throw "Unknown command"
    }
}

