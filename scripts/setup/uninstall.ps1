# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


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

.\require.ps1 Is-Administrator

function Uninstall($_path)
{
    $adminRoot = $_path

	if (!(.\config.ps1 Exists -Path $adminRoot)) { 
		throw "Cannot find setup.config file for uninstall. Cannot continue"
	}

    $installedSettings = .\config.ps1 Get -Path $adminRoot

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
        .\net.ps1 DeleteSslBinding -Port $Port
    }

    if ($DeleteGroup) {
        Write-Verbose "Deleting $(.\globals.ps1 IISAdministratorsGroupName) group"
        .\security.ps1 RemoveLocalGroup -Name $(.\globals.ps1 IISAdministratorsGroupName)
    }

    if ($ownsSvc) {
        sc.exe delete $ServiceName
    }
    
    .\cache.ps1 Destroy
    
    $InstallationDirectory = Get-Item $adminRoot -ErrorAction SilentlyContinue
    if ($InstallationDirectory -ne $null) {   
        try {
            .\security.ps1 Add-SelfRights -Path $InstallationDirectory.FullName
        }
        catch {
            Write-Warning "Unable to obtain full control of installation directory"
        }     
        if (-not($KeepFiles)) {

            Try
            {
                $files = Get-ChildItem $InstallationDirectory.FullName

                foreach ($file in $files) {
                    if ($file.name -ne "setup") {
                        Remove-Item $file.FullName -Force -Recurse -ErrorAction SilentlyContinue
                    }
                    else {
                        Get-ChildItem $file.FullName | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
                        Remove-Item $file.FullName -Force -Recurse -ErrorAction SilentlyContinue
                    }
                }

                Remove-Item $InstallationDirectory.FullName -Force -Recurse -ErrorAction Stop
                Write-Verbose "Successfully removed installation folder."
            }
            Catch
            {
                Write-Warning $_.Exception.Message
            }
        }
        else {
            try {
                $setupConfig = Get-Item $(Join-Path $InstallationDirectory.FullName "setup.config")
                Remove-Item $setupConfig -Force -ErrorAction Stop
            }
            catch {
                Write-Warning "Could not remove installation configuration file"
            }
        }
    }
    exit 0
}

Uninstall $Path