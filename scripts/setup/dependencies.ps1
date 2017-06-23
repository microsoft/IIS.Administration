# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Is-NanoServer",
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
    $EditionId = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'EditionID').EditionId

    return ($EditionId -eq "ServerStandardNano") -or
        ($EditionId -eq "ServerDataCenterNano") -or
        ($EditionId -eq "NanoServer") -or
        ($EditionId -eq "ServerTuva")
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