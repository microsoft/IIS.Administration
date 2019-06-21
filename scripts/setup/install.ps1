# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true)]
    [string]
    $Path,

    [string]
    $Version,
    
    [parameter(Mandatory=$true)]
    [string]
    $ServiceName,
    
    [parameter()]
    [int]
    $Port,
    
    [parameter()]
    [string]
    $DistributablePath,
    
    [parameter()]
    [string]
    $CertHash,
    
    [parameter()]
    [switch]
    $SkipVerification,

    [parameter()]
    [switch]
    $DontCopy,

    [parameter()]
    [switch]
    $IncludeDefaultCors,

    [parameter()]
    [switch]
    $InstallCertOnly
)


$_rollbackStore = @{}

function StartService
{
    param (
    [Parameter(Mandatory=$true)][string]$name,
    [Parameter(Mandatory=$false)][int]$retries = 2,
    [Parameter(Mandatory=$false)][int]$secondsDelay = 1
    )

    $retrycount = 0
    $completed = $false

    while (-not $completed) {
        try {
            if ($retrycount -eq 0) {
                Write-Verbose ("Start-Service for {0} started." -f $name)
                Start-Service -name $name
            }
            $status = (Get-Service -name $name).Status

            Write-Verbose ("Checking the current status. Status: {0}" -f $status)
            if ($status -ne [System.ServiceProcess.ServiceControllerStatus]::Running) {
                Write-Warning ("Get-Service does not return Running. Status: {0}" -f $status)

                # AspNetCore based app service is slow to start and Start-Service fails occasionally. We need to retry in that case.
                if (($retrycount -eq 0) -and ($status -eq [System.ServiceProcess.ServiceControllerStatus]::Stopped)) {
                    Write-Verbose ("Retrying to call Start-Service for {0}." -f $name)
                    Start-Sleep $secondsDelay
                    Start-Service -name $name
                }
                throw ("Unexpected status: " + $status)
            }

            Write-Verbose ("Start-Service {0} succeeded." -f $name)
            $completed = $true

        } catch {
            if ($retrycount -ge $retries) {
                Write-Warning ("StartService failed the maximum number of {0} times. Error: {1}" -f $retrycount, $($_.Exception.Message))
                throw
            } else {
                Write-Verbose ("StartService failed. Retrying in {0} seconds. Error: {1}" -f $secondsDelay, $($_.Exception.Message))
                Start-Sleep $secondsDelay
                $retrycount++
            }
        }
    }
}

function Get-ScriptDirectory
{
    Split-Path $script:MyInvocation.MyCommand.Path
}

function CheckInstallParameters
{
    if ([String]::IsNullOrEmpty($Path)){
		throw "Path required."
    }
    if ([String]::IsNullOrEmpty($Version)){
		throw "Version required."
    }
    if ([String]::IsNullOrEmpty($DistributablePath) -and -not($DontCopy)){
        throw "Distributable path required"
    }
    if ([String]::IsNullOrEmpty($ServiceName)){
        throw "Service name required"
    }
    if ($Port -eq 0) {
        $script:Port = 55539
    }
	else {
		$script:PortIsExplicit = $true
	}

    $distributableDirectory = Get-Item $DistributablePath -ErrorAction SilentlyContinue
    if ($distributableDirectory -eq $null -or !($distributableDirectory -is [System.IO.DirectoryInfo]) -and -not($DontCopy)){
        throw "Invalid DistributablePath directory: $DistributablePath"
    }
}

function CheckUninstallParameters
{
    if ([String]::IsNullOrEmpty($Path)){
		$script:Path = $env:ProgramFiles
    }
}

function InstallationPreparationCheck
{
    Write-Host "Checking installation requirements"

    if (!$SkipVerification) { 
        Write-Verbose "Ok"
        # We require.NET 3.5 for JSON manipulation if it isn't available through built in powershell commands
        if ($(Get-Command "ConvertFrom-Json" -ErrorAction SilentlyContinue) -eq $null) {
            Write-Verbose ".NET 3.5 required for setup to continue"
            Write-Verbose "Verifying NetFx3 is Enabled"            
            try {
                $netfx3Enabled = .\dependencies.ps1 NetFx3Enabled
            }
            catch {
                $netfx3Enabled = $false
            }
            if (!$netfx3Enabled) {
                Write-Warning "NetFx3 not enabled"
			    Write-Host "Enabling NetFx3 (.NET Framework 3.5)"
			    try {
				    .\dependencies.ps1 EnableNetFx3
			    }
			    catch {
				    Write-Warning "Could not enable NetFx3 (.NET Framework 3.5)"
				    throw $_
			    }
            }
		    Write-Verbose "Ok"
        }
    }
    # Shared framework
    .\require.ps1 Dotnet

	Write-Verbose "Checking if port `'$Port`' is available"
    $available = .\net.ps1 IsAvailable -Port $Port
    if ($available) {
        Write-Verbose Ok
    }
    else {
        Write-Verbose "Not available"
    }

    Write-Host "Installation Requirements met"
}

