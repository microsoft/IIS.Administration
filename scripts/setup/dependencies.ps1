# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Is-NanoServer",
                 "IisEnabled",
                 "EnableIis",
                 "UrlAuthEnabled",
                 "EnableUrlAuth",
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

function Is-NanoServer() {
    return $(Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'EditionID').EditionId -eq "ServerDataCenterNano"
}

function Enable-Feature($_featureName) {
    dism.exe /Quiet /NoRestart /Online /Enable-Feature /FeatureName:"$_featureName"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Error enabling $_featureName"
        throw $(new-object "System.ComponentModel.Win32Exception" -ArgumentList $LASTEXITCODE)
    }
}

function Feature-Enabled($_featureName) {
    # Returns string[]
    $info = dism.exe /Online /Get-FeatureInfo /FeatureName:"$_featureName"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Error checking state of $_featureName"
        throw $(new-object "System.ComponentModel.Win32Exception" -ArgumentList $LASTEXITCODE)
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
    if (Is-NanoServer) {
        throw "Unable to enable IIS"
    }
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

function UrlAuthEnabled
{
    return Test-Path $env:windir\system32\inetsrv\urlauthz.dll
}

function EnableUrlAuth {    
    if ($OptionalFeatureCommand -ne $null) {
        $urlAuth = Get-WindowsOptionalFeature -Online -FeatureName "IIS-URLAuthorization" -Verbose:$false -ErrorAction SilentlyContinue

        if ($urlAuth -eq $null) {
            throw "Unable to enable URL Authorization"
        }
        
        Enable-WindowsOptionalFeature -Online -FeatureName "IIS-URLAuthorization" -NoRestart -ErrorAction Stop
    }
    else {
        Enable-Feature "IIS-URLAuthorization"
    }
}

function HostableWebCoreEnabled {
    # Nano Server IIS has hostable web core enabled by default. Any IIS feature that is used must be enabled separately.
    if (Is-NanoServer) {
        return IisEnabled
    }
    return Feature-Enabled "IIS-HostableWebCore"
}

function EnableHostableWebCore { 
    if (Is-NanoServer) {
        return
    }
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
    "Is-NanoServer"
    {
        return Is-NanoServer
    }
    "IisEnabled"
    {
        return IisEnabled
    }
    "EnableIis"
    {
        return EnableIis
    }
    "UrlAuthEnabled"
    {
        return UrlAuthEnabled
    }
    "EnableUrlAuth"
    {
        return EnableUrlAuth
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