# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Is-Administrator",
                 "DotNetServerHosting",
                 "VCRuntime")]
    [string]
    $Command
)

# Throws if the caller is not an Administrator
function Is-Administrator {
    Write-Verbose "Verifying user is an Administrator"
    if (-not(([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))) {
        throw "User must be an Administrator to continue."
    }
    Write-Verbose "Ok"
}

# Throws if DotNet Server Hosting has not been installed.
function DotNetServerHosting {
    Write-Verbose "Verifying .NET Core shared framework installed"
    if($(Get-Command "dotnet.exe" -ErrorAction SilentlyContinue) -eq $null) {
        Write-Warning ".NET Core Server Hosting tools not installed"
        Write-Warning "Download .NET Core Server Hosting tools from 'https://go.microsoft.com/fwlink/?LinkId=817246'"
        throw ".NET Core required to continue"
    }
    Write-Verbose "Ok"
    Write-Verbose "Verifying AspNet Core Module is installed"
	$aspNetCoreModuleSchemaInstalled = test-path "$env:windir\system32\inetsrv\config\schema\aspnetcore_schema.xml"
	$aspNetCoreModuleDllInstalled = test-path "$env:windir\system32\inetsrv\aspnetcore.dll"
    if (!$aspNetCoreModuleSchemaInstalled -or !$aspNetCoreModuleDllInstalled) {
        Write-Warning "ASP.Net Core Module not installed"
        Write-Warning "Download ASP.Net Core module from 'https://go.microsoft.com/fwlink/?LinkId=817246'"
        throw "Cannot install IIS Administration API without ASP.Net Core Module being installed"
    }
    Write-Verbose "Ok"
}

# Throws if VCRuntime has not been installed.
function VCRuntime {
    Write-Verbose "Verifying that the Visual C++ Runtime is installed"
	$vcRuntimeInstalled = test-path "$env:windir\system32\vcruntime140.dll"
    if (!$vcRuntimeInstalled) {
        Write-Warning "The Visual C++ 2015 Runtime cannot be found"
        Write-Warning "Download the Visual C++ 2015 Redistributable package from 'https://www.microsoft.com/en-us/download/details.aspx?id=53587'"
        throw "Cannot install IIS Administration API without the Visual C++ 2015 Runtime being installed"
    }
    Write-Verbose "Ok"
}

switch($Command)
{
    "Is-Administrator"
    {
        Is-Administrator
    }
    "DotNetServerHosting"
    {
        DotNetServerHosting
    }
    "VCRuntime"
    {
        VCRuntime
    }
    default
    {
        throw "Unknown command"
    }
}