function GetRollbackStore {
    return $_rollbackStore;
}

function rollback() {
    Write-Host "Rolling back"

    $rollbackStore = GetRollbackStore

    #
    # Delete any service we created
    if ($rollbackStore.createdService -ne $null) {
        Write-Host "Rolling back service creation"

        try {
            Stop-Service $rollbackStore.createdService -ErrorAction SilentlyContinue
        }
        catch {
            Write-Warning "Could not stop newly created service: $($_.Exception.Message)"
        }

        sc.exe delete "$($rollbackStore.createdService)" | Out-Null
    }

    #
    # Revert acls to allow removal of files
    if ($rollbackStore.setAclsPath -ne $null) {
        $system = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::LocalSystemSid, $null)
        .\security.ps1 Add-FullControl -Path $rollbackStore.setAclsPath -Identity $system -Recurse
        .\security.ps1 Add-SelfRights -Path $rollbackStore.setAclsPath -Recurse
    }

    #
    # Restore our service we may have deleted
    if ($rollbackStore.deletedSvc -ne $null) {

        $name = $rollbackStore.deletedSvc.Name
        $startType = $rollbackStore.deletedSvcStartType
        $binaryPath = $rollbackStore.deletedSvcBinaryPath

        Write-Host "Rolling back service $name"

        try {
            New-Service -BinaryPathName $binaryPath -StartupType $startType -DisplayName $name -Name $name -ErrorAction Stop | Out-Null
        }
        catch {
            Write-Warning "Could not restore the $($name) service: $($_.Exception.Message)"
        }
    }

    #
    # Remove any new HTTP.Sys binding
    if ($rollbackStore.newBoundCertPort -ne $null) {
        Write-Host "Rolling back HTTP.Sys port binding" 

        try {
            .\net.ps1 DeleteSslBinding -Port $rollbackStore.newBoundCertPort
        }
        catch {
            Write-Warning "Could not roll back SSL binding on port $($rollbackStore.newBoundCertPort): $($_.Exception.Message)"
        }
    }

    #
    # Restore any deleted HTTP.Sys binding
    if ($rollbackStore.preboundCertInfo -ne $null) {
    
        $info = $rollbackStore.preboundCertInfo
        try {
            Write-Host "Rolling back deleted SSL binding on port $($info.IpEndpoint.Port)"
            .\net.ps1 BindCert -Hash $($info.CertificateHash) -AppId $($info.AppId) -Port $($info.IpEndpoint.Port)
        }
        catch {
            Write-Warning "Could not restore previous SSL binding: $($_.Exception.Message)"
        }
    }

    #
    # Remove setup config
    if ($rollbackStore.createdConfigPath -ne $null) {
    
        $configPath = $rollbackStore.createdConfigPath
        try {
            Write-Host "Rolling back setup config creation"
            .\config.ps1 Remove -Path $configPath
        }
        catch {
            Write-Warning "Could not remove setup config: $($_.Exception.Message)"
        }
    }

    #
    # Remove any certificate we may have created 
    if ($rollbackStore.createdCertThumbprint -ne $null) {
        Write-Host "Rolling back SSL certificate creation" 

        $files = Get-ChildItem Cert:\LocalMachine -Recurse| where {$_.Thumbprint -eq $rollbackStore.createdCertThumbprint}

        try {
            foreach ($file in $files){
                remove-item $file.PSPath
            }
        }
        catch {
            write-warning "Could not delete certificate that was created during installation: $($_.Exception.Message)"
        }
    }

    #
    # Restart the existing service if we stopped it
    if ($rollbackStore.stoppedOldService -ne $null) {    
        try {
            Write-Host "Restarting service $($rollbackStore.stoppedOldService)"	
            StartService -Name $rollbackStore.stoppedOldService
        }
        catch {
            write-warning "Could not restart service $($rollbackStore.stoppedOldService): $($_.Exception.Message)"
        }
    }

    #
    # Remove the logs folder if we created it
    if ($rollbackStore.createdLogsDirectory -eq $true) {
        $logsPath = $rollbackStore.logsPath

        try {
            Write-Host "Rolling back logs folder creation"
            .\files.ps1 Remove-ItemForced -Path $logsPath
        }
        catch {
            write-warning "Could not delete logs folder ${logsPath}: $($_.Exception.Message)"
        } 
    }

    #
    # Remove the program folder if we created it
    if ($rollbackStore.createdAdminRoot -eq $true) {
        $adminRoot = $rollbackStore.adminRoot

        try {
            Write-Host "Rolling back installation folder creation"
            .\files.ps1 Remove-ItemForced -Path $adminRoot
        }
        catch {
            write-warning "Could not delete installation folder ${adminRoot}: $($_.Exception.Message)"
        } 
    }

    Write-Host "Finished rolling back."
}

