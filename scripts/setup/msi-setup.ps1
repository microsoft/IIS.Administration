# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


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
    $Version,

    [parameter()]
    [boolean]
    $IncludeDefaultCors = $true
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

    $ServiceName = .\globals.ps1 DEFAULT_SERVICE_NAME
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc -ne $null) {
        throw "$ServiceName already exists."
    }
    
    $Port = 55539
    if ($Upgrade) {
        $Port = 0
    }

    .\install.ps1 -Path $adminRoot -Port $Port -DistributablePath $Path -Version $Version -ServiceName $ServiceName -IncludeDefaultCors:$IncludeDefaultCors -DontCopy
}

function Upgrade() {
    $adminRoot = $Path
    $ServiceName = .\globals.ps1 DEFAULT_SERVICE_NAME

    $latest = .\ver.ps1 Get-Latest -Path $Path -ServiceName $ServiceName

    if ($latest -eq $null) {
        throw "Could not find installation to upgrade from"
    }

    $ver = $(Get-Item $latest).Name
    if ($(.\ver.ps1 Compare-Version -Left $Version -Right $ver) -le 0) {
        throw "Cannot upgrade from $ver to $Version."
    }
    
    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue
    if ($svc -ne $null) {
        $ServiceName = $ServiceName + " $Version"
    }
    
    $installed = $false
    try {
        .\install.ps1 -Path $adminRoot -Port 0 -DistributablePath $Path -Version $Version -ServiceName $ServiceName -IncludeDefaultCors:$IncludeDefaultCors -DontCopy
        $installed = $true
        .\migrate.ps1 -Source $latest -Destination $(Join-Path $adminRoot $Version)
        try {            
            .\uninstall.ps1 -Path $latest -KeepFiles -KeepGroups
        }
        catch {
            # Uninstall must not throw
            Write-Warning -Message $($_.Exception.Message + [Environment]::NewLine + $_.InvocationInfo.PositionMessage)
        }
    }
    catch {
        if ($installed) {
            try {
                .\uninstall.ps1 -Path $(Join-Path $adminRoot $version) -KeepFiles
            }
            catch {
                # Uninstall must not throw
                Write-Warning -Message $($_.Exception.Message + [Environment]::NewLine + $_.InvocationInfo.PositionMessage)
            }
        }
        throw $_
    }
}

function Uninstall() {
    $adminRoot = Join-Path $Path $Version
    .\uninstall.ps1 -Path $adminRoot -KeepFiles -DeleteCert -DeleteBinding
}


#
# Set code page 437 in order to fix a globalization issue of Wix which is that all the powershell console output is broken in localized OS.
try {
    chcp 437
}
catch {
    Write-Warning -Message "Fails to execute chcp 437"
}

#
# To fix one of the reasons of the initial service start failure, we need to disable .Net Telemetry
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1

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
    Set-Variable -Name $(.\globals.ps1 INSTALL_METHOD_KEY) -Value "MSI" -Scope Global
    
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
            try {
                #Uninstall must not throw
                Uninstall
            }
            catch {
                Write-Warning -Message $($_.Exception.Message + [Environment]::NewLine + $_.InvocationInfo.PositionMessage)
            }
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