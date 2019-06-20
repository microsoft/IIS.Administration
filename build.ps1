#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Entry point for building/testing the project, common usage:

  .\build.ps1 -devSetup -publish -install -test -verbose
    What the command does
        * Ensure local machine is properly setup for build and test the repo
        * Build and publish application in `dist` directory
        * Build and run installer
        * Run functional tests


  .\build.ps1 -devSetup -test -testRoot "C:\inetpub" -verbose
   Run test on a machine with IIS Administration API already installed

.PARAMETER publish
  build and publish the manifests in dist directory.
  Include this switch to build and test the project in a single step. However, its required that msbuild and nuget needs to be in the path when `publish` is set to true

.PARAMETER devSetup
  Ensure local machine is properly setup for build and test the repo

.PARAMETER install
  Install the built manifest for testing

.PARAMETER keep
  Do not uninstall the application after steps are run

.PARAMETER test
.PARAMETER testFilter
  Run the functional tests, use testFilter to filter to tests to be performed

.PARAMETER testPort
  The port to use for service

.PARAMETER testRoot
  The root directory for tests to create web sites. Note that the directory must be granted both read and write permission via IIS Administration API's config file

.PARAMETER pingRetryCount
.PARAMETER pingRetryPeriod
  When waiting for the service to come up, these properties defines the fequency and number of time to retry pinging the endpoint

.PARAMETER buildType
  Build the binaries in debug or release mode, default: release

.PARAMETER appName
  Do not change: the name of the application

.PARAMETER installedCertName
  Do not change: the name of the self host certed installed
#>
[CmdletBinding()]
param(
    [switch]
    $publish,

    [switch]
    $devSetup,

    [switch]
    $install,

    [switch]
    $keep,

    [switch]
    $test,

    [string]
    $testFilter,

    [int]
    $testPort = 55539,

    [string]
    $testRoot,

    [int]
    $pingRetryCount = 20,

    [int]
    $pingRetryPeriod = 10,

    [ValidateSet('debug','release')]
    [string]
    $buildType = 'release',

    [string]
    $appName = "Microsoft IIS Administration",

    [string]
    $installedCertName = "Microsoft IIS Administration Server Certificate"
)

$ErrorActionPreference = "Stop"

function BuildHeader {
    "[Build] $(Get-Date -Format yyyyMMddTHHmmssffff):"
}

function ForceResolvePath($path) {
    $path = Resolve-Path $path -ErrorAction SilentlyContinue -ErrorVariable err
    if (-not($path)) {
        $path = $err[0].TargetObject
    }
    return $path
}

function DevEnvSetup() {
    $configArgs = @{}
    $configArgs.ConfigureTestEnvironment = $true
    if ($testPort) {
        $configArgs.TestPort = $testPort
    }
    if ($testRoot) {
        Write-Verbose "Test port $testPort"
        $configArgs.TestRoot = $testRoot
    }
    & ([System.IO.Path]::Combine($scriptDir, "Configure-DevEnvironment.ps1")) @configArgs
}

function Publish() {
    if (!(Where.exe msbuild)) {
        throw "msbuild command is required for publish option"
    }
    msbuild /t:publish /p:Configuration=$buildType
}

function BuildSetupExe() {
    if (!(Where.exe msbuild)) {
        throw "msbuild command is required to build installer"
    }
    if (!(Where.exe nuget)) {
        throw "nuget command is required to build installer"
    }
    Push-Location installer
    try {
        nuget restore
        msbuild /p:Configuration=$buildType
    } finally {
        Pop-Location
    }
}

function EnsureIISFeatures() {
    Get-WindowsOptionalFeature -Online `
        | Where-Object {$_.FeatureName -match "IIS-" -and $_.State -eq [Microsoft.Dism.Commands.FeatureState]::Disabled} `
        | ForEach-Object {Enable-WindowsOptionalFeature -Online -FeatureName $_.FeatureName}
}

function InstallTestService() {
    Start-Process CMD -NoNewWindow -Wait @("/C", $script:installerLocation, "-q")

    ## Working around a windows group policy issue
    ## Installer just added the current user to "IIS Adminstration API Owners" group and the group policy may not have been updated without re-logon
    $roles = @('administrators', 'owners')
    $userJson = (ConvertTo-Json $(whoami)).Trim('"')
    foreach ($role in $roles) {
        $queryAddUser = '.security.users.' + $role + ' |= . + [\"' + $userJson + '\"]'
        Write-Verbose "Running query $queryAddUser to config file"
        & ([System.IO.Path]::Combine($scriptDir, "Edit-AppSettings.ps1")) -quiet -wait -query $queryAddUser
    }
    $script:installed = $true
}

function UninstallTestService() {
    Start-Process CMD -NoNewWindow -Wait @("/C", $script:installerLocation, "-q", "-uninstall")
    Write-Verbose "Uninstalled $appName"
}

