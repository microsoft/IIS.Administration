# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


#Requires -RunAsAdministrator
#Requires -Version 4.0
Param(    
    [parameter(Position=0)]
    [string]
    $Path,
    
    [parameter()]
    [switch]
    $DeleteCert,
    
    [parameter()]
    [switch]
    $DeleteBinding,
    
    [parameter()]
    [switch]
    $DeleteGroup,
    
    [parameter()]
    [switch]
    $KeepFiles
)

function Uninstall($_path)
{
    $adminRoot = $_path

	if (!(.\installationconfig.ps1 Exists -Path $adminRoot)) { 
		throw "Cannot find setup.config file for uninstall. Cannot continue"
	}

    $installedSettings = .\installationconfig.ps1 Get -Path $adminRoot

    if ($Port -eq 0) {
        $Port = $installedSettings.Port
    }

    if ([String]::IsNullOrEmpty($ServiceName)) {
        $ServiceName = $installedSettings.ServiceName
    }

    $svc = Get-Service $ServiceName -ErrorAction SilentlyContinue

    # Check if service belongs to the application being uninstalled
    $ownsSvc = $false
    
    if ($svc -ne $null) {
        $ownsSvc = .\services.ps1 Is-Owner -Path $adminRoot -Service $svc
    }

    # Bring down the service before interacting with it in any way
    if ($ownsSvc) {
        Stop-Service $ServiceName
    }

    if ($DeleteCert) {
        Write-Verbose "Deleting certificate"
        .\cert.ps1 Delete -Thumbprint $installedSettings.CertificateThumbprint
    }

    if ($DeleteBinding) {
        Write-Verbose "Deleting SSL binding"
        .\network.ps1 DeleteSslBinding -Port $Port
    }

    if ($DeleteGroup) {
        Write-Verbose "Deleting $(.\constants.ps1 IISAdministratorsGroupName) group"
        .\activedirectory.ps1 RemoveLocalGroup -Name $(.\constants.ps1 IISAdministratorsGroupName)
    }

    if ($ownsSvc) {
        sc.exe delete $ServiceName
    }
    
    .\cache.ps1 Destroy
    
    $InstallationDirectory = Get-Item $adminRoot -ErrorAction SilentlyContinue
    if ($InstallationDirectory -ne $null -and -not($KeepFiles)) {

        try {
            .\acl.ps1 Add-SelfRights -Path $InstallationDirectory.FullName
        }
        catch {
            Write-Warning "Unable to obtain full control of installation directory"
        }

        Try
        {
            $files = Get-ChildItem $InstallationDirectory.FullName

            foreach ($file in $files) {
                if ($file.name -ne "setup") {
                    Remove-Item $file.FullName -Force -Recurse -ErrorAction Stop
                }
            }

            Remove-Item $InstallationDirectory.FullName -Recurse -Force
            Write-Verbose "Successfully removed installation folder."
        }
        Catch
        {
            Write-Warning $_.Exception.Message
            Write-Warning "Could not remove installation folder"
        }
    }
    exit 0
}

Uninstall $Path