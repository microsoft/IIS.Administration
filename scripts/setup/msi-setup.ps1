# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


#Requires -RunAsAdministrator
#Requires -Version 4.0
Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Install",
                 "Uninstall",
                 "Upgrade")]
    [string]
    $Command,

    [parameter()]
    [string]
    $Path,

    [parameter()]
    [string]
    $Version
)

function Get-ScriptDirectory {
    Split-Path $script:MyInvocation.MyCommand.Path
}

function Require-Script($name) {
    $p = Join-Path $(Get-ScriptDirectory) $("$name.ps1")
    if (-not(Test-Path $p)) {
        throw "Could not find required script $name"
    }
}

function CheckParameters() {
    if ([string]::IsNullOrEmpty($Path)) {
        throw "Path required."
    }
    if ([string]::IsNullOrEmpty($Version)) {
        throw "Version required."
    }
    
    $Script:Path = $Path.Trim(' ')
    $Script:Path = $Path.Trim('"')
}

function Install() {
    $adminRoot = $Path

    $ServiceName = .\constants.ps1 DEFAULT_SERVICE_NAME
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc -ne $null) {
        throw "$ServiceName already exists."
    }
    
    $Port = 55539
    if ($Upgrade) {
        $Port = 0
    }

    .\install.ps1 -Path $adminRoot -Port $Port -DistributablePath $Path -Version $Version -ServiceName $ServiceName -DontCopy
}

function Upgrade() {
    $adminRoot = $Path
    $ServiceName = .\constants.ps1 DEFAULT_SERVICE_NAME

    $latest = .\versioning.ps1 Get-Latest -Path $Path -ServiceName $ServiceName

    if ($latest -eq $null) {
        throw "Could not find installation to upgrade from"
    }

    $ver = $(Get-Item $latest).Name
    if ($(.\versioning.ps1 Compare-Version -Left $Version -Right $ver) -le 0) {
        throw "Cannot upgrade from $ver to $Version."
    }
    
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc -ne $null) {
        $ServiceName = $ServiceName + " $Version"
    }
    
    $installed = $false
    try {
        .\install.ps1 -Path $adminRoot -Port 0 -DistributablePath $Path -Version $Version -ServiceName $ServiceName -DontCopy
        $installed = $true
        .\migrate.ps1 -Source $latest -Destination $(Join-Path $adminRoot $Version)
    }
    catch {
        if ($installed) {
            .\uninstall.ps1  -Path $adminRoot -Version $Version -KeepFiles
        }
        throw $_
    }
}

function Uninstall() {
    $adminRoot = Join-Path $Path $Version

    .\uninstall.ps1 -Path $adminRoot -KeepFiles
}

Require-Script "acl"
Require-Script "activedirectory"
Require-Script "cache"
Require-Script "cert"
Require-Script "constants"
Require-Script "dependencies"
Require-Script "installationconfig"
Require-Script "migrate"
Require-Script "modules"
Require-Script "network"
Require-Script "services"
Require-Script "uninstall"

$exitCode = 0
try {
    Push-Location $(Get-ScriptDirectory)
    
    switch($Command)
    {
        "Install"
        {
            CheckParameters
            Install
        }
        "Upgrade"
        {
            CheckParameters
            Upgrade
        }
        "Uninstall"
        {
            CheckParameters
            Uninstall
        }
        default
        {
            throw "Unknown command"
        }
    }
}
catch {
    throw
}
finally {
    Pop-Location
}