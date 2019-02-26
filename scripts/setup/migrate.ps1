# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true)]
    [string]
    $Destination,
    
    [parameter(Mandatory=$true)]
    [string]
    $Source
)

$Script:migrateRollback = @{}

function Rollback {
    Write-Warning "Rolling back migration."
    
    #
    # Stop new service that we started
    if ($migrateRollback.startedNewService -ne $null) {
        try {
            Stop-Service $migrateRollback.startedNewService -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not stop newly created service $($migrateRollback.startedNewService): $($_.Exception.Message)"
        }
    }

    #
    # Delete new service we created
    if ($migrateRollback.createdNewService -ne $null) {
        Write-Host "Rolling back service creation"

        try {
            sc.exe delete "$($migrateRollback.createdNewService)" | Out-Null
        }
        catch {
            Write-Warning "Could not remove newly created service '$($migrateRollback.createdNewService)': $($_.Exception.Message)"
        }
    }

    #
    # Restore source service we may have deleted
    if ($migrateRollback.deletedSourceSvc -ne $null) {

        $name = $migrateRollback.deletedSourceSvc.Name
        $startType = $migrateRollback.deletedSourceSvcStartType
        $binaryPath = $migrateRollback.deletedSourceSvcImagePath

        Write-Host "Rolling back service $name"

        try {
            New-Service -BinaryPathName $binaryPath -StartupType $startType -DisplayName $name -Name $name -ErrorAction Stop | Out-Null
        }
        catch {
            Write-Warning "Could not restore the $($name) service: $($_.Exception.Message)"
        }
    }  

    #
    # Restart source service we stopped
    if ($migrateRollback.stoppedSourceService -ne $null) {
        Write-Host "Restarting the $($migrateRollback.stoppedSourceService) service."

        try {
            Start-Service $migrateRollback.stoppedSourceService -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not restart source service: $($_.Exception.Message)"
        }
    }
}

