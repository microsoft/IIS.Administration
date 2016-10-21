# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Is-Owner",
                 "Get-ServiceRegEntry")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [System.ServiceProcess.ServiceController]
    $Service,
    
    [parameter()]
    [string]
    $Name
)

# Checks if a given path owns a service. This is done by checking if the executable for that service is a child of the provided path.
# Service: The target service.
# Path: The path to test for possesion of the service.
function IsOwner($_service, $_path) {
    if ($_service -eq $null) {
        throw "Service required."
    }
    if ([string]::IsNullOrEmpty($_path)) {
        throw "Path required."
    }
    
    $ownsSvc = $false

    $reg = Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\$($_service.Name)"
    $imagePath = $reg.ImagePath.Substring(0, $reg.ImagePath.IndexOf(".exe") + ".exe".Length)
    $rootIndex = $imagePath.IndexOf("\host\")

    if ($rootIndex -ne -1) {
        $imageRoot = $imagePath.Substring(0, $rootIndex)
        $ownsSvc = [System.IO.Path]::GetFullPath($_path).TrimEnd(' ', '\') -eq [System.IO.Path]::GetFullPath($imageRoot).TrimEnd(' ', '\')
    }

    return $ownsSvc
}

# Retrieves registry entry for a service.
# Name: The name of the service.
function Get-ServiceRegEntry($_name) {
    if ([string]::IsNullOrEmpty($_name)) {
        throw "Name required."
    }
    $reg = Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\$_name" -ErrorAction SilentlyContinue
    return $reg
}

switch ($Command)
{
    "Is-Owner"
    {
        return IsOwner $Service $Path
    }
    "Get-ServiceRegEntry"
    {
        return Get-ServiceRegEntry $Name
    }
    default
    {
        throw "Unknown command"
    }
}