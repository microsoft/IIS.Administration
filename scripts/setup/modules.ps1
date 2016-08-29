# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Get-JsonContent",
                 "Add-NewModules",
                 "Set-JsonContent")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [System.Array]
    $OldModules,
    
    [parameter()]
    [System.Array]
    $NewModules,
    
    [parameter()]
    [System.Object]
    $JsonObject
)

function Get-JsonContent($_path)
{
	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path required"
	}

    if (-not(Test-Path $_path)) {
        throw "$_path not found."
    }

    $lines = Get-Content $Path

    $content = ""

    foreach ($line in $lines) {
        $content += $line
    }

    return ConvertFrom-Json $content
}

function Set-JsonContent($_path, $jsonObject) {

	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path required"
	}

    if ($jsonObject -eq $null) {
        throw "JsonObject required"
    }

    New-Item -Type File $_path -Force -ErrorAction Stop | Out-Null

    ConvertTo-Json $jsonObject -Depth 100 | Out-File $_path -ErrorAction Stop
}

function Add-NewModules($_oldModules, $_newModules) {

    if ($_oldModules -eq $null) {
        throw "OldModules required"
    }

    if ($_newModules -eq $null) {
        throw "NewModules required"
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
            $_oldModules += $module
        }
    }

    return $_oldModules
}

switch ($Command)
{
    "Add-NewModules"
    {
        return Add-NewModules $OldModules $NewModules
    }
    "Get-JsonContent"
    {
        return Get-JsonContent $Path
    }
    "Set-JsonContent"
    {
        Set-JsonContent $Path $JsonObject
    }
    default
    {
        throw "Unknown command"
    }
}