function CleanUp() {
    if (!$script:installed -or $keep) {
        Write-Verbose "Skipping clean up"
    } else {
        try {
            Write-Verbose "$serviceName is being stopped"
            Stop-Service $serviceName
            Write-Verbose "$serviceName stopped"
        } catch {
            Write-Warning "Exception while trying to stop service $($_.Exception.Message)"
            if ($_.Exception -and
                $_.Exception -is [Microsoft.PowerShell.Commands.ServiceCommandException]) {
                Write-Host "$serviceName was not installed"
            }
        }

        try {
            Write-Verbose "$serviceName is being uninstalled"
            UninstallTestService
            Write-Verbose "$serviceName is uninstalled"
        } catch {
            Write-Warning "Failed to uninistall ${serviceName}: $($_.Exception.Message)"
        }
    }
}

function StartTest() {
    Write-Host "$(BuildHeader) Functional tests..."
    $testProj = [System.IO.Path]::Combine($projectRoot, "test", "Microsoft.IIS.Administration.Tests", "Microsoft.IIS.Administration.Tests.csproj")

    if ($testFilter) {
        dotnet test $testProj --filter $testFilter
    } else {
        dotnet test $testProj
    }
}

function VerifyPath($path) {
    if (!(Test-Path $path)) {
        Write-Path "$path does not exist"
        return $false
    }
    return $true
}

function VerifyPrecondition() {
    if (!(VerifyPath [System.IO.Path]::Combine($projectRoot, "test", "appsettings.test.json")) `
        -or !(VerifyPath [System.IO.Path]::Combine($projectRoot, "test", "Microsoft.IIS.Administration.Tests", "test.config.json.template"))) {
        throw "Test configurations do no exist, run .\scripts\Configure-DevEnvironment.ps1 -ConfigureTestEnvironment"
    }
}

function GetGlobalVariable($name) {
    & ([System.IO.Path]::Combine($scriptDir, "setup", "globals.ps1")) $name
}

function SanityTest() {
    Write-Host "Sanity tests..."
    if ($PSBoundParameters['Verbose']) {
        ListCerts
    }
    TouchUrl "https://localhost:${testPort}"
    TouchUrl "https://localhost:${testPort}/security/tokens"
}

function TouchUrl($url) {
    try {
        Invoke-WebRequest -UseDefaultCredentials $url
    } catch {
        Write-Host $_
        Write-Host (ConvertTo-Json $_.Exception)
        throw
    }
}

function ListCerts() {
    Write-Verbose "Listing from cert:LocalMachine\My"
    Get-ChildItem cert:LocalMachine\My | Where-Object { $_.FriendlyName -eq $installedCertName }
    Write-Verbose "Listing from cert:LocalMachine\Root"
    Get-ChildItem cert:LocalMachine\Root | Where-Object { $_.FriendlyName -eq $installedCertName }
    Write-Verbose "Done listing certs"
}

########################################################### Main Script ##################################################################
$debug = $PSBoundParameters['debug']
$script:installed = $false
try {
    $projectRoot = git rev-parse --show-toplevel
} catch {
    Write-Warning "Error looking for project root $_, using script location instead"
    $projectRoot = $PSScriptRoot
}
$scriptDir = Join-Path $projectRoot "scripts"
# publish script only takes full path
$publishPath = Join-Path $projectRoot "dist"
$serviceName = GetGlobalVariable DEFAULT_SERVICE_NAME
if ($testFilter) {
    $test = $true
}

Write-Host "$(BuildHeader) Starting clean up..."
CleanUp

try {
    if ($devSetup) {
        Write-Host "$(BuildHeader) Dev setup..."
        DevEnvSetup
        Write-Host "$(BuildHeader) Ensure IIS Features..."
        EnsureIISFeatures
    }

    if ($publish) {
        Write-Host "$(BuildHeader) Publishing..."
        dotnet restore
        Publish
        & ([System.IO.Path]::Combine($scriptDir, "build", "Clean-BuildDir.ps1")) -manifestDir $publishPath
        if ($test) {
            & ([System.IO.Path]::Combine($scriptDir, "tests", "Copy-TestConfig.ps1"))
        }
    }

    if ($install) {
        # Note: assume 64 bits and release built
        $script:installerLocation = [System.IO.Path]::Combine($projectRoot, "installer", "IISAdministrationBundle", "bin", "x64", "Release", "IISAdministrationSetup.exe")
        if (!$publish -and (Test-Path $script:installerLocation)) {
            Write-Host "Skipping building setup exe because it exists..."
        } else {
            BuildSetupExe
        }
        Write-Host "$(BuildHeader) Installing service..."
        InstallTestService
        if ($debug) {
            $proceed = Read-Host "$(BuildHeader) Pausing for debug, continue? (Y/n)..."
            if ($proceed -NotLike "y*") {
                Write-Host "$(BuildHeader) Aborting..."
                Exit 1
            }
        }
    }

    if ($test) {
        Write-Host "$(BuildHeader) Starting test..."
        SanityTest
        Write-Host "$(BuildHeader) Starting functional test..."
        StartTest
    }
} catch {
    throw
} finally {
    Write-Host "$(BuildHeader) Final clean up..."
    CleanUp
}

Write-Host "$(BuildHeader) done..."
