# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Migrate-Modules")]
    [string]
    $Command,

    [parameter()]
    [string]
    $Source,
    
    [parameter()]
    [string]
    $Destination
)

# Modules no longer in use
$DEPRECATED_MODULES = @("Microsoft.IIS.Administration.WebServer.Transactions")

# Migrates IIS Administration module settings from one installation to another
# Modules added in the destination installation are kept
# Module enabled status is preserved
# OldModulesPath: The full path to the modules.json file that is being migrated from
# NewModulesPath: The full path to the modules.json file that is being migrated to
function Migrate-Modules($_source, $_destination) {
    $userFiles = .\config.ps1 Get-UserFileMap

    $oldPath = $(Join-Path $_source $userFiles["modules.json"])
    $newPath = $(Join-Path $_destination $userFiles["modules.json"])

    $oldModules = .\json.ps1 Get-JsonContent -Path $oldPath
    $newModules = .\json.ps1 Get-JsonContent -Path $newPath

    $joined = Add-NewModules -_oldModules $oldModules.modules -_newModules $newModules.modules
    $filtered = Remove-DeprecatedModules -_modules $joined

    $oldModules = @{modules = $filtered}

    .\json.ps1 Set-JsonContent -Path $newPath -JsonObject $oldModules
}

# Given two arrays of modules, The union of the two are returned.
# OldModules: The original modules list.
# NewModules: The new modules list used to update.
function Add-NewModules($_oldModules, $_newModules) {

    if ($_oldModules -eq $null) {
        throw "OldModules required"
    }

    if ($_newModules -eq $null) {
        throw "NewModules required"
    }

    $ms = New-Object "System.Collections.ArrayList"
    foreach ($module in $_oldModules) {
        $ms.Add($module) | out-null
    }
    
    foreach ($module in $_newModules) {
        $exists = $false

        foreach ($oldModule in $_oldModules) {

            if ($oldModule.name -eq $module.name) {
                $exists = $true
                break
            }
        }

        if (-not($exists)) {
            $ms.Add($module) | out-null
        }
    }

    $ms
}

# Given an array of modules, returns a filtered array containing no deprecated modules.
# Modules: The list of modules.
function Remove-DeprecatedModules($_modules) {
    $ms = New-Object "System.Collections.ArrayList"
    foreach ($module in $_modules) {
        if (-not(_Contains $DEPRECATED_MODULES $module.name)) {
            $ms.Add($(.\json.ps1 To-HashObject -JsonObject $module)) | out-null
        }
    }

    $ms
}

function _Contains($arr, $val) {
    foreach ($item in $arr) {
        if ($item -eq $val) {
            return $true
        }
    }
    return $false
}

switch ($Command)
{
    "Migrate-Modules"
    {
        Migrate-Modules $Source $Destination
    }
    default
    {
        throw "Unknown command"
    }
}

