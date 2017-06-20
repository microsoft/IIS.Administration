# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


#Requires -RunAsAdministrator
#Requires -Version 4.0
$scriptDir = Split-Path $script:MyInvocation.MyCommand.Path

function SetEnvironmentVariables {
	if ($env:iis_admin_solution_dir -eq $null) {
		setx iis_admin_solution_dir $((Resolve-Path (Join-Path $scriptDir "..")).Path) /m
	}
}

try {
    Push-Location $scriptDir

    Write-Host "Setting environment variables."
    SetEnvironmentVariables
}
finally {
    Pop-Location
}