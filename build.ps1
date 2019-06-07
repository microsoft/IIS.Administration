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


  .\build.ps1 -devSetup -test -verbose
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
  Run the functional tests

.PARAMETER testPort
  The port to use for service

.PARAMETER pingRetryCount
.PARAMETER pingRetryPeriod
  When waiting for the service to come up, these properties defines the fequency and number of time to retry pinging the endpoint

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
    $keep,

    [switch]
    $test,

    [int]
    $testPort = 55539,

    [int]
    $pingRetryCount = 20,

    [int]
    $pingRetryPeriod = 10,

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
    & ([System.IO.Path]::Combine($scriptDir, "Configure-DevEnvironment.ps1")) -ConfigureTestEnvironment -TestPort $testPort
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

function EnsureTestService($hold) {
    Write-Host "$(BuildHeader) Sanity tests..."
    $pingEndpoint = "https://localhost:$testPort"
    $pingSucceeded = $false
    while (!$pingSucceeded -and ($pingRetryCount -ge 0)) {
        try {
            Invoke-WebRequest -UseDefaultCredentials -UseBasicParsing $pingEndpoint | Out-Null
            $pingSucceeded = $true
        } catch {
            Write-Verbose "Failed to ping with status $($_.Exception.Status): $($_.Exception.Message)"
            if ($_.Exception.Status -eq [System.Net.WebExceptionStatus]::ConnectFailure) {
                $pingRetryCount--;
                if ($pingRetryCount -ge 0) {
                    Write-Verbose "Wait $pingRetryPeriod seconds to retry, numbers of attempts left: ${pingRetryCount}..."
                    Start-Sleep $pingRetryPeriod
                }
            }
        }
    }

    if (!$pingSucceeded) {
        Write-Error "Failed to ping test server $pingEndpoint, did you forget to start it manually?"
        Exit 1
    }

    if ($hold) {
        Read-Host "Press enter to continue..."
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

Write-Host "$(BuildHeader) Starting clean up..."
CleanUp

try {
    if ($devSetup) {
        Write-Host "$(BuildHeader) Dev setup..."
        DevEnvSetup
        Write-Host "$(BuildHeader) Ensure IIS Features..."
        EnsureIISFeatures
    }

    dotnet restore
    if ($publish) {
        Write-Host "$(BuildHeader) Publishing..."
        Publish
        & ([System.IO.Path]::Combine($scriptDir, "build", "Clean-BuildDir.ps1")) -manifestDir $publishPath
        if ($test) {
            & ([System.IO.Path]::Combine($scriptDir, "tests", "Copy-TestConfig.ps1"))
        }
        BuildSetupExe
        $script:installerLocation = [System.IO.Path]::Combine($projectRoot, "installer", "IISAdministrationBundle", "bin", "x64", "Release", "IISAdministrationSetup.exe")
    }

    if ($install) {
        Write-Host "$(BuildHeader) Installing service..."
        InstallTestService
        Write-Host "$(BuildHeader) Starting service..."
        EnsureTestService (!$test)

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
        StartTest
    }
} catch {
    throw
} finally {
    Write-Host "$(BuildHeader) Final clean up..."
    CleanUp
}

Write-Host "$(BuildHeader) done..."