function Migrate {
    if ([System.String]::IsNullOrEmpty($Source)) {
        throw "Source path required."
    }
    if ([System.String]::IsNullOrEmpty($Destination)) {
        throw "Destination path required."
    }

    $sourceSettings = .\config.ps1 Get -Path $Source

    if ($sourceSettings -eq $null) {
        throw "Cannot find installation settings for source."
    }
    if ([string]::IsNullOrEmpty($sourceSettings.ServiceName)) {
        throw "Cannot find source service name."
    }

    $destinationSettings = .\config.ps1 Get -Path $Destination

    if ($destinationSettings -eq $null) {
        throw "Cannot find installation settings for destination."
    }
    if ([string]::IsNullOrEmpty($destinationSettings.ServiceName)) {
        throw "Cannot find destination service name."
    }

    $sourceSvc = Get-Service $sourceSettings.ServiceName
    $destinationSvc = Get-Service $destinationSettings.ServiceName

    if ($destinationSvc -eq $null) {
        throw "Destination service not found"
    }

    if ($sourceSvc -ne $null -and $sourceSvc.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
        Stop-Service $sourceSvc.Name -ErrorAction Stop
        $migrateRollback.stoppedSourceService = $sourceSvc.Name
    }

    # Do any necessary sanitization of log files
    try {
        .\sanitize-logs.ps1 -Source $source
    }
    catch {
        Write-Warning "Error sanitizing logs: $($_.Exception.Message)"
        # Never fail
    }

    if ($destinationSvc.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
        Stop-Service $destinationSvc.Name -ErrorAction Stop
    }
    
    $sslBindingInfo = $null

    $oldServicePort = .\config.ps1 Get-ConfigPort -Path $Source
    $newServicePort = .\config.ps1 Get-ConfigPort -Path $Destination
    
    # Get the certificate info used by the old service
    $oldServiceCertInfo = .\net.ps1 GetSslBindingInfo -Port $oldServicePort
    $newServiceCertInfo = .\net.ps1 GetSslBindingInfo -Port $newServicePort

    $oldServiceUsesIisAdminCert = .\cert.ps1 Is-IISAdminCertificate -Thumbprint $oldServiceCertInfo.CertificateHash
    $newServiceUsesIisAdminCert = .\cert.ps1 Is-IISAdminCertificate -Thumbprint $newServiceCertInfo.CertificateHash

    if ($oldServiceUsesIisAdminCert -and $newServiceUsesIisAdminCert) {

        # Migration moves an old service's settings to a new service, thus the new service will begin using the old service's port
        # Here we copy over binding info if the services are using the IIS Administration certificate to enable certificate renewal
        $sslBindingInfo = .\net.ps1 CopySslBindingInfo -SourcePort $newServicePort -DestinationPort $oldServicePort
    }
    else {

        $sslBindingInfo = $oldServiceCertInfo
    }

    # Remove unused binding
    if ($newServiceUsesIisAdminCert -and $oldServicePort -ne $newServicePort) {

        # The migration causes the new service's port to become unused since it will begin using the old service's port
        # As long as the old service's port and new service's port aren't the same, we need to clean it up
        .\net.ps1 DeleteSslBinding -Port $newServicePort
    }

    $userFiles = .\config.ps1 Get-UserFileMap

    .\modules.ps1 Migrate-Modules -Source $Source -Destination $Destination
    .\config.ps1 Migrate-AppSettings -Source $Source -Destination $Destination
    .\files.ps1 Copy-FileForced -Source $(Join-Path $Source $userFiles["api-keys.json"]) -Destination $(Join-Path $Destination $userFiles["api-keys.json"]) -ErrorAction SilentlyContinue

    Start-Service $destinationSvc.Name -ErrorAction Stop
    Stop-Service $destinationSvc.Name -ErrorAction Stop

    if ($sourceSvc -ne $null) { 
        $svc = .\services.ps1 Get-ServiceRegEntry -Name $sourceSvc.Name

        if ($svc -eq $null) {
            throw "Could not access service information in registry."
        }

        $migrateRollback.deletedSourceSvc = $sourceSvc
        $migrateRollback.deletedSourceSvcStartType = [System.ServiceProcess.ServiceStartMode]$svc.Start
        $migrateRollback.deletedSourceSvcImagePath = $svc.ImagePath
        
        sc.exe delete "$($sourceSvc.Name)" | Out-Null

        if ($LASTEXITCODE -ne 0) {
            $migrateRollback.deletedSourceSvc = $null
            throw "Could not delete source service"
        }
    }
    
    # Register the Self Host exe as a service
    .\services.ps1 Create-IisAdministration -Name $sourceSettings.ServiceName -Path $destination

    if ($LASTEXITCODE -ne 0) {
        throw "Could not create new service"
    }
    $migrateRollback.createdNewService = $sourceSettings.ServiceName

    if ($sourceSvc -ne $null -and $destinationSvc.Name -ne $sourceSvc.Name) {
        sc.exe delete "$($destinationSvc.Name)" | Out-Null

        if ($LASTEXITCODE -ne 0) {
            throw "Could not delete destination service"
        }
    }

    $svc = Get-Service $sourceSvc.Name
    Start-Service $svc.Name -ErrorAction Stop

    $migrateRollback.startedNewService = $sourceSettings.ServiceName

    $installObject = @{
        InstallPath = $Destination
        Port = $destinationPort
        ServiceName = $sourceSvc.Name
        Version = $destinationSettings.Version
        Installer = $([System.Environment]::UserDomainName + '/' + [System.Environment]::UserName)
		Date = Date
		CertificateThumbprint = $sourceSettings.CertificateThumbprint
    }

    .\config.ps1 Write-Config -ConfigObject $installObject -Path $Destination

    .\security.ps1 Set-Acls -Path $Destination

    # Remove expired IIS Administration certificates
    $certs = .\cert.ps1 Get-IISAdminCertificates
    foreach ($cert in $certs) {
        if ((($cert.NotAfter - [System.DateTime]::Now).TotalDays -lt 0) -and $cert.Thumbprint.ToLower() -ne $sslBindingInfo.CertificateHash.ToLower()) {
            Write-Verbose "Removing old IIS Administration Certificate"
            .\cert.ps1 Delete -Thumbprint $cert.Thumbprint
        }
    }

    Write-Host "Migration complete, URI: https://localhost:$destinationPort"
}

try {
    .\require.ps1 Is-Administrator
    Migrate
}
catch {
    Rollback
    throw
}