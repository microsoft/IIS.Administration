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
    [bool]
    $SkipIisAdministrators,

    [parameter()]
    [switch]
    $DontCopy
)


$_rollbackStore = @{}

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
        Write-Verbose "Verifying IIS is enabled"
        $iisInstalled = .\dependencies.ps1 IisEnabled
        if (!$iisInstalled) {
            Write-Warning "IIS-WebServer not enabled"
			Write-Host "Enabling IIS"
			try {
                .\dependencies.ps1 EnableIis
			}
			catch {
				Write-Warning "Could not enable IIS"
				throw $_
			}
        }
        Write-Verbose "Ok"
        Write-Verbose "Verifying Windows Authentication is Enabled"
        $winAuthEnabled = .\dependencies.ps1 WinAuthEnabled
        if (!$winAuthEnabled) {
            Write-Warning "IIS-WindowsAuthentication not enabled"
			Write-Host "Enabling IIS Windows Authentication"
			try {
                .\dependencies.ps1 EnableWinAuth
			}
			catch {
				Write-Warning "Could not enable IIS Windows Authentication"
				throw $_
			}
        }
        Write-Verbose "Ok"
        Write-Verbose "Verifying IIS-HostableWebCore is Enabled"
        $hostableWebCoreEnabled = .\dependencies.ps1 HostableWebCoreEnabled
        if (!$hostableWebCoreEnabled) {
            Write-Warning "IIS-HostableWebCore not enabled"
			Write-Host "Enabling IIS Hostable Web Core"
			try {
				.\dependencies.ps1 EnableHostableWebCore
			}
			catch {
				Write-Warning "Could not enable IIS Hostable Web Core"
				throw $_
			}
        }
        Write-Verbose "Ok"
        # We require.NET 3.5 for JSON manipulation if it isn't available through built in powershell commands
        if ($(Get-Command "ConvertFrom-Json" -ErrorAction SilentlyContinue) -eq $null) {
            Write-Verbose ".NET 3.5 required for setup to continue"
            Write-Verbose "Verifying NetFx3 is Enabled"
            $netfx3Enabled = .\dependencies.ps1 NetFx3Enabled
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
        }
		Write-Verbose "Ok"
    }
    # Shared framework and ANCM
    .\require.ps1 DotNetServerHosting
    Write-Verbose "Ok"

	Write-Verbose "Checking if port `'$Port`' is available"
    $available = .\net.ps1 IsAvailable -Port $Port
    if ($available) {
        Write-Verbose Ok
    }
    else {
        Write-Verbose "Not available"
    } 

    if (!$SkipIisAdministrators) {
        Write-Verbose "Verifying that IIS Administrators group does not already exist"
        $group = .\security.ps1 GetLocalGroup -Name $(.\globals.ps1 IISAdministratorsGroupName)

        # It is okay if IIS Administrators group already exists if it is our group
        if ($group -ne $null -and (-not (.\security.ps1 GroupEquals -Group $group -Name $(.\globals.ps1 IISAdministratorsGroupName) -Description $(.\globals.ps1 IISAdministratorsDescription) ))) {
            throw "IIS Administrators group already exists."
        }  
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
            Write-Warning "Could not stop newly created service"
        }

        sc.exe delete "$($rollbackStore.createdService)" | Out-Null
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
            Write-Warning "Could not restore the $($name) service."
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
            Write-Warning "Could not roll back SSL binding on port $($rollbackStore.newBoundCertPort)"
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
            Write-Warning "Could not restore previous SSL binding"
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
            Write-Warning "Could not remove setup config"
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
            write-warning "Could not delete certificate that was created during installation."
        }
    }

    #
    # Remove IIS Administrators group if we created it
    if ($rollbackStore.iisAdministratorsGroup -ne $null) {
        Write-Host "Rolling back IIS Administrators group creation" 

        .\security.ps1 RemoveLocalGroup -Name $rollbackStore.iisAdministratorsGroup
    }

    #
    # Restart the existing service if we stopped it
    if ($rollbackStore.stoppedOldService -ne $null) {    
		
        try {
            Write-Host "Restarting service $($rollbackStore.stoppedOldService)"
            Start-Service $rollbackStore.stoppedOldService
        }
        catch {
            write-warning "Could not restart service $($rollbackStore.stoppedOldService)."
        } 
    }

    #
    # Remove the program folder if we created it
    if ($rollbackStore.createdAdminRoot -eq $true) {
        $adminRoot = $rollbackStore.adminRoot

        try {
            Write-Host "Rolling back installation folder creation"
            Remove-Item $adminRoot -Force -Recurse
        }
        catch {
            write-warning "Could not delete installation folder $adminRoot."
        } 
    }

    Write-Host "Finished rolling back."
}

