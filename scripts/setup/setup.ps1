# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


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
    $DeleteCert,
    
    [parameter()]
    [switch]
    $DeleteBinding,

    [parameter()]
    [boolean]
    $IncludeDefaultCors = $true
)

Set-StrictMode -Off

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
        $Script:path = .\globals.ps1 DEFAULT_INSTALL_PATH
    }
    if ([String]::IsNullOrEmpty($DistributablePath)){
        $script:DistributablePath = $(Resolve-Path $(Join-Path $(Get-ScriptDirectory) ..)).Path
    }
    $distributableDirectory = Get-Item $DistributablePath -ErrorAction SilentlyContinue
    if ($distributableDirectory -eq $null -or !($distributableDirectory -is [System.IO.DirectoryInfo])){
        Write-Verbose "Invalid DistributablePath directory: $DistributablePath"
    }    
    $versionInfoPath = Join-Path $DistributablePath "setup\version.json"
    if($(Get-Item $versionInfoPath -ErrorAction SilentlyContinue) -eq $null) {
        throw "Cannot find version information."
    }
    try {
        $Script:Version = $(.\json.ps1 Get-JsonContent -Path $versionInfoPath).version
        if ([string]::IsNullOrEmpty($Script:Version)) {
            throw "Could not obtain version information."
        }
    }
    catch {
        Write-Warning "Could not obtain version information."
        throw $_
    }
}

function CheckUninstallParameters() {
    if ([string]::IsNullOrEmpty($Path)) {
        $Script:path = .\globals.ps1 DEFAULT_INSTALL_PATH
    }
}

function Install() {
    $adminRoot = $Path
    $latest = .\ver.ps1 Get-Latest -Path $adminRoot
    if ($latest -ne $null) {
        Upgrade
    }
    else {
        $ServiceName = .\globals.ps1 DEFAULT_SERVICE_NAME
        .\install.ps1 -Path $adminRoot -Port $Port -SkipVerification:$SkipVerification -DistributablePath $DistributablePath -CertHash $CertHash -Version $Version -ServiceName $ServiceName -IncludeDefaultCors:$IncludeDefaultCors
    }
}

function Upgrade() {
    $adminRoot = $Path    
    $latest = .\ver.ps1 Get-Latest -Path $adminRoot

    if ($latest -eq $null) {
        throw "Cannot find previous installation."
    }

    $item = Get-Item $latest
    $latestVersion = $item.Name

    if ($(.\ver.ps1 Compare-Version -Left $Version -Right $latestVersion) -le 0) {
        Write-Host "Application already installed"
        return
    }

    $ServiceName = .\globals.ps1 DEFAULT_SERVICE_NAME
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc -ne $null) {
        $ServiceName = $ServiceName + " $Version"
    }

    $installed = $false
    try {
        .\install.ps1 -Path $adminRoot -Version $Version -SkipVerification:$SkipVerification -ServiceName $ServiceName -Port 0 -DistributablePath $DistributablePath -CertHash $CertHash -IncludeDefaultCors:$IncludeDefaultCors
        $installed = $true
        .\migrate.ps1 -Source $latest -Destination $(Join-Path $adminRoot $Version)
    }
    catch {
        if ($installed) {      
            .\uninstall.ps1 -Path $(Join-Path $adminRoot $Version)
        }

        throw $_
    }

    .\uninstall.ps1 -Path $latest -KeepGroups
}

function Uninstall() {

    $adminRoot = $Path

    $expectedDirectories = @('logs')

    if (-not(Test-Path $adminRoot)) {
        Write-Host Microsoft IIS Administration not found
        return
    }

    $children = Get-ChildItem $adminRoot | Where-Object { $_ -is [System.IO.DirectoryInfo] }

    $validAdminRoot = $false
    foreach ($child in $children) {
        if (.\config.ps1 Exists -Path $child.FullName) {
		    $validAdminRoot = $true
            break
        }
    }

    if (-not($validAdminRoot)) {
        throw "Cannot find setup.config file for uninstall. Cannot continue"
    }

    # Previous installations if any
    foreach ($child in $children) {
        if ($(.\config.ps1 Exists -Path $child.FullName) -and -not($(Get-ScriptDirectory).StartsWith($child.FullName)) ) {
            .\uninstall.ps1 -Path $child.FullName -DeleteCert:$DeleteCert -DeleteBinding:$DeleteBinding
        }
    }
    
    $children = Get-ChildItem $adminRoot | Where-Object { $_ -is [System.IO.DirectoryInfo] }

    # Other known directories
    foreach ($child in $children) {
        # Use nested for loop to support missing System.Object[].Contains
        foreach ($dir in $expectedDirectories) {
            if ($dir -eq $child.Name) {
                .\files.ps1 Remove-ItemForced -Path $child.FullName -ErrorAction SilentlyContinue
            }
        }
    }
    
    # Can be null if uninstalled from outside installation directory
    $children = Get-ChildItem $adminRoot | Where-Object { $_ -is [System.IO.DirectoryInfo] }

    if ($children -ne $null) {
        # Current installation
        foreach ($child in $children) {
            if (.\config.ps1 Exists -Path $child.FullName) {
                .\uninstall.ps1 -Path $child.FullName -DeleteCert:$DeleteCert -DeleteBinding:$DeleteBinding
                break
            }
        }
    }
    
    $dir = Get-Item $adminRoot -ErrorAction SilentlyContinue
    if ($dir -ne $null) {
        Try
        {
            $files = Get-ChildItem $dir.FullName
            
            if ($files -eq $null -or $files.Length -eq 0) {
                Remove-Item $dir.FullName -Recurse -Force -ErrorAction Stop
                Write-Host "Successfully removed installation folder."
            }
            else {
                Write-Warning "Cannot remove the installation folder $dir because it is not empty"
            }
        }
        Catch
        {
            Write-Warning $_.Exception.Message
        }
    }
}

Require-Script "acl-util"
Require-Script "cache"
Require-Script "cert"
Require-Script "config"
Require-Script "dependencies"
Require-Script "files"
Require-Script "globals"
Require-Script "json"
Require-Script "migrate"
Require-Script "modules"
Require-Script "net"
Require-Script "netsh"
Require-Script "require"
Require-Script "sanitize-logs"
Require-Script "security"
Require-Script "services"
Require-Script "uninstall"
Require-Script "ver"

try {
    Push-Location $(Get-ScriptDirectory)
    .\require.ps1 Is-Administrator
    Set-Variable -Name $(.\globals.ps1 INSTALL_METHOD_KEY) -Value "Script" -Scope Global
    
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
catch {
    Write-Error -Exception $_.exception -Message $($_.Exception.Message + [Environment]::NewLine + $_.InvocationInfo.PositionMessage)
    exit -1
}
finally {
    Clear-Variable -Name $(.\globals.ps1 INSTALL_METHOD_KEY) -Scope Global
    Pop-Location
}
exit 0