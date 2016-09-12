# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Store",
                 "Get",
                 "Destroy")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [string]
    $Name
)

# Cache directory for backing up files
$USER_FILE_CACHE = "$env:USERPROFILE/appdata/local/IIS Administration"

function Store($_path, $_name) {

	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path required"
	}

	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name required"
	}
    
    if (-not(Test-Path $_path)) {
        throw "$_path not found."
    }

    $finalPath = $(Join-Path $USER_FILE_CACHE $_name)

    # Ensure directory structure created
    Remove-Item -Force -Recurse $finalPath -ErrorAction SilentlyContinue
    New-Item -Type File -Force $finalPath -ErrorAction Stop | Out-Null
    Remove-Item -Force -Recurse $finalPath

    Copy-Item -Force -Recurse $_path $finalPath -ErrorAction Stop
}

function Get($_name) {

	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name required"
	}
    
    $storedPath = $(Join-Path $USER_FILE_CACHE $_name)

    if (-not(Test-Path $storedPath)) {
        return $null
    }

    return Get-Item $storedPath
}

function Destroy {
    Remove-Item -Force -Recurse $USER_FILE_CACHE -ErrorAction SilentlyContinue | Out-Null
}

switch ($Command)
{
    "Store"
    {
        Store $Path $Name
    }
    "Get"
    {
        return Get $Name
    }
    "Destroy"
    {
        Destroy
    }
    default
    {
        throw "Unknown command"
    }
}

