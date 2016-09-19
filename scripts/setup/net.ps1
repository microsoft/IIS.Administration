# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("IsAvailable",
                 "GetAvailable",
                 "HasSslBinding",
                 "GetSslBindingInfo",
                 "BindCert",
                 "DeleteSslBinding")]
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
    $AppId
)

$MAX_PORT = 65535
$MIN_PORT = 1

function ValidatePort($portNo) {
    if ($portNo -lt $MIN_PORT -or $portNo -gt $MAX_PORT) {
        throw "Please specify a valid port. ($MIN_PORT - $MAX_PORT)"
    }
}

function PortAvailable($portNo)
{
    ValidatePort($portNo)

    $tcp = New-Object System.Net.Sockets.TcpClient
    Try
    {
        $tcp.connect('localhost', $portNo)
        return $false
    }
    Catch
    {
        return $true
    }
    Finally
    {
        $tcp.Close()
    }
}

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

function SslBindingExists($portNo)
{
    ValidatePort $portno

    $ipEndpoint = New-Object "System.Net.IPEndPoint" -ArgumentList ([System.Net.IPAddress]::Any, $portNo)
    $binding = .\netsh.ps1 Get-SslBinding -IpEndpoint $ipEndpoint

    return $binding -ne $null
}

function GetBoundCertificateInfo($portNo) {

    ValidatePort($portNo)

    $ipEndpoint = New-Object "System.Net.IPEndPoint" -ArgumentList ([System.Net.IPAddress]::Any, $portNo)
    return .\netsh.ps1 Get-SslBinding -IpEndpoint $ipEndpoint
}

function BindCertificate($_hash, $portNo, $_appId)
{
    ValidatePort($portNo)
    
    $ipEndpoint = New-Object "System.Net.IPEndPoint" -ArgumentList ([System.Net.IPAddress]::Any, $portNo)
    $certificate = Get-Item "Cert:\LocalMachine\my\$_hash"

    .\netsh.ps1 Add-SslBinding -IpEndpoint $ipEndpoint -Certificate $certificate -AppId $_appId
}

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

