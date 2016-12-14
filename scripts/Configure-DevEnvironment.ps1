# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


#Requires -RunAsAdministrator
#Requires -Version 4.0
$scriptDir = Split-Path $script:MyInvocation.MyCommand.Path

function AllowWebConfigOverrides {
    Write-Host "Setting up override settings in applicationHost.config."
    $appHostPath = Join-Path $scriptDir "..\.vs\config\applicationHost.config"
    if (-not(Test-Path $appHostPath)) {
        Write-Warning "Cannot find applicationHost.config file. Run the project through visual studio to generate the file then re-run this script."
        return
    }
    $appHostPath = $(Resolve-Path $appHostPath).Path

    Add-Type -LiteralPath C:\Windows\System32\inetsrv\Microsoft.Web.Administration.dll
    $sm = New-Object "Microsoft.Web.Administration.ServerManager" -ArgumentList "$appHostPath"
    $appHost = $sm.GetApplicationHostConfiguration()
    $ws = $appHost.GetSection("system.webServer/security/authentication/windowsAuthentication")
    $as = $appHost.GetSection("system.webServer/security/authentication/anonymousAuthentication")
    
    $ws.OverrideMode = [Microsoft.Web.Administration.OverrideMode]::Allow
    $as.OverrideMode = [Microsoft.Web.Administration.OverrideMode]::Allow
    $sm.CommitChanges()
}

function AddCurrentUserToIISAdministrators {
    Write-Host "Configuring local IIS Administrators group."
    $group = .\setup\security.ps1 GetLocalGroup -Name $(.\setup\globals.ps1 IISAdministratorsGroupName)
    
    if ($group -ne $null -and (-not (.\setup\security.ps1 GroupEquals -Group $group -Name $(.\setup\globals.ps1 IISAdministratorsGroupName) -Description $(.\setup\globals.ps1 IISAdministratorsDescription) ))) {
        throw "Unknown IIS Administrators group exists."
    }
    else {
        if ($group -eq $null) {
            $group = .\setup\security.ps1 CreateLocalGroup -Name $(.\setup\globals.ps1 IISAdministratorsGroupName) -Description $(.\setup\globals.ps1 IISAdministratorsDescription)
        }

        $currentUser = .\setup\security.ps1 CurrentAdUser
        .\setup\security.ps1 AddUserToGroup -AdPath $currentUser -Group $group
    }
}

function SetEnvironmentVariables {
	if ($env:iis_admin_solution_dir -eq $null) {
		setx iis_admin_solution_dir $((Resolve-Path (Join-Path $scriptDir "..")).Path) /m
	}
}

try {
    Push-Location $scriptDir

	# The applicationHost.config file created by Visual Studio does not allow necesarry settings to be overwritten by default. 
    AllowWebConfigOverrides

	# Synchronize behavior with production scenario.
    AddCurrentUserToIISAdministrators

    SetEnvironmentVariables
}
finally {
    Pop-Location
}