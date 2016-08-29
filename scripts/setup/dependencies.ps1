# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("IisEnabled",
                 "EnableIis",
                 "WinAuthEnabled",
                 "EnableWinAuth",
                 "HostableWebCoreEnabled",
                 "EnableHostableWebCore")]
    [string]
    $Command
)

# SKU >= Server 2012
$OptionalFeatureCommand = Get-Command "Get-WindowsOptionalFeature" -ErrorAction SilentlyContinue
#SKU == 2008 R2
$AddFeatureCommand = Get-Command "Add-WindowsFeature" -ErrorAction SilentlyContinue

function IisEnabled
{    
    # Server 2012 - 2016 use Get-WindowsOptionalFeature
    # Server 2008 R2 uses Get-WindowsFeature
    # Nano Server doesn't use either, and once IIS is installed, everything is installed

    if ($OptionalFeatureCommand -ne $null) {
        $webserverRole = Get-WindowsOptionalFeature -Online -FeatureName "IIS-WebServerRole" -Verbose:$false -ErrorAction SilentlyContinue
        $webserver = Get-WindowsOptionalFeature -Online -FeatureName "IIS-WebServer" -Verbose:$false -ErrorAction SilentlyContinue

        #
        # Handle Nano Server case where IIS doesn't exist as an optional feature
        if ($webserverRole -eq $null) {
            $iis = Get-Service W3SVC -ErrorAction SilentlyContinue
            if ($iis -eq $NULL) {
                return $false
            }
            return $true
        }

        return $webserverRole.State -eq [Microsoft.Dism.Commands.FeatureState]::Enabled -and $webserver.State -eq [Microsoft.Dism.Commands.FeatureState]::Enabled
    }
    else {
        $webserverRole = Get-WindowsFeature -Name Web-Server -Verbose:$false -ErrorAction Stop
        $webserver = Get-WindowsFeature -Name Web-WebServer -Verbose:$false -ErrorAction Stop

        return $webserverRole.Installed -and $webserver.Installed
    }
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
    else {
        #SKU == 2008 R2
        
        Add-WindowsFeature -Name Web-Server -ErrorAction Stop
        Add-WindowsFeature -Name Web-WebServer -ErrorAction Stop
    }
}

function WinAuthEnabled
{
    if ($OptionalFeatureCommand -ne $null) {
        $winAuth = Get-WindowsOptionalFeature -Online -FeatureName "IIS-WindowsAuthentication" -Verbose:$false -ErrorAction SilentlyContinue

        #
        # Handle Nano Server case where IIS comes with all features
        if ($winAuth -eq $null) {
            $iis = Get-Service W3SVC -ErrorAction SilentlyContinue
            if ($iis -eq $NULL) {
                return $false
            }
            return $true
        }
        
        return $winAuth.State -eq [Microsoft.Dism.Commands.FeatureState]::Enabled
    }
    else {
        $winAuth = Get-WindowsFeature -Name Web-Windows-Auth -Verbose:$false -ErrorAction Stop

        return $winAuth.Installed
    }
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
        Add-WindowsFeature -Name Web-Windows-Auth -ErrorAction Stop
    }
}

function HostableWebCoreEnabled
{
    if ($OptionalFeatureCommand -ne $null) {
        $hwc = Get-WindowsOptionalFeature -Online -FeatureName "IIS-HostableWebCore" -Verbose:$false -ErrorAction SilentlyContinue

        #
        # Handle Nano Server case where IIS comes with all features
        if ($hwc -eq $null) {
            $iis = Get-Service W3SVC -ErrorAction SilentlyContinue
            if ($iis -eq $NULL) {
                return $false
            }
            return $true
        }
        
        return $hwc.State -eq [Microsoft.Dism.Commands.FeatureState]::Enabled
    }
    else {
        $hwc = Get-WindowsFeature -Name Web-WHC -Verbose:$false -ErrorAction Stop

        return $hwc.Installed
    }
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
        Add-WindowsFeature -Name Web-WHC -ErrorAction Stop
    }
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
    default
    {
        throw "Unknown command"
    }
}