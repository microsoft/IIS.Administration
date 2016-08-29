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
        $tcp.Dispose()
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

    $httpsys_binding = netsh http show sslcert ipport=0.0.0.0:$portno | where { $_ -match "IP" }

    return $httpsys_binding -ne $null
}

function GetBoundCertificateInfo($portNo) {

    ValidatePort($portNo)

    $s = netsh http show sslcert ipport="0.0.0.0:$portNo"
    $certHashStr = 'Certificate Hash             : '
    $appIdStr = 'Application ID               : '
    $ipPortStr = 'IP:port                      : '

    if($s[5] -eq $null -or $s[5].IndexOf($certHashStr) -eq -1) {
        return $null
    }

    $start = $s[5].IndexOf($certHashStr) + $certHashStr.Length
    $thumbprint =  $s[5].Substring($start, $s[5].Length - $start)
    
    $start = $s[6].IndexOf($appIdStr) + $appIdStr.Length
    $appId =  $s[6].Substring($start, $s[6].Length - $start)
    
    $start = $s[4].IndexOf($ipPortStr) + $ipPortStr.Length
    $ipPort =  $s[4].Substring($start, $s[4].Length - $start)

    return @{
        thumbprint = $thumbprint
        appId = $appId
        ipPort = $ipPort
    }
}

function BindCertificate($_hash, $portNo, $_appId)
{
    ValidatePort($portNo)

    netsh http add sslcert ipport="0.0.0.0:$portNo" certhash="$_hash" appid=$_appId
}

function DeleteHttpsBinding($portNo)
{
    ValidatePort($portNo)

    if(SslBindingExists $portNo) {
        netsh http delete sslcert ipport="0.0.0.0:$portNo" | Out-Null
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

