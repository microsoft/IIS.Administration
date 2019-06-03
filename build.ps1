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

.PARAMETER publish
  build and publish the manifests in dist directory.
  Include this switch to build and test the project in a single step. However, its required that msbuild and nuget needs to be in the path when `publish` is set to true

.PARAMETER devSetup
  Ensure local machine is properly setup for build and test the repo

.PARAMETER install
  Install the built manifest for testing

.PARAMETER keepInstalledApp
  Do not uninstall the application after steps are run

.PARAMETER test
  Run the functional tests

.PARAMETER testPort
  The port to use for service

.PARAMETER installCheckRetryCount
.PARAMETER installCheckRetryPeriod
  When waiting for the service to be installed, the retry count/wait period

.PARAMETER buildType
  Build the binaries in debug or release mode, default: release

.PARAMETER appName
  Do not change: the name of the application
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
    $keepInstalledApp,

    [switch]
    $test,

    [int]
    $testPort = 44326,

    [int]
    $installCheckRetryCount = 20,

    [int]
    $installCheckRetryPeriod = 10,

    [ValidateSet('debug','release')]
    [string]
    $buildType = 'release',

    $appName = "Microsoft IIS Administration"
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
    & ([System.IO.Path]::Combine($scriptDir, "Configure-DevEnvironment.ps1")) -ConfigureTestEnvironment
}

function Publish() {
    dotnet restore
    msbuild /t:publish /p:Configuration=$buildType
}

function BuildSetupExe() {
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

function GetApp($appName) {
    try {
        return Get-WmiObject -Class Win32_Product | Where-Object { $_.Name -match $appName }
    } catch {
        Write-Verbose "Error getting application ${appName}: $($_.Exception.Message)"
    }
}

function InstallTestService() {
    & ([System.IO.Path]::Combine($projectRoot, "installer", "IISAdministrationBundle", "bin", "x64", "Release", "IISAdministrationSetup.exe")) /s /w
    # This is needed because /w didn't seem to wait until installer is completed
    while (($installCheckRetryCount -gt 0) -and !(GetApp $appName)) {
        $installCheckRetryCount--
        if ($installCheckRetryCount -gt 0) {
            Start-Sleep $installCheckRetryPeriod
        }
    }
    Write-Host "$appName installed"
    Write-Host "Sanity test:"
    $certStore = "Cert:\LocalMachine\My"
    $certName = & ([System.IO.Path]::Combine($scriptDir, "setup", "globals.ps1")) CERT_NAME
    $cert = Get-ChildItem $certStore | Where-Object { $_.FriendlyName -eq $certName }
    if (!$cert) {
        throw "Cert $certName not found in $certStore"
    }
    $pingEndpoint = "https://localhost:$testPort"
    Invoke-WebRequest -UseDefaultCredentials -UseBasicParsing $pingEndpoint
    Write-Host "Ping Successful"
}

function UninstallTestService() {
    $app = GetApp $appName
    $app.Uninstall() | Out-Null
    Write-Host "$appName uninstalled"
}

function CleanUp() {
    try {
        Stop-Service $serviceName
    } catch {
        if ($_.exception -and
            $_.exception -is [Microsoft.PowerShell.Commands.ServiceCommandException]) {
            Write-Host "$serviceName was not installed"
        } else {
            throw
        }
    }
    if (!$keepInstalledApp) {
        try {
            UninstallTestService
        } catch {
            Write-Warning $_
            Write-Warning "Failed to uninistall $serviceName"
        }
    }
}

function StartTest() {
    Write-Host "$(BuildHeader) Functional tests..."
    dotnet test ([System.IO.Path]::Combine($projectRoot, "test", "Microsoft.IIS.Administration.Tests", "Microsoft.IIS.Administration.Tests.csproj"))
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

########################################################### Main Script ##################################################################
$debug = $PSBoundParameters['debug']

if ($publish) {
    if (!(Where.exe msbuild)) {
        throw "msbuild command is required to publish application"
    }
    if (!(Where.exe nuget)) {
        throw "nuget command is required to build application installer"
    }
}

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

Write-Host "$(BuildHeader) Starting clean up..."
CleanUp

try {
    if ($devSetup) {
        Write-Host "$(BuildHeader) Dev setup..."
        DevEnvSetup
        Write-Host "$(BuildHeader) Ensure IIS Features..."
        EnsureIISFeatures
    }
    
    Write-Host "$(BuildHeader) Publishing..."
    if ($publish) {
        Publish
        & ([System.IO.Path]::Combine($scriptDir, "build", "Clean-BuildDir.ps1")) -manifestDir $publishPath
        if ($test) {
            & ([System.IO.Path]::Combine($scriptDir, "tests", "Copy-TestConfig.ps1"))
        }
        BuildSetupExe
    }

    if ($install) {
        Write-Host "$(BuildHeader) Installing service..."
        InstallTestService
    }

    if ($test) {
        Write-Host "$(BuildHeader) Starting test..."
        StartTest
    }
} catch {
    throw
} finally {
    Write-Host "$(BuildHeader) Final clean up..."
    CleanUp
}

Write-Host "$(BuildHeader) done..."
