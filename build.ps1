#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [switch]
    $devSetup,

    [switch]
    $install,

    [switch]
    $test,

    [string]
    $publishPath = (Join-Path $PSScriptRoot "dist"),

    [string]
    $installPath = (Join-Path $env:ProgramFiles "IIS Administration"),

    [int]
    $testPort = 44326
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
    & ([System.IO.Path]::Combine($scriptDir, "publish", "publish.ps1")) -OutputPath $publishPath -SkipPrompt
    if ($test) {
        Write-Host "$(BuildHeader) Overwriting published config file with test configurations..."
        $testConfig = [System.IO.Path]::Combine($projectRoot, "test", "appsettings.test.json")
        $publishConfig = [System.IO.Path]::Combine($publishPath, "Microsoft.IIS.Administration", "config", "appsettings.json")
        Copy-Item -Path $testconfig -Destination $publishConfig -Force
    }
}

function EnsureIISFeatures() {
    Get-WindowsOptionalFeature -Online `
        | Where-Object {$_.FeatureName -match "IIS-" -and $_.State -eq [Microsoft.Dism.Commands.FeatureState]::Disabled} `
        | ForEach-Object {Enable-WindowsOptionalFeature -Online -FeatureName $_.FeatureName}
}

function InstallTestService() {
    & ([System.IO.Path]::Combine($scriptDir, "setup", "setup.ps1")) Install -DistributablePath $publishPath -Path $installPath -Verbose -Port $testPort
}

function UninistallTestService() {
    & ([System.IO.Path]::Combine($scriptDir, "setup", "setup.ps1")) Uninstall -Path $installPath -ErrorAction SilentlyContinue | Out-Null
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
    try {
        UninistallTestService
    } catch {
        Write-Warning $_
        Write-Warning "Failed to uninistall $serviceName"
    }
}

function StartTestService($hold) {
    $group = GetGlobalVariable IIS_ADMIN_API_OWNERS
    $member = & ([System.IO.Path]::Combine($scriptDir, "setup", "security.ps1")) CurrentAdUser

    Write-Host "$(BuildHeader) Sanity tests..."
    $pingEndpoint = "https://localhost:$testPort"
    try {
        Invoke-WebRequest -UseDefaultCredentials -UseBasicParsing $pingEndpoint | Out-Null
    } catch {
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
$user = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name

try {
    $projectRoot = git rev-parse --show-toplevel
} catch {
    Write-Warning "Error looking for project root $_, using script location instead"
    $projectRoot = $PSScriptRoot
}
$scriptDir = Join-Path $projectRoot "scripts"
# publish script only takes full path
$publishPath = ForceResolvePath "$publishPath"
$installPath = ForceResolvePath "$installPath"
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
    Publish
    
    if ($install) {
        Write-Host "$(BuildHeader) Installing service..."
        InstallTestService
    }
    
    if ($test) {
        Write-Host "$(BuildHeader) Starting service..."
        StartTestService (!$test)

        if ($debug) {
            $proceed = Read-Host "$(BuildHeader) Pausing for debug, continue? (Y/n)..."
            if ($proceed -NotLike "y*") {
                Write-Host "$(BuildHeader) Aborting..."
                Exit 1
            }
        }

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
