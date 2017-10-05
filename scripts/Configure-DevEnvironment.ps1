# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Param(
    [switch]
    $ConfigureTestEnvironment
)

#Requires -RunAsAdministrator
#Requires -Version 4.0
$scriptDir = Split-Path $script:MyInvocation.MyCommand.Path

try {
    Push-Location $scriptDir

    $solutionDir = (Resolve-Path (Join-Path $scriptDir "..")).Path

    Write-Host "Setting environment variables."
	if ($env:iis_admin_solution_dir -eq $null) {
		$env:iis_admin_solution_dir = $solutionDir
		setx iis_admin_solution_dir $env:iis_admin_solution_dir /m
        Write-Verbose "iis_admin_solution_dir $env:iis_admin_solution_dir"
	}

	if ($env:iis_admin_test_dir  -eq $null -and $ConfigureTestEnvironment) {
		$env:iis_admin_test_dir = $([System.IO.Path]::Combine("$env:SystemDrive\", "tests\iisadmin"))
		setx iis_admin_test_dir $env:iis_admin_test_dir /m
        Write-Verbose "iis_admin_test_dir $env:iis_admin_test_dir"
	}

    # Check for existence of app configuration files
    # Create them from defaults if they don't exist
    $configDir = Join-Path $solutionDir "src/microsoft.iis.administration/config"

    if (-not(Test-Path $(Join-Path $configDir "appsettings.json"))) {
        Copy-Item $(Join-Path $configDir "appsettings.default.json") $(Join-Path $configDir "appsettings.json")

        Write-Host "appsettings.json created at $(Join-Path $configDir "appsettings.json")"
        Write-Host "Add users to the 'users' section to grant access to the application"
        Write-Host "For more info about the security configuration visit https://docs.microsoft.com/en-us/iis-administration/configuration/appsettings.json#security"
    }

    if (-not(Test-Path $(Join-Path $configDir "modules.json"))) {
        Copy-Item $(Join-Path $configDir "modules.default.json") $(Join-Path $configDir "modules.json")
    }

    if ($ConfigureTestEnvironment) {
        Write-Host "Configuring test environment"
        .\tests\Create-CcsInfrastructure.ps1
    }
}
finally {
    Pop-Location
}