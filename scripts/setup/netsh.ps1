# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Add-SslBinding",
                 "Delete-SslBinding",
                 "Get-SslBinding")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $AppId,
    
    [parameter()]
    [System.Net.IPEndPoint]
    $IpEndpoint,
    
    [parameter()]
    [System.Security.Cryptography.X509Certificates.X509Certificate]
    $Certificate
)

$cs = '
namespace Microsoft.IIS.Administration.Setup {
using System;
using System.Runtime.InteropServices;
    public class Http {           
        public const int HTTP_INITIALIZE_CONFIG = 2;  
        public const int HTTP_SERVICE_CONFIG_SSLCERT_INFO = 1; 

        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]  
        public static extern uint HttpDeleteServiceConfiguration(IntPtr ServiceHandle, int ConfigId, ref HTTP_SERVICE_CONFIG_SSL_SET pConfigInformation, int ConfigInformationLength, IntPtr pOverlapped);  
        
        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]  
        public static extern uint HttpInitialize(HTTPAPI_VERSION version, uint flags, IntPtr pReserved);   

        [DllImport("httpapi.dll", EntryPoint = "HttpQueryServiceConfiguration",  
            CharSet = CharSet.Unicode, ExactSpelling = true,  
            CallingConvention = CallingConvention.StdCall)]  
        public static extern uint HttpQueryServiceConfiguration(  
            IntPtr serviceHandle,  
            HTTP_SERVICE_CONFIG_ID configID,  
            ref HTTP_SERVICE_CONFIG_SSL_QUERY pInputConfigInfo,  
            UInt32 InputConfigInfoLength, 
            IntPtr pOutputConfigInfo,
            UInt32 OutputConfigInfoLength,  
            [In, Out] ref UInt32 pReturnLength,  
            IntPtr pOverlapped  
        );  
        
        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]  
        public static extern uint HttpSetServiceConfiguration(IntPtr ServiceHandle, int ConfigId, ref HTTP_SERVICE_CONFIG_SSL_SET pConfigInformation, int ConfigInformationLength, IntPtr pOverlapped);
  
        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]  
        public static extern uint HttpTerminate(uint flags, IntPtr pReserved);

        public static HTTP_SERVICE_CONFIG_SSL_SET MarshalConfigSslSet(IntPtr ptr) {
            return (HTTP_SERVICE_CONFIG_SSL_SET)Marshal.PtrToStructure(ptr, typeof(HTTP_SERVICE_CONFIG_SSL_SET));
        }
    }   
  
    public enum HTTP_SERVICE_CONFIG_ID {  
        HttpServiceConfigIPListenList,  
        HttpServiceConfigSSLCertInfo,  
        HttpServiceConfigUrlAclInfo,  
        HttpServiceConfigMax  
    }  
  
    public enum HTTP_SERVICE_CONFIG_QUERY_TYPE {  
        HttpServiceConfigQueryExact,  
        HttpServiceConfigQueryNext,  
        HttpServiceConfigQueryMax  
    }  
    
    [StructLayout(LayoutKind.Sequential)]  
    public struct HTTPAPI_VERSION {  
        public ushort HttpApiMajorVersion;  
        public ushort HttpApiMinorVersion;  
    }    
  
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]  
    public struct HTTP_SERVICE_CONFIG_SSL_KEY {  
        public IntPtr pIpPort;  
    }  
  
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]  
    public struct HTTP_SERVICE_CONFIG_SSL_QUERY {  
        public HTTP_SERVICE_CONFIG_QUERY_TYPE QueryDesc; 
        public IntPtr KeyDesc;  
        public Int32 dwToken;  
    }  
        
    [StructLayout(LayoutKind.Sequential)]  
    public struct HTTP_SERVICE_CONFIG_SSL_SET {  
        public IntPtr KeyDesc;  
        public uint SslHashLength;  
        public IntPtr pSslHash;  
        public Guid AppId;  
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string pSslCertStoreName;  
        public int DefaultCertCheckMode;  
        public int DefaultRevocationFreshnessTime;  
        public int DefaultRecovationUrlRetrievalTimeout;  
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string pDefaultSslCtlIdentifier;  
        [MarshalAs(UnmanagedType.LPWStr)]  
        public string pDefaultSslCtlStoreName;  
        public int DefaultFlags;  
    }      
}
'

$SUCCESS = 0
function InitializeInterop() {
    try {
        [Microsoft.IIS.Administration.Setup.Http] | Out-Null
    }
    catch {
        Add-Type $cs
    }
}

function GetIpEndpointBytes($_ipEndpoint) {
    $socketAddress = $_ipEndpoint.Serialize()
    $ipBytes = [System.Array]::CreateInstance([System.Byte], $socketAddress.Size)
    for ($i = 0; $i -lt $socketAddress.Size; $i++) {
        $ipBytes[$i] = $socketAddress[$i]
    }
    return $ipBytes
}

