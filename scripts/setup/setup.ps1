# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


#Requires -RunAsAdministrator
#Requires -Version 4.0
Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Install",
                 "Uninstall")]
    [string]
    $Command,

    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [int]
    $Port,
    
    [parameter()]
    [string]
    $DistributablePath,
    
    [parameter()]
    [string]
    $CertHash,
    
    [parameter()]
    [switch]
    $SkipVerification,

    [parameter()]
    [switch]
    $SkipIisAdministrators,
    
    [parameter()]
    [switch]
    $DeleteCert,
    
    [parameter()]
    [switch]
    $DeleteBinding,
    
    [parameter()]
    [switch]
    $DeleteGroup
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

function CheckInstallParameters() {
    if ([string]::IsNullOrEmpty($Path)) {
        $Script:path = .\constants.ps1 DEFAULT_INSTALL_PATH
    }
    if ([String]::IsNullOrEmpty($DistributablePath)){
        $script:DistributablePath = $(Resolve-Path $(Join-Path $(Get-ScriptDirectory) ..)).Path
    }
    $distributableDirectory = Get-Item $DistributablePath -ErrorAction SilentlyContinue
    if ($distributableDirectory -eq $null -or !($distributableDirectory -is [System.IO.DirectoryInfo])){
        Write-Verbose "Invalid DistributablePath directory: $DistributablePath"
    }    
    $versionInfoPath = Join-Path $DistributablePath "Version.txt"
    if($(Get-Item $versionInfoPath -ErrorAction SilentlyContinue) -eq $null) {
        throw "Cannot find version information."
    }
    try {
        $Script:Version = Get-Content $versionInfoPath -ErrorAction Stop
    }
    catch {
        Write-Warning "Could not obtain version information."
        throw $_
    }
}

function CheckUninstallParameters() {
    if ([string]::IsNullOrEmpty($Path)) {
        $Script:path = .\constants.ps1 DEFAULT_INSTALL_PATH
    }
}

function Install() {
    $adminRoot = $Path

       
    $latest = .\versioning.ps1 Get-Latest -Path $adminRoot

    if ($latest -ne $null) {
        Upgrade
    }
    else {
        $ServiceName = .\constants.ps1 DEFAULT_SERVICE_NAME
        .\install.ps1 -Path $adminRoot -Port $Port -SkipVerification:$SkipVerification -SkipIisAdministrators:$SkipIisAdministrators -DistributablePath $DistributablePath -CertHash $CertHash -Version $Version -ServiceName $ServiceName
    }
}

function Upgrade() {
    $adminRoot = $Path    
    $latest = .\versioning.ps1 Get-Latest -Path $adminRoot

    if ($latest -eq $null) {
        throw "Cannot find previous installation."
    }

    $item = Get-Item $latest
    $latestVersion = $item.Name

    if ($(.\versioning.ps1 Compare-Version -Left $Version -Right $latestVersion) -le 0) {
        Write-Host "Application already installed"
        return
    }

    $ServiceName = .\constants.ps1 DEFAULT_SERVICE_NAME
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc -ne $null) {
        $ServiceName = $ServiceName + " $Version"
    }

    $installed = $false
    try {
        .\install.ps1 -Path $adminRoot -Version $Version -SkipVerification:$SkipVerification -SkipIisAdministrators:$SkipIisAdministrators -ServiceName $ServiceName -Port 0 -DistributablePath $DistributablePath -CertHash $CertHash
        $installed = $true
        .\migrate.ps1 -Source $latest -Destination $(Join-Path $adminRoot $Version)
    }
    catch {
        if ($installed) {      
            .\uninstall.ps1 -Path $(Join-Path $adminRoot $Version)
        }

        throw $_
    }

    .\uninstall.ps1 -Path $latest
}

function Uninstall() {

    $adminRoot = $Path

    $children = Get-ChildItem -Directory $adminRoot

    $validAdminRoot = $false
    foreach ($child in $children) {
        if (.\installationconfig.ps1 Exists -Path $child.FullName) {
		    $validAdminRoot = $true
            break
        }
    }

    if (-not($validAdminRoot)) {
        throw "Cannot find setup.config file for uninstall. Cannot continue"
    }

    
    foreach ($child in $children) {
        if (-not(.\installationconfig.ps1 Exists -Path $child.FullName)) {
            Remove-Item -Recurse -Force $child.FullName -ErrorAction SilentlyContinue
        }
    }
    
    $children = Get-ChildItem -Directory $adminRoot

    foreach ($child in $children) {
        .\uninstall.ps1 -Path $child.FullName -DeleteCert:$DeleteCert -DeleteBinding:$DeleteBinding -DeleteGroup:$DeleteGroup
    }    
    
    $dir = Get-Item $adminRoot -ErrorAction SilentlyContinue
    if ($dir -ne $null) {
        Try
        {
            $files = Get-ChildItem $dir.FullName

            Remove-Item $dir.FullName -Recurse -Force -ErrorAction Stop
            Write-Host "Successfully removed installation folder."
        }
        Catch
        {
            Write-Warning $_.Exception.Message
        }
    }
}

Require-Script "acl"
Require-Script "activedirectory"
Require-Script "cache"
Require-Script "cert"
Require-Script "selfsignedcertificate"
Require-Script "constants"
Require-Script "dependencies"
Require-Script "httpsys"
Require-Script "installationconfig"
Require-Script "migrate"
Require-Script "modules"
Require-Script "network"
Require-Script "services"
Require-Script "uninstall"

try {
    Push-Location $(Get-ScriptDirectory)
    
    switch($Command)
    {
        "Install"
        {
            CheckInstallParameters
            Install
        }
        "Uninstall"
        {
            CheckUninstallParameters
            Uninstall
        }
        default
        {
            throw "Unknown command"
        }
    }
}
finally {
    Pop-Location
}