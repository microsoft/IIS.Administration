# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


#Requires -RunAsAdministrator
#Requires -Version 4.0
Param (
    [parameter(Mandatory=$true)]
    [string]
    $Destination,
    
    [parameter(Mandatory=$true)]
    [string]
    $Source
)

$Script:migrateRollback = @{}

function Cache-UserFiles($root) {
    # Copy all user files to temporary location
    $userFiles = .\installationconfig.ps1 Get-UserFileMap
    foreach ($fileName in $userFiles.keys) {

        try {
            .\cache.ps1 Store -Path $(Join-Path $root $userFiles[$fileName]) -Name $userFiles[$fileName]
        }
        catch {
                
            # Not generated until first run
            if ($fileName -eq 'api-keys.json') {
                Write-Warning "$filename could not be found for backup"
                continue;
            }

            Write-Warning $_.Exception.Message
            Write-Warning "Could not temporarily cache application files"
                
            throw;
        }
    }
}

function RestoreUserFiles($root) {
    $userFiles = .\installationconfig.ps1 Get-UserFileMap
    foreach ($fileName in $userFiles.keys) {
        $item = .\cache.ps1 Get -Name $userFiles[$fileName]

        if ($item -ne $null) {
            Copy-Item -Force $item.FullName (Join-Path $root $userFiles[$fileName]) 
        }
    }
}

function Rollback {
    Write-Warning "Rolling back migration."
    
    #
    # Stop new service that we started
    if ($migrateRollback.startedNewService -ne $null) {
        try {
            Stop-Service $migrateRollback.startedNewService -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not stop newly created service $($migrateRollback.startedNewService)"
        }
    }

    #
    # Restore destination service we may have deleted
    if ($migrateRollback.deletedDestinationSvc -ne $null) {

        $name = $migrateRollback.deletedDestinationSvc.Name
        $startType = $migrateRollback.deletedDestinationSvcStartType
        $binaryPath = $migrateRollback.deletedDestinationSvcImagePath

        Write-Host "Rolling back service $name"

        try {
            New-Service -BinaryPathName $binaryPath -StartupType $startType -DisplayName $name -Name $name -ErrorAction Stop | Out-Null
        }
        catch {
            Write-Warning "Could not restore the $($name) service."
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
            Write-Warning "Could not remove newly created service '$($migrateRollback.createdNewService)'"
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
            Write-Warning "Could not restore the $($name) service."
        }
    }

    #
    # Restore destination user files we cached
    if ($migrateRollback.cachedUserFiles -ne $null) {
        "Rolling back destination application files"

        RestoreUserFiles $migrateRollback.cachedUserFiles
    }

    #
    # Restart destination service we stopped
    if ($migrateRollback.stoppedDestinationService -ne $null) {
        Write-Host "Restarting the $($migrateRollback.stoppedDestinationService) service."

        try {
            Start-Service $migrateRollback.stoppedDestinationService -ErrorAction Stop
        }
        catch {
            Write-Warning "Could not restart destination service"
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
            Write-Warning "Could not restart source service"
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

    $sourceSettings = .\installationconfig.ps1 Get -Path $Source

    if ($sourceSettings -eq $null) {
        throw "Cannot find installation settings for source."
    }
    if ([string]::IsNullOrEmpty($sourceSettings.ServiceName)) {
        throw "Cannot find source service name."
    }

    $destinationSettings = .\installationconfig.ps1 Get -Path $Destination

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
        Stop-Service $sourceSvc -ErrorAction Stop
        $migrateRollback.stoppedSourceService = $sourceSvc.Name
    }
    if ($destinationSvc.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
        Stop-Service $destinationSvc -ErrorAction Stop
        $migrateRollback.stoppedDestinationService = $destinationSvc.Name
    }

    $userFiles = .\installationconfig.ps1 Get-UserFileMap

    Cache-UserFiles $Destination
    $migrateRollback.cachedUserFiles = $Destination

    # Modules should be the union of destination and source modules
    $oldModules = .\modules.ps1 Get-JsonContent -Path $(Join-Path $Source $userFiles["modules.json"])
    $newModules = .\modules.ps1 Get-JsonContent -Path $(Join-Path $Destination $userFiles["modules.json"])
    $oldModules.modules = .\modules.ps1 Add-NewModules -OldModules $oldModules.modules -NewModules $newModules.modules

    foreach ($fileName in $userFiles.keys) {
        Copy-Item -Force -Recurse $(Join-Path $Source $userFiles[$fileName]) $(Join-Path $Destination $userFiles[$fileName]) -ErrorAction SilentlyContinue
    }

    .\modules.ps1 Set-JsonContent -Path $(Join-Path $Destination $userFiles["modules.json"]) -JsonObject $oldModules

    $appHostPath = Join-Path $Destination host\applicationHost.config
    $appPath = Join-Path $Destination Microsoft.IIS.Administration
    $Port = $sourceSettings.Port

    # Configure applicationHost.config based on install parameters
    .\installationconfig.ps1 Write-AppHost -AppHostPath $appHostPath -ApplicationPath $appPath -Port $Port

    Start-Service $destinationSvc -ErrorAction Stop
    Stop-Service $destinationSvc -ErrorAction Stop

    if ($sourceSvc -ne $null) { 
        $svc = .\services.ps1 Get-ServiceAsWmiObject -Name $sourceSvc.Name

        if ($svc -eq $null) {
            throw "Could not access service information through WMI."
        }

        $migrateRollback.deletedSourceSvc = $sourceSvc
        $migrateRollback.deletedSourceSvcStartType = $sourceSvc.StartType
        $migrateRollback.deletedSourceSvcImagePath = $svc.PathName
        
        sc.exe delete "$($sourceSvc.Name)" | Out-Null

        if ($LASTEXITCODE -ne 0) {
            $migrateRollback.deletedSourceSvc = $null
            throw "Could not delete source service"
        }
    }

    $platform = "onecore"
    if (!$ONECORE) {
        $platform = "win32"
    }
    
    # Register the Self Host exe as a service
    $svcExePath = Join-Path $destination "host\x64\$platform\Microsoft.IIS.Host.exe"
    sc.exe create "$($sourceSettings.ServiceName)" binpath= "$svcExePath -appHostConfig:\`"$appHostPath\`" -serviceName:\`"$($sourceSvc.Name)\`"" start= auto | Out-Null

    if ($LASTEXITCODE -ne 0) {
        throw "Could not create new service"
    }
    $migrateRollback.createdNewService = $sourceSettings.ServiceName

    if ($sourceSvc -ne $null -and $destinationSvc.Name -ne $sourceSvc.Name) {
        $svc = .\services.ps1 Get-ServiceAsWmiObject -Name $destinationSvc.Name

        if ($svc -eq $null) {
            throw "Could not access service information through WMI."
        }

        $migrateRollback.deletedDestinationSvc = $destinationSvc
        $migrateRollback.deletedDestinationSvcStartType = $destinationSvc.StartType
        $migrateRollback.deletedDestinationSvcImagePath = $svc.PathName

        sc.exe delete "$($destinationSvc.Name)" | Out-Null

        if ($LASTEXITCODE -ne 0) {
            $migrateRollback.deletedDestinationSvc -eq $null
            throw "Could not delete destination service"
        }
    }

    $svc = Get-Service $sourceSvc.Name
    Start-Service $svc -ErrorAction Stop

    $migrateRollback.startedNewService = $sourceSettings.ServiceName

    $installObject = @{
        InstallPath = $Destination
        Port = $Port
        ServiceName = $sourceSvc.Name
        Version = $destinationSettings.Version
        Installer = $([System.Environment]::UserDomainName + '/' + [System.Environment]::UserName)
		Date = Date
		CertificateThumbprint = $sourceSettings.CertificateThumbprint
    }

    .\installationconfig.ps1 Write-Config -ConfigObject $installObject -Path $Destination

    Write-Host "Migration complete, URI: https://localhost:$Port"
}

try {
    Migrate
}
catch {
    Rollback
    throw
}