function GetBindingInfo($sslConfig) {
    $hash = [System.Array]::CreateInstance([System.Byte], [int]($sslConfig.SslHashLength))
    [System.Runtime.InteropServices.Marshal]::Copy($sslConfig.pSslHash, $hash, 0, $sslConfig.SslHashLength)

    $socketAddressLength = 16
    $sa = [System.Array]::CreateInstance([System.Byte], $socketAddressLength)
    [System.Runtime.InteropServices.Marshal]::Copy($sslConfig.KeyDesc, $sa, 0, $socketAddressLength)
    $socketAddress = New-Object "System.Net.SocketAddress" -ArgumentList ([System.Net.Sockets.AddressFamily]::InterNetwork, $socketAddressLength)
    for ($i = 0; $i -lt $sa.Length; $i++) {
        $socketAddress[$i] = $sa[$i]
    }

    $ep = New-Object "System.Net.IPEndPoint" -ArgumentList ([ipaddress]::Any, 0)
    $endpoint = [System.Net.IPEndPoint]$ep.Create($socketAddress)

    $ret = @{}
    $ret.CertificateHash = [System.BitConverter]::ToString($hash).Replace("-", "")
    $ret.AppId = $sslConfig.AppId
    $ret.IpEndpoint = $endpoint
    return $ret
}

function InitializeHttpSys() {
    $v = New-Object "Microsoft.IIS.Administration.Setup.HTTPAPI_VERSION"
    $V.HttpApiMajorVersion = 1
    $v.HttpApiMinorVersion = 0

    $result = [Microsoft.IIS.Administration.Setup.Http]::HttpInitialize($v, [Microsoft.IIS.Administration.Setup.Http]::HTTP_INITIALIZE_CONFIG, [System.IntPtr]::Zero)

    if ($result -ne $SUCCESS) {
        Write-Warning "Error initializing Http API"
        throw [System.ComponentModel.Win32Exception] $([System.int32]$result)
    }

    return $result
}

function TerminateHttpSys() {
    return [Microsoft.IIS.Administration.Setup.Http]::HttpTerminate([Microsoft.IIS.Administration.Setup.Http]::HTTP_INITIALIZE_CONFIG, [System.IntPtr]::Zero)
}

# Binds an SSL certificate to the specified ip endpoint for use by HTTP.Sys.
# IpEndpoint: The IP endpoint consists of the IP address and port for the certificate to be bound to.
# Certificate: The Certificate to bind to the IP endpoint.
# AppId: The unique ID that identifies the application the binding is being created for.
function Add-SslBinding($_ipEndpoint, $_certificate, $_appId) {
    if ($_ipEndpoint -eq $null) {
        throw "Ip Endpoint required."
    }
    if ($_certificate -eq $null) {
        throw "Certificate required."
    }
    if ($appId -eq $null) {
        throw "App id required."
    }
    if (-not($_appId -is [System.Guid])) {
        $_appId = New-Object "System.Guid" -ArgumentList $_appId
    }

    setSslConfiguration $_ipEndpoint $_certificate $_appId
}

# Deletes an SSL binding that is registered for the specified IP endpoint.
# IpEndpoint: The IP endpoint consists of the IP address and port for the certificate to be deleted from.
function Delete-SslBinding($_ipEndpoint) {
    if ($_ipEndpoint -eq $null) {
        throw "Ip Endpoint required."
    }

    setSslConfiguration $_ipEndpoint $null $([System.Guid]::Empty)
}

