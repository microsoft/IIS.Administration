# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Get",
                 "Remove",
                 "Exists",
                 "Get-UserFileMap",
                 "Write-Config",
                 "Write-AppHost",
                 "Get-AppHostPort")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [object]
    $ConfigObject,
    
    [parameter()]
    [string]
    $AppHostPath,
    
    [parameter()]
    [string]
    $AppSettingsPath,
    
    [parameter()]
    [string]
    $ApplicationPath,
    
    [parameter()]
    [string]
    $Version,
    
    [parameter()]
    [int]
    $Port
)

# Name of file we place installation data in
$INSTALL_FILE = "setup.config"
$IISAdminSiteName = "IISAdmin"

# Returns a map of the files that contain user configurable settings.
function Get-UserFileMap {
    return @{
        "applicationHost.config" = "host/applicationHost.config"
        "web.config" = "Microsoft.IIS.administration/web.config"
        "modules.json" = "Microsoft.IIS.administration/config/modules.json"
        "config.json" = "Microsoft.IIS.administration/config/appsettings.json"
        "api-keys.json" = "Microsoft.IIS.administration/config/api-keys.json"
    }
}

# Tests whether a setup configuration exists at the given path.
# Path: The path to test for existence of a setup configuration.
function Exists($_path) {

    if ([string]::IsNullOrEmpty($_path)) {
        throw "Path required."
    }

    return Test-Path (Join-Path $_path $INSTALL_FILE)
}

# Returns an object representation of the setup configuration located at the specified path.
# Path: The parent directory of the setup configuration.
function Get($_path) {

    if ([string]::IsNullOrEmpty($_path)) {
        throw "Path required."
    }
    
	if (-not(Exists $_path)) {
		return $null
	}

    [xml]$configXml = Get-Content (Join-Path $_path $INSTALL_FILE)
    $config = $configXml.Configuration

    $userFiles = Get-UserFileMap

    $installConfig = @{
        InstallPath = $config.InstallPath
        Port = $config.Port
        ServiceName = $config.ServiceName
        Version = $config.Version
        UserFiles = $userFiles
        Installer = $config.Installer
        Date = $config.Date
		CertificateThumbprint = $config.CertificateThumbprint
    }

    return $installConfig
}

# Removes the setup configuration located at the specified path.
# Path: The parent directory of the setup configuration.
function Remove($_path) {
    if ($(Test-Path $_path)) {
        Remove-Item -Force $(Join-Path $_path $INSTALL_FILE)
    }
}

# Writes the provided object into a setup configuration at the specified path.
# ConfigObject: The object to write into the setup configuration.
# Path: The path to a directory to hold the setup configuration.
function Write-Config($obj, $_path) {

    if ([string]::IsNullOrEmpty($_path)) {
        throw "Path required."
    }

    $xml = [xml]"<?xml version=`"1.0`" encoding=`"utf-8`"?><Configuration></Configuration>"
    $xConfig = $xml["Configuration"]

    foreach ($key in $obj.keys) {     
           
        $xElem = $xml.CreateElement($key)
        $xElem.InnerText = $obj[$key]
        $xConfig.AppendChild($xElem) | Out-Null
    }     
    $xml.AppendChild($xConfig) | Out-Null

    $sw = New-Object System.IO.StreamWriter -ArgumentList (Join-Path $_path $INSTALL_FILE)
    $xml.Save($sw) | Out-Null
    $sw.Dispose()
}

# Sets the applicationHost.config file to host the Microsoft.IIS.Administration application given the specified settings.
# AppHostPath: The location of the applicationHost.config file.
# ApplicationPath: The location of the application to register in the applicationHost.config file.
# Port: The port to listen on.
# Version: The version of the application.
function Write-AppHost($_appHostPath, $_applicationPath, $_port, $_version) {

    if ([string]::IsNullOrEmpty($_appHostPath)) {
        throw "AppHostPath required."
    }
    if ([string]::IsNullOrEmpty($_applicationPath)) {
        throw "ApplicationPath required."
    }
    if ($_port -eq 0) {
        throw "Port required."
    }

    $IISAdminPoolName = "IISAdminAppPool" + $_version

    [xml]$xml = Get-Content -Path "$_appHostPath"
    $xml.configuration."system.applicationHost".applicationPools.Add.name = $IISAdminPoolName
    $sites = $xml.GetElementsByTagName("site")

    $site = $null;
    foreach ($s in $sites) {
    if ($s.name -eq $IISAdminSiteName) { 
            $site = $s;
        } 
    }

    if ($site -eq $null) {
        throw "Installation applicationHost.config does not contain IISAdmin site"
    }

    $site.application.SetAttribute("applicationPool", "$IISAdminPoolName")
    $site.application.virtualDirectory.SetAttribute("physicalPath", "$_applicationPath")
    $site.bindings.binding.SetAttribute("bindingInformation", "*:$($_port):")
    $sw = New-Object System.IO.StreamWriter -ArgumentList $_appHostPath
    $xml.Save($sw)
    $sw.Dispose()
}

# Gets the port that the IIS Administration site is configured to listen on in the applicationHost.config file.
# AppHostPath: The location of the applicationHost.config file.
function Get-AppHostPort($_appHostPath) {
    if ([string]::IsNullOrEmpty($_appHostPath)) {
        throw "AppHostPath required."
    }
    if (-not(Test-Path $_appHostPath)) {
        throw "applicationHost.config not found at $_appHostPath"
    }

    
    [xml]$xml = Get-Content -Path "$_appHostPath"
    $sites = $xml.GetElementsByTagName("site")

    $site = $null;
    foreach ($s in $sites) {
    if ($s.name -eq $IISAdminSiteName) { 
            $site = $s;
        } 
    }

    if ($site -eq $null) {
        throw "Installation applicationHost.config does not contain IISAdmin site"
    }

    $info = $site.bindings.binding.GetAttribute("bindingInformation")

    $parts = $info.Split(':')
    $test = $null
    if ($parts.Length -ne 3) {
        throw "Unsupported binding information format"
    }
    if (-not([uint16]::TryParse($parts[1], [ref]$test))) {
        throw "Unsupported binding information format"
    }
    return $parts[1]
}

switch ($Command)
{
    "Get"
    {
        return Get $Path
    }
    "Remove"
    {
        Remove $Path
    }
    "Exists"
    {
        return Exists $Path
    }
    "Get-UserFileMap"
    {
        return Get-UserFileMap
    }
    "Write-Config"
    {
        Write-Config $ConfigObject $Path
    }
    "Write-AppHost"
    {
        Write-AppHost $AppHostPath $ApplicationPath $Port $Version
    }
    "Get-AppHostPort"
    {
        return Get-AppHostPort $AppHostPath
    }
    default
    {
        throw "Unknown command"
    }
}