# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("IisEnabled",
                 "EnableIis",
                 "WinAuthEnabled",
                 "EnableWinAuth",
                 "HostableWebCoreEnabled",
                 "EnableHostableWebCore",
                 "NetFx3Enabled",
                 "EnableNetFx3")]
    [string]
    $Command
)

# SKU >= Server 2012
$OptionalFeatureCommand = Get-Command "Get-WindowsOptionalFeature" -ErrorAction SilentlyContinue
#SKU == 2008 R2
$AddFeatureCommand = Get-Command "Add-WindowsFeature" -ErrorAction SilentlyContinue

function Enable-Feature($_featureName) {
    dism.exe /Online /Enable-Feature /FeatureName:"$_featureName"
    if ($LASTEXITCODE -ne 0) {
        throw $(new-object "System.ComponentModel.Win32Exception" -ArgumentList $LASTEXITCODE "Error enabling $_featureName")
    }
}

function Feature-Enabled($_featureName) {
    # Returns string[]
    $info = dism.exe /Online /Get-FeatureInfo /FeatureName:"$_featureName"
    if ($LASTEXITCODE -ne 0) {
        throw $(new-object "System.ComponentModel.Win32Exception" -ArgumentList $LASTEXITCODE "Error checking state of $_featureName")
    }
    foreach ($line in $info) {
        if ($line.StartsWith("State :")) {
            return $line.Contains("Enabled")
        }
    }
    throw "Could not get the state of $_featureName"
}

function IisEnabled
{   
    $iis = Get-Service W3SVC -ErrorAction SilentlyContinue
    if ($iis -eq $NULL) {
        return $false
    }
    return $true
}

function EnableIis {    
    if ($OptionalFeatureCommand -ne $null) {
        # SKU > Server 2012

        $webserverRole = Get-WindowsOptionalFeature -Online -FeatureName "IIS-WebServerRole" -Verbose:$false

        #
        # Handle Nano Server case where IIS doesn't exist as an optional feature
        if ($webserverRole -eq $null) {
            throw "Unable to enable IIS"
        }
        
        Enable-WindowsOptionalFeature -Online -FeatureName "IIS-WebServerRole" -NoRestart -ErrorAction Stop
        Enable-WindowsOptionalFeature -Online -FeatureName "IIS-WebServer" -NoRestart -ErrorAction Stop
    }
    elseif ($AddFeatureCommand -ne $null) {
        #SKU == 2008 R2 with PS Upgrade
        
        Add-WindowsFeature -Name Web-Server -ErrorAction Stop
        Add-WindowsFeature -Name Web-WebServer -ErrorAction Stop
    }
    else {
        dism.exe /Online /Enable-Feature /FeatureName:"IIS-WebServerRole" /FeatureName:"IIS-WebServer" /FeatureName:"WAS-ConfigurationAPI" /FeatureName:"WAS-NetFxEnvironment" /FeatureName:"WAS-ProcessModel" /FeatureName:"WAS-WindowsActivationService"
        if ($LASTEXITCODE -ne 0) {
            throw $(new-object "System.ComponentModel.Win32Exception" -ArgumentList $LASTEXITCODE "Error enabling IIS")
        }
    }
}

function WinAuthEnabled
{
    return Test-Path $env:windir\system32\inetsrv\authsspi.dll
}

function EnableWinAuth {    
    if ($OptionalFeatureCommand -ne $null) {
        $winAuth = Get-WindowsOptionalFeature -Online -FeatureName "IIS-WindowsAuthentication" -Verbose:$false -ErrorAction SilentlyContinue

        if ($winAuth -eq $null) {
            throw "Unable to enable Windows Authentication"
        }
        
        Enable-WindowsOptionalFeature -Online -FeatureName "IIS-WindowsAuthentication" -NoRestart -ErrorAction Stop
    }
    else {
        Enable-Feature "IIS-WindowsAuthentication"
    }
}

function HostableWebCoreEnabled
{
    return Feature-Enabled "IIS-HostableWebCore"
}

function EnableHostableWebCore {  
    if ($OptionalFeatureCommand -ne $null) {
        $hwc = Get-WindowsOptionalFeature -Online -FeatureName "IIS-HostableWebCore" -Verbose:$false -ErrorAction SilentlyContinue

        if ($hwc -eq $null) {
            throw "Unable to enable IIS Hostable Web Core"
        }
        
        Enable-WindowsOptionalFeature -Online -FeatureName "IIS-HostableWebCore" -NoRestart -ErrorAction Stop
    }
    else {
        Enable-Feature "IIS-HostableWebCore"
    }
}

function NetFx3Enabled {
    return Feature-Enabled "NetFx3"
}

function EnableNetFx3 {
    Enable-Feature "NetFx3"
}

switch ($Command)
{
    "IisEnabled"
    {
        return IisEnabled
    }
    "EnableIis"
    {
        return EnableIis
    }
    "WinAuthEnabled"
    {
        return WinAuthEnabled
    }
    "EnableWinAuth"
    {
        return EnableWinAuth
    }
    "HostableWebCoreEnabled"
    {
        return HostableWebCoreEnabled
    }
    "EnableHostableWebCore"
    {
        return EnableHostableWebCore
    }
    "NetFx3Enabled"
    {
        return NetFx3Enabled
    }
    "EnableNetFx3"
    {
        return EnableNetFx3
    }
    default
    {
        throw "Unknown command"
    }
}