# Retrieves an SSL binding that is registered for the specified IP endpoint. Returns NULL if no binding exists.
# IpEndpoint: The IP endpoint consists of the IP address and port for the certificate to be deleted from.
function Get-SslBinding($_ipEndpoint) {
    if ($_ipEndpoint -eq $null) {
        throw "Ip Endpoint required."
    }

    $bufferSize = 4096
    try {
        InitializeHttpSys| Out-Null

        $ipBytes = [System.Byte[]]$(GetIpEndpointBytes($_ipEndpoint))
        $hIp = [System.Runtime.InteropServices.GCHandle]::Alloc($ipBytes, [System.Runtime.InteropServices.GCHandleType]::Pinned)
        $pIp = $hIp.AddrOfPinnedObject()

        $queryParam = New-Object "Microsoft.IIS.Administration.Setup.HTTP_SERVICE_CONFIG_SSL_QUERY"
        $queryParam.QueryDesc = [Microsoft.IIS.Administration.Setup.HTTP_SERVICE_CONFIG_QUERY_TYPE]::HttpServiceConfigQueryExact
        $queryParam.dwToken = 0
        $queryParam.KeyDesc = $pIp

        $returnLen = 0
        $pReturnSet = [System.Runtime.InteropServices.Marshal]::AllocHGlobal($bufferSize)    

        $result = [Microsoft.IIS.Administration.Setup.Http]::HttpQueryServiceConfiguration(
                        [System.IntPtr]::Zero,
                        [Microsoft.IIS.Administration.Setup.HTTP_SERVICE_CONFIG_ID]::HttpServiceConfigSSLCertInfo,
                        [ref] $queryParam,
                        [uint32]([System.Runtime.InteropServices.Marshal]::SizeOf($queryParam)),
                        $pReturnSet,
                        $bufferSize,
                        [ref] $returnLen,
                        [System.IntPtr]::Zero)

        if ($result -eq 2) {
            # File not found
            return $null
        }
        if ($result -ne $SUCCESS) {
            Write-Warning "Error reading Ssl Cert Configuration"
            throw [System.ComponentModel.Win32Exception] $([System.int32]$result)
        }
        $sslConfig = [Microsoft.IIS.Administration.Setup.Http]::MarshalConfigSslSet($pReturnSet)
        return GetBindingInfo $sslConfig
    }
    finally {
        if ($hIp -ne $null) {
            $hIp.Free()
            $hIp = $null
        }
        if ($pReturnSet -ne [System.IntPtr]::Zero) {
            [System.Runtime.InteropServices.Marshal]::FreeHGlobal($pReturnSet)
            $pReturnSet = [System.IntPtr]::Zero
        }
        TerminateHttpSys | Out-Null
    }
}

function setSslConfiguration($_ipEndpoint, $_certificate, $_appId) {

    try {
        InitializeHttpSys| Out-Null
    
        $sslSet = New-Object "Microsoft.IIS.Administration.Setup.HTTP_SERVICE_CONFIG_SSL_SET"
        $sslSetSize = [System.Runtime.InteropServices.Marshal]::SizeOf($sslSet)

        $ipBytes = [System.Byte[]]$(GetIpEndpointBytes($_ipEndpoint))
        $hIp = [System.Runtime.InteropServices.GCHandle]::Alloc($ipBytes, [System.Runtime.InteropServices.GCHandleType]::Pinned)
        $pIp = $hIp.AddrOfPinnedObject()

        $sslSet.KeyDesc = $pIp # IntPtr
        $sslSet.SslHashLength = 0
        $sslSet.pSslHash = [System.IntPtr]::Zero
        $sslSet.pSslCertStoreName = [System.IntPtr]::Zero
        $sslSet.AppId = $_appId

        if ($_certificate -ne $null) {
            # Create binding
            
            $certBytes = $_certificate.GetCertHash()
            $hCertBytes = [System.Runtime.InteropServices.GCHandle]::Alloc($certBytes, [System.Runtime.InteropServices.GCHandleType]::Pinned)
            $pCertBytes = $hCertBytes.AddrOfPinnedObject()
        
            $sslSet.SslHashLength = 20
            $sslSet.pSslHash = $pCertBytes
            $sslSet.pSslCertStoreName = "MY"

            $result = [Microsoft.IIS.Administration.Setup.Http]::HttpSetServiceConfiguration([System.IntPtr]::Zero, 
                      [Microsoft.IIS.Administration.Setup.Http]::HTTP_SERVICE_CONFIG_SSLCERT_INFO,
                      [ref]$sslSet,
                      $sslSetSize,
                      [System.IntPtr]::Zero)  
        }   
        else {
            Write-Verbose "Deleting Ssl Cert Configuration"
            $result = [Microsoft.IIS.Administration.Setup.Http]::HttpDeleteServiceConfiguration([System.IntPtr]::Zero, 
                      [Microsoft.IIS.Administration.Setup.Http]::HTTP_SERVICE_CONFIG_SSLCERT_INFO,
                      [ref]$sslSet,
                      $sslSetSize,
                      [System.IntPtr]::Zero)  
        }  

        if ($result -ne $SUCCESS) {
            Write-Warning "Error setting Ssl Cert Configuration"
            throw [System.ComponentModel.Win32Exception] $([System.int32]$result)
        }
    }
    finally {              
        if ($hIp -ne $null) {
            $hIp.Free()
            $hIp = $null
        }
        if ($hCertBytes -ne $null) {
            $hCertBytes.Free()
            $hCertBytes = $null
        }
        TerminateHttpSys| Out-Null
    }
}

InitializeInterop
switch ($Command)
{
    "Add-SslBinding"
    {
        return Add-SslBinding $IpEndpoint $Certificate $AppId
    }
    "Delete-SslBinding"
    {
        return Delete-SslBinding $IpEndpoint
    }
    "Get-SslBinding"
    {
        return Get-SslBinding $IpEndpoint
    }
    default
    {
        throw "Unknown command"
    }
}

