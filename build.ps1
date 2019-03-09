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

    [switch]
    $enableIISFeatures,

    [int]
    $testPort = 44326
)

$ErrorActionPreference = "Stop"

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
}

function EnsureIISFeatures() {
    Get-WindowsOptionalFeature -Online `
        | Where-Object {$_.FeatureName -match "IIS-" -and $_.State -eq [Microsoft.Dism.Commands.FeatureState]::Disabled} `
        | ForEach-Object {Enable-WindowsOptionalFeature -Online -FeatureName $_.FeatureName}
}

function InstallTestService() {
    & ([System.IO.Path]::Combine($scriptDir, "setup", "setup.ps1")) Install -DistributablePath $publishPath -Path $installPath -Verbose -Port $testPort
    $adminGroup = & ([System.IO.Path]::Combine($scriptDir, "setup", "globals.ps1")) IIS_ADMIN_API_OWNERS
    $user = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    if (!(Get-LocalGroupMember -Group $adminGroup -Member $user -ErrorAction SilentlyContinue)) {
        Add-LocalGroupMember -Group $adminGroup -Member $user
    }
    $testConfigLocation = [System.IO.Path]::Combine($PSScriptRoot, "test", "appsettings.test.json")
    $configLocation = Get-ChildItem -Recurse $installPath appsettings.json
    if ($configLocation -is [Array]) {
        throw "Multiple config files detected in $installPath"
    }
    Copy-Item -Path $testConfigLocation -Destination $configLocation -Force
    Restart-Service $serviceName
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

function StartTest() {
    $pingEndpoint = "https://localhost:$testPort"
    try {
        Invoke-WebRequest $pingEndpoint | Out-Null
    } catch {
        Write-Error "Failed to ping test server $pingEndpoint, did you forget to start it manually?"
        Exit 1
    }
    ## do the real test
}

########################################################### Main Script ##################################################################

$scriptDir = Join-Path $PSScriptRoot "scripts"
# publish script only takes full path
$publishPath = ForceResolvePath "$publishPath"
$installPath = ForceResolvePath "$installPath"
$serviceName = & ([System.IO.Path]::Combine($scriptDir, "setup", "globals.ps1")) DEFAULT_SERVICE_NAME

Write-Host "[Build] Starting clean up..."
CleanUp

try {
    if ($devSetup) {
        Write-Host "[Build] Dev setup..."
        DevEnvSetup
        Write-Host "[Build] Ensure IIS Features..."
        EnsureIISFeatures
    }
    
    Write-Host "[Build] Publishing..."
    Publish
    
    if ($install) {
        Write-Host "[Build] Installing test service..."
        InstallTestService
    }
    
    if ($test) {
        Write-Host "[Build] Starting test..."
        StartTest
    }
} catch {
    throw
} finally {
    Write-Host "[Build] Final clean up..."
    CleanUp
}

Write-Host "[Build] done..."
