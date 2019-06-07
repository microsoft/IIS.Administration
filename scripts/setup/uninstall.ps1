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
    $KeepFiles,

    [parameter()]
    [switch]
    $KeepGroups
)

.\require.ps1 Is-Administrator

function Uninstall($_path)
{
    $adminRoot = $_path
    Write-Verbose "Uninistalling from $adminRoot"

	if (!(.\config.ps1 Exists -Path $adminRoot)) { 
		throw "Cannot find setup.config file for uninstall. Cannot continue"
	}

    $installedSettings = .\config.ps1 Get -Path $adminRoot

    if (!$Port) {
        $Port = $installedSettings.Port
    }

    if ([String]::IsNullOrEmpty($ServiceName)) {
        $ServiceName = $installedSettings.ServiceName
    }
    if ([String]::IsNullOrEmpty($ServiceName)) {
        $ServiceName = ""
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
        Write-Verbose "Deleting SSL binding for port: $Port"
        .\net.ps1 DeleteSslBinding -Port $Port
    }

    if ($ownsSvc) {
        sc.exe delete $ServiceName
    }
    
    .\cache.ps1 Destroy
    
    $InstallationDirectory = Get-Item $adminRoot -ErrorAction SilentlyContinue
    if ($InstallationDirectory -ne $null) {  

        # Add system full control to directory so MSI can remove the files
        if ($(.\globals.ps1 INSTALL_METHOD_VALUE) -eq "MSI") {
            try {
                $system = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::LocalSystemSid, $null)
                .\security.ps1 Add-FullControl -Path $InstallationDirectory.FullName -Identity $system -Recurse
            }
            catch {
                Write-Warning "Unable to obtain full control of installation directory: $($_.Exception.Message)"
            }
        }

        if (-not($KeepFiles)) {

            Try
            {
                $files = Get-ChildItem $InstallationDirectory.FullName

                foreach ($file in $files) {
                    if ($file.name -ne "setup") {
                        .\files.ps1 Remove-ItemForced -path $file.FullName -ErrorAction SilentlyContinue
                    }
                    else {
                        Get-ChildItem $file.FullName | ForEach-Object{ .\files.ps1 Remove-ItemForced -Path $_.FullName -ErrorAction SilentlyContinue }
                        .\files.ps1 Remove-ItemForced -Path $file.FullName -ErrorAction SilentlyContinue
                    }
                }

                .\files.ps1 Remove-ItemForced -Path $InstallationDirectory.FullName -ErrorAction Stop
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
                .\files.ps1 Remove-ItemForced -Path $setupConfig -ErrorAction Stop
            }
            catch {
                Write-Warning "Could not remove installation configuration file: $($_.Exception.Message)"
            }
        }
    }

    $groupName = .\globals.ps1 'IIS_ADMIN_API_OWNERS'
    $group = .\security.ps1 GetLocalGroup -Name $groupName
    $installerFlag = .\globals.ps1 'INSTALLER_FLAG'
    if (!$KeepGroups) {
        if ($group -and $group.Description.Contains($installerFlag)) {
            .\security.ps1 RemoveLocalGroup -Name $groupName
        }
    }

    exit 0
}

Uninstall $Path
