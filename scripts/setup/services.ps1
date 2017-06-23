# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Is-Owner",
                 "Get-ServiceRegEntry",
                 "Create-IisAdministration")]
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
    $rootIndex = -1
    $launcher = "dotnet.exe"

    $reg = Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Services\$($_service.Name)"

    # If imagepath contains Microsoft.IIS.Host.exe the service is using hostable web core
    # Ex: C:\Program Files\IIS Administration\1.0.39\host\OneCore\x64\Microsoft.IIS.Host.exe -appHostConfig:"C:\Program Files\IIS Administration\1.0.39\host\applicationHost.config" -serviceName:"Microsoft IIS Administration"
    if ($reg.ImagePath.Contains('Microsoft.IIS.Host.exe')) {
        $platform = "OneCore"
        if (!$(.\globals.ps1 ONECORE)) {
            $platform = "Win32"
        }
        $imagePath = $reg.ImagePath.Substring(0, $reg.ImagePath.IndexOf(".exe") + ".exe".Length)
        $rootIndex = $imagePath.IndexOf("\host\$platform\x64\Microsoft.IIS.Host.exe")
    }
    # If imagepath contains Microsoft.IIS.Administration.dll the service is using Web Listener
    # Ex: dotnet "C:\Program Files\IIS Administration\1.1.1\Microsoft.IIS.Administration\Microsoft.IIS.Administration.dll" /serviceName="Microsoft IIS Administration"
    elseif ($reg.ImagePath.Contains('Microsoft.IIS.Administration.dll') -and $reg.ImagePath.Contains($launcher)) {
        $launcherIndex = $reg.ImagePath.IndexOf($launcher)
        $start = $launcherIndex + $launcher.Length
        $trimmed = $reg.ImagePath.Substring($start, $reg.ImagePath.Length - $start).Trim(' ', '"')
        $imagePath = $trimmed.Substring(0, $trimmed.IndexOf(".dll") + ".dll".Length)
        $rootIndex = $imagePath.IndexOf("\Microsoft.IIS.Administration\Microsoft.IIS.Administration.dll")
    }

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

# Creates the IIS Administration Service
# Name: The name for the service
# Path: The root path of the application, Ex: C:\Program Files\IIS Administration\1.1.1
function Create-IisAdministration($_name, $_path) {
    $dotnetPath = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"    

    if (-not(Test-Path $dotnetPath)) {
        Throw "Dotnet runtime launcher not found in expected location: $dotnetPath"
    }

    $svcDllPath = Join-Path $_path "Microsoft.IIS.Administration\Microsoft.IIS.Administration.dll"
    sc.exe create "$_name" depend= http binpath= "$dotnetPath \`"$svcDllPath\`" /serviceName=\`"$_name\`"" start= auto
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
    "Create-IisAdministration"
    {
        return Create-IisAdministration $Name $Path
    }
    default
    {
        throw "Unknown command"
    }
}