# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [string]
    $Source
)

function Get-LogsPath($IisAdminPath) {
    $appSettingsPath = [System.IO.Path]::Combine($IisAdminPath, "Microsoft.IIS.Administration\config\appsettings.json")

    Write-Verbose "Obtaining logs path for installation at $IisAdministrationPath"

    # Get logs path entry from appsettings.json
    $logsPath = $null
    if ($(Test-Path $appSettingsPath)) {
        $settings = .\json.ps1 Get-JsonContent -Path $appSettingsPath

        if ($settings -ne $null -and $settings.auditing -ne $null -and $settings.auditing.path -ne $null) {
            $logsPath = [System.Environment]::ExpandEnvironmentVariables($settings.auditing.path)
        }
    }

    Write-Verbose "appsettings log path: $logsPath"

    # Resolve possible relative path from appsettings
    $resolved = $null
    if ($logsPath -ne $null) {
        if ([System.IO.Path]::IsPathRooted($logsPath)) {
            $resolved = $logsPath
        }
        else {
            $appPath = [System.IO.Path]::Combine($IisAdminPath, "Microsoft.IIS.Administration")
            $resolved = [System.IO.Path]::Combine($appPath, $logsPath)
        }
    }

    # If log path not obtained from appsettings, use default location
    if ($resolved -eq $null) {
        $root = [System.IO.Path]::GetDirectoryName($IisAdminPath)
        $resolved = [System.IO.Path]::Combine($root, "logs")
    }

    Write-Verbose "Resolved log path: $resolved"

    return $resolved
}

function Get-LogsExtension($IisAdminPath) {
    $appSettingsPath = [System.IO.Path]::Combine($IisAdminPath, "Microsoft.IIS.Administration\config\appsettings.json")

    $logsExtension = $null
    if ($(Test-Path $appSettingsPath)) {
        $settings = .\json.ps1 Get-JsonContent -Path $appSettingsPath

        if ($settings -ne $null -and $settings.auditing -ne $null -and $settings.auditing.name -ne $null) {
            $logsExtension = [System.IO.Path]::GetExtension($settings.auditing.name)
        }
    }

    if ($logsExtension -eq $null) {
        $logsExtension = ".txt"
    }

    return $logsExtension
}

# Removes unsafe lines from a file
function Clear-CcsAuditPasswordsFromFile($filePath) {

    Write-Verbose "Sanitizing $filePath"

    $lines = [System.IO.File]::ReadAllLines($filePath)

    $lines = $lines | where {
        $l = $_.ToLowerInvariant();
        -not($l.Contains("`"password`"")) -and -not($l.Contains("`"private_key_password`""))
    }

    [System.IO.File]::WriteAllLines($filePath, [string[]]$lines)
}

function Clear-CcsAuditPasswords($IisAdministrationPath) {
    # Get the logs path the installation is configured to use
    $logsPath = Get-LogsPath $IisAdministrationPath
    $logsExtension = Get-LogsExtension $IisAdministrationPath

    if (-not(Test-Path $logsPath)) {
        return
    }

    $logsDir = Get-Item $logsPath
    if (-not($logsDir -is [System.IO.DirectoryInfo])) {
        return
    }

    $acl = Get-Acl $logsDir.FullName
    .\security.ps1 Add-SelfRights -Path $logsPath -Recurse

    try {
        # Get all files in the logs directory
        $logFiles = Get-ChildItem -Path $logsPath -Filter "*$logsExtension" | Where {$_ -is [System.IO.FileInfo]}

        # Null if empty dir
        if ($logFiles -eq $null) {
            return
        }

        foreach ($file in $logFiles) {
            try {
                Clear-CcsAuditPasswordsFromFile -filePath $file.FullName
            }
            catch {
                Write-Warning "Error clearing ccs audit password from $($file.FullName): $($_.Exception.Message)"
                #If one file fails, do not block the remaining files
            }
        }
    }
    finally {
        .\security.ps1 Set-AclForced -Acl $acl -Path $logsPath
    }
}


if ($Source.Contains("1.1.0")) {
    Clear-CcsAuditPasswords -IisAdministrationPath $Source
}
