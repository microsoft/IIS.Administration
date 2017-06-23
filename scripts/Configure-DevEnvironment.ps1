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

    Write-Host "Setting environment variables."
	if ($env:iis_admin_solution_dir -eq $null) {
		$env:iis_admin_solution_dir = $((Resolve-Path (Join-Path $scriptDir "..")).Path)
		setx iis_admin_solution_dir $env:iis_admin_solution_dir /m
        Write-Verbose "iis_admin_solution_dir $env:iis_admin_solution_dir"
	}

	if ($env:iis_admin_test_dir  -eq $null -and $ConfigureTestEnvironment) {
		$env:iis_admin_test_dir = $([System.IO.Path]::Combine("$env:SystemDrive\", "tests\iisadmin"))
		setx iis_admin_test_dir $env:iis_admin_test_dir /m
        Write-Verbose "iis_admin_test_dir $env:iis_admin_test_dir"
	}

    if ($ConfigureTestEnvironment) {
        Write-Host "Configuring test environment"
        .\tests\Create-CcsInfrastructure.ps1
    }
}
finally {
    Pop-Location
}