function Install
{
    $rollbackStore = GetRollbackStore

    # Mark the location that the service will be copied too
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

    # Construct an access rule that allows full control for Administrators
    .\security.ps1 SetAdminAcl -Path $adminRoot

    if (-not($DontCopy)) {
        Write-Host "Copying files"
        try {
            Copy-Item -Recurse -Force (Join-Path $DistributablePath host) $adminRoot -ErrorAction Stop
            Copy-Item -Recurse -Force (Join-Path $DistributablePath Microsoft.IIS.Administration) $adminRoot -ErrorAction Stop
            Copy-Item -Recurse -Force (Join-Path $DistributablePath setup) $adminRoot -ErrorAction Stop
        }
        catch {
            Write-Warning "Failed copying application files"
            throw
        }
    }

    # applicationHost.config must be configured on install for proper loading of the API
    $appHostPath = Join-Path $adminRoot host\applicationHost.config

    # The application path needs to be logged in the applicationHost.config
    $appPath = Join-Path $adminRoot Microsoft.IIS.Administration

    # Configure applicationHost.config based on install parameters
    .\config.ps1 Write-AppHost -AppHostPath $appHostPath -ApplicationPath $appPath -Port $Port -Version $Version

    if (!$SkipIisAdministrators) {
        $group = .\security.ps1 GetLocalGroup -Name $(.\globals.ps1 IISAdministratorsGroupName)
    
        if ($group -eq $null) {
            $group = .\security.ps1 CreateLocalGroup -Name $(.\globals.ps1 IISAdministratorsGroupName) -Description $(.\globals.ps1 IISAdministratorsDescription)
            $rollbackStore.iisAdministratorsGroup = $group
        }

        # Add the user running the installer to the IIS Administrators group
        $currentUser = .\security.ps1 CurrentAdUser
        .\security.ps1 AddUserToGroup -AdPath $currentUser -Group $group
    }

    # Need a cert to bind to the port the API is supposed to listen on
    $cert = $null
    if (![String]::IsNullOrEmpty($CertHash)) {

        # User provided a certificate hash (thumbprint)
        # Retrieve the cert from the hash
        $cert = Get-Item "Cert:\LocalMachine\My\$CertHash" -ErrorAction SilentlyContinue

        if ($cert -eq $null) {
            throw "Could not find certificate with hash $CertHash in store: My"
        }
        Write-Verbose "Using certificate with thumbprint $CertHash"
    }
    else {

        $cert = .\cert.ps1 Get -Name $(.\globals.ps1 CERT_NAME)
        if ($cert -eq $null) {
            # No valid cert exists, we must create one to enable HTTPS

            Write-Verbose "Creating new IIS Administration Certificate"
            $cert = .\cert.ps1 New -Name $(.\globals.ps1 CERT_NAME)
            $rollbackStore.createdCertThumbprint = $cert.Thumbprint;

            Write-Verbose "Adding the certificate to trusted store"
            .\cert.ps1 AddToTrusted -Certificate $cert
        }
        else {            
            # There is already a Microsoft IIS Administration Certificate on the computer that we can use for the API
            Write-Verbose "Using pre-existing IIS Administration Certificate"
        }
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
    
    # Get the certificate currently bound on desired installation port if any
    $preboundCert = .\net.ps1 GetSslBindingInfo -Port $Port

    # If a certificate is bound we delete it to bind our cert
    if ( $preboundCert -ne $null) {
        $rollbackStore.preboundCertInfo = $preboundCert

        # Remove any preexisting HTTPS. binding on the specified port
        Write-Verbose "Deleting certificate from port $Port in HTTP.Sys"
        .\net.ps1 DeleteSslBinding -Port $Port | Out-Null
    }

    Write-Verbose "Binding Certificate to port $Port in HTTP.Sys"

    .\net.ps1 BindCert -Hash $cert.thumbprint -Port $Port -AppId $(.\globals.ps1 IIS_HWC_APP_ID)  | Out-Null
    $rollbackStore.newBoundCertPort = $Port

    $platform = "OneCore"
    if (!$(.\globals.ps1 ONECORE)) {
        $platform = "Win32"
    }

    # Register the Self Host exe as a service
    $svcExePath = Join-Path $adminRoot "host\$platform\x64\Microsoft.IIS.Host.exe"
    sc.exe create "$ServiceName" binpath= "$svcExePath -appHostConfig:\`"$appHostPath\`" -serviceName:\`"$ServiceName\`"" start= auto
    $rollbackStore.createdService = $ServiceName

	try {
		Start-Service "$ServiceName" -ErrorAction Stop
	}
    catch {
        throw "Could not start service"
    }

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