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
    $LegacyConfigurations
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
            Write-Warning "Could not stop newly created service"
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
    # Remove the logs folder if we created it
    if ($rollbackStore.createdLogsDirectory -eq $true) {
        $logsPath = $rollbackStore.logsPath

        try {
            Write-Host "Rolling back logs folder creation"
            .\files.ps1 Remove-ItemForced -Path $logsPath
        }
        catch {
            write-warning "Could not delete logs folder $logsPath."
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
            write-warning "Could not delete installation folder $adminRoot."
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

    .\config.ps1 Write-AppSettings -Port $Port -LegacyConfigurations:$LegacyConfigurations -AppSettingsPath (Join-Path $adminRoot "Microsoft.IIS.Administration\config\appsettings.json")

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
        # Check for existing IIS Administration Certificate
        $cert = .\cert.ps1 Get-LatestIISAdminCertificate
        $certCreationName = $(.\globals.ps1 CERT_NAME)

        if ($cert -ne $null) {
            #
            # Check if existing cert has sufficient lifespan left (3+ months)
            $expirationDate = $cert.NotAfter
            $remainingLifetime = $expirationDate - [System.DateTime]::Now

            if ($remainingLifetime.TotalDays -lt $(.\globals.ps1 CERT_EXPIRATION_WINDOW)) {
                Write-Verbose "The IIS Administration Certificate will expire in less than $(.\globals.ps1 CERT_EXPIRATION_WINDOW) days"
                $certCreationName = $(.\globals.ps1 CERT_NAME) + " " + [System.DateTime]::Now.Year.ToString()
                $cert = $null
            }
        }

        if ($cert -eq $null) {
            # No valid cert exists, we must create one to enable HTTPS

            Write-Verbose "Creating new IIS Administration Certificate"
            $cert = .\cert.ps1 New -Name $certCreationName
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

    $useIisAdminCert = $true

    if ($preboundCert -ne $null) {

        # We will use the IIS Admin certificate only if the pre-existing binding was using it
        $useIisAdminCert = .\cert.ps1 Is-IISAdminCertificate -Thumbprint $preboundCert.CertificateHash
    }

    if ($useIisAdminCert) {

        # If a certificate is bound we delete it to bind our cert
        if ( $preboundCert -ne $null) {
            $rollbackStore.preboundCertInfo = $preboundCert

            # Remove any preexisting HTTPS. binding on the specified port
            Write-Verbose "Deleting certificate from port $Port in HTTP.Sys"
            .\net.ps1 DeleteSslBinding -Port $Port | Out-Null
        }

        Write-Verbose "Binding Certificate to port $Port in HTTP.Sys"

        .\net.ps1 BindCert -Hash $cert.thumbprint -Port $Port -AppId $(.\globals.ps1 IIS_ADMINISTRATION_APP_ID)  | Out-Null
        $rollbackStore.newBoundCertPort = $Port
    }

    # Construct an access rule that allows full control for Administrators
    .\security.ps1 Set-Acls -Path $adminRoot
    $rollbackStore.setAclsPath = $adminRoot

    # Register the Self Host exe as a service
    .\services.ps1 Create-IisAdministration -Name $ServiceName -Path $adminRoot
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