function Install
{
    $rollbackStore = GetRollbackStore

    # Mark the location that the service will be copied to
    $adminRoot = Join-Path $Path $Version
    $rollbackStore.adminRoot = $adminRoot

    $destinationDirectory = Get-Item $adminRoot -ErrorAction SilentlyContinue
    if ($destinationDirectory -eq $null -and -not($DontCopy)){
    
        Try
        {
		    Write-Verbose "Creating installaton directory $adminRoot"
            new-item -ItemType Directory $adminRoot -Force -ErrorAction Stop | Out-Null
            $rollbackStore.createdAdminRoot = $true
        }
        Catch
        {   
            Write-Warning "Directory creation failed"
            throw "Could not create directory at $adminRoot"
        }
    }
    elseif (-not($destinationDirectory -eq $null) -and -not($destinationDirectory -is [System.IO.DirectoryInfo])) {
        throw "Install destination already exists and is not a directory"
    }

    $destinationDirectory = Get-Item $adminRoot
    $logsPath = [System.IO.Path]::Combine($destinationDirectory.Parent.FullName, 'logs')

    if (-not([System.IO.Directory]::Exists($logsPath))) {
        New-Item -ItemType Directory $logsPath -ErrorAction Stop | Out-Null
        $rollbackStore.logsPath = $logsPath
        $rollbackStore.createdLogsDirectory = $true
    }

    # Check for a previous installation at the installation path provided
    $previousInstallSettings = .\config.ps1 Get -Path $adminRoot

    if ($previousInstallSettings -ne $null) {
        throw "Cannot overwrite previous installation."
    }

    $svc = Get-Service "$ServiceName" -ErrorAction SilentlyContinue

    if ($svc -ne $null) {
        throw "Service with name: `'$ServiceName`' already exists"
    }

    if (!(.\net.ps1 IsAvailable -Port $Port)) {

		# Make sure we don't scan for available port if explicit port was specified
		if ($PortIsExplicit) {
			Write-Warning "Port: `'$Port`' already in use"
			throw "The port specified is not available"
		}

		try {
			$Port = .\net.ps1 GetAvailable -Port $Port
		}
		catch {
			throw $_
		}
    }

    if (-not($DontCopy)) {
        Write-Host "Copying files"
        try {
            Copy-Item -Recurse -Force (Join-Path $DistributablePath Microsoft.IIS.Administration) $adminRoot -ErrorAction Stop
            Copy-Item -Recurse -Force (Join-Path $DistributablePath setup) $adminRoot -ErrorAction Stop
        }
        catch {
            Write-Warning "Failed copying application files"
            throw
        }
    }

    .\config.ps1 Write-AppSettings -Port $Port -IncludeDefaultCors:$IncludeDefaultCors -AppSettingsPath (Join-Path $adminRoot "Microsoft.IIS.Administration\config\appsettings.json")

    # Need a cert to bind to the port the API is supposed to listen on
    $ensureOutput = .\ensure-cert.ps1 -Port $Port -CertHash $CertHash
    $cert = $ensureOutput.cert
    if ($ensureOutput.createdCertThumbprint) {
        $rollbackStore.createdCertThumbprint = $ensureOutput.createdCertThumbprint
    }
    if ($ensureOutput.preboundCertInfo) {
        $rollbackStore.preboundCertInfo = $ensureOutput.preboundCertInfo
    }
    if ($ensureOutput.newBoundCertPort) {
        $rollbackStore.newBoundCertPort = $ensureOutput.newBoundCertPort
    }

    # Create a hash table storing parameters of the installation that can be used when configuring or uninstalling
    # at a later date
    $installObject = @{
        InstallPath = $Path
        Port = $Port
        ServiceName = $ServiceName
        Version = $Version
        Installer = $([System.Environment]::UserDomainName + '/' + [System.Environment]::UserName)
		Date = date
		CertificateThumbprint = $cert.thumbprint
    }
    $rollbackStore.createdConfigPath = $adminRoot
    .\config.ps1 Write-Config -ConfigObject $installObject -Path $adminRoot
    
    # Construct an access rule that allows full control for Administrators
    .\security.ps1 Set-Acls -Path $adminRoot
    $rollbackStore.setAclsPath = $adminRoot

    # Register the Self Host exe as a service
    .\services.ps1 Create-IisAdministration -Name $ServiceName -Path $adminRoot
    $rollbackStore.createdService = $ServiceName
    StartService -Name "$ServiceName"

    $svc = Get-Service "$ServiceName" -ErrorAction SilentlyContinue
    if ($svc -eq $null) {
        throw "Could not install service"
    }

	sc.exe description "$ServiceName" "$(.\globals.ps1 SERVICE_DESCRIPTION)" | Out-Null

    .\cache.ps1 Destroy

    write-host Service installed, URI: https://localhost:$Port
}

try {
    Push-Location $(Get-ScriptDirectory)
    .\require.ps1 Is-Administrator

    CheckInstallParameters
    InstallationPreparationCheck

    Install
    exit 0
}
catch {
    rollback
    throw
}
finally {
    Pop-Location
}