# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [string]
    $IisAdministrationPath
)

# Acquire the log path the installation is using
# Get all log/audit files
# Sanitize the log/audit files

# 131377878541327692 ---> Thursday, April 27, 2017 5:30:54 PM  (UTC)
$Release = [System.DateTime]::FromFileTimeUtc(131377878541327692)

function Get-LogsPath($IisAdminPath) {
    # Only 1.1.0 affected
    $version = "1.1.0"
    $appSettingsPath = [System.IO.Path]::Combine($IisAdminPath, "$version\Microsoft.IIS.Administration\config\appsettings.json")

    Write-Verbose "Obtaining logs path for installation at $IisAdministrationPath"

    # Get logs path entry from appsettings.json
    $logsPath = $null
    if ($(Test-Path $appSettingsPath)) {
        $settings = .\modules.ps1 Get-JsonContent -Path $appSettingsPath

        if ($settings -ne $null -and $settings.logging -ne $null -and $settings.logging.path -ne $null) {
            $logsPath = [System.Environment]::ExpandEnvironmentVariables($settings.logging.path)
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
            $appPath = [System.IO.Path]::Combine($IisAdminPath, "$version\Microsoft.IIS.Administration")
            $resolved = [System.IO.Path]::Combine($appPath, $logsPath)
        }
    }

    # If log path not obtained from appsettings, use default location
    if ($resolved -eq $null) {
        $resolved = [System.IO.Path]::Combine($IisAdminPath, "logs")
    }

    Write-Verbose "Resolved log path: $resolved"

    return $resolved
}

# Removes unsafe lines from a file
function Sanitize-Audit($filePath) {

    Write-Verbose "Sanitizing $filePath"

    $lines = [System.IO.File]::ReadAllLines($filePath)

    $lines = $lines | where {
        $l = $_.ToLowerInvariant();
        -not($l.Contains("`"password`"")) -and -not($l.Contains("`"private_key_password`""))
    }

    [System.IO.File]::WriteAllLines($filePath, [string[]]$lines)
}



# Get the logs path the installation is configured to use
$logsPath = Get-LogsPath $IisAdministrationPath

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
    $logFiles = Get-ChildItem $logsPath | Where {$_ -is [System.IO.FileInfo]}

    # Null if empty dir
    if ($logFiles -eq $null) {
        return
    }

    # Filter unaffected files
    $targetFiles = $logFiles | where { $_.LastWriteTimeUtc -ge $Release }

    if ($targetFiles -eq $null) {
        return
    }

    foreach ($file in $targetFiles) {
        Sanitize-Audit -filePath $file.FullName
    }
}
finally {
    .\security.ps1 Set-AclForced -Acl $acl -Path $logsPath
}