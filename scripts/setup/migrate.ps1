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
            Write-Warning "Could not stop newly created service $($migrateRollback.startedNewService)"
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
        # Never fail
    }

    if ($destinationSvc.Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
        Stop-Service $destinationSvc.Name -ErrorAction Stop
    }

    $userFiles = .\config.ps1 Get-UserFileMap

    # Modules should be the union of destination and source modules
    $oldModules = .\modules.ps1 Get-JsonContent -Path $(Join-Path $Source $userFiles["modules.json"])
    $newModules = .\modules.ps1 Get-JsonContent -Path $(Join-Path $Destination $userFiles["modules.json"])
    $joined = .\modules.ps1 Add-NewModules -OldModules $oldModules.modules -NewModules $newModules.modules
    $filtered = .\modules.ps1 Remove-DeprecatedModules -Modules $joined
    $oldModules = @{modules = $filtered}

    foreach ($fileName in $userFiles.keys) {
        .\files.ps1 Copy-FileForced -Source $(Join-Path $Source $userFiles[$fileName]) -Destination $(Join-Path $Destination $userFiles[$fileName]) -ErrorAction SilentlyContinue
    }

    .\modules.ps1 Set-JsonContent -Path $(Join-Path $Destination $userFiles["modules.json"]) -JsonObject $oldModules

    $appHostPath = Join-Path $Destination host\applicationHost.config
    $appPath = Join-Path $Destination Microsoft.IIS.Administration
    $sourceAppHostPath = Join-Path $Source host\applicationHost.config
    $Port = .\config.ps1 Get-AppHostPort -AppHostPath $sourceAppHostPath

    $destDir = Get-Item $Destination

    # Configure applicationHost.config based on install parameters
    .\config.ps1 Write-AppHost -AppHostPath $appHostPath -ApplicationPath $appPath -Port $Port -Version $destDir.Name

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

    $platform = "onecore"
    if (!$(.\globals.ps1 ONECORE)) {
        $platform = "win32"
    }
    
    # Register the Self Host exe as a service
    $svcExePath = Join-Path $destination "host\$platform\x64\Microsoft.IIS.Host.exe"
    sc.exe create "$($sourceSettings.ServiceName)" depend= http binpath= "$svcExePath -appHostConfig:\`"$appHostPath\`" -serviceName:\`"$($sourceSvc.Name)\`"" start= auto | Out-Null

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
        Port = $Port
        ServiceName = $sourceSvc.Name
        Version = $destinationSettings.Version
        Installer = $([System.Environment]::UserDomainName + '/' + [System.Environment]::UserName)
		Date = Date
		CertificateThumbprint = $sourceSettings.CertificateThumbprint
    }

    .\config.ps1 Write-Config -ConfigObject $installObject -Path $Destination

    .\security.ps1 Set-Acls -Path $Destination

    Write-Host "Migration complete, URI: https://localhost:$Port"
}

try {
    .\require.ps1 Is-Administrator
    Migrate
}
catch {
    Rollback
    throw
}