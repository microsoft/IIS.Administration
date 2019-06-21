# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Param(
    [switch]
    $ConfigureTestEnvironment,

    [int]
    $TestPort = 55539,

    [string]
    $TestRoot,

    [switch]
    $CertSetup
)

#Requires -RunAsAdministrator
#Requires -Version 4.0

function ReplaceTemplate($path, $env) {
    $templateSuffix = ".template"
    if (!($path.EndsWith($templateSuffix))) {
        throw "Template file name $path must end with $templateSuffix"
    }
    $content = Get-Content -Path $path -Raw -Encoding UTF8
    foreach ($key in $env.Keys) {
        $content = $content -replace "%${key}%", $env[$key]
    }
    $outFile = $path.Substring(0, $path.Length - $templateSuffix.Length)
    Set-Content -Path $outFile -Value $content -Force
}

$scriptDir = Split-Path $script:MyInvocation.MyCommand.Path

try {
    Push-Location $scriptDir

    $solutionDir = (Resolve-Path (Join-Path $scriptDir "..")).Path

    Write-Host "Setting environment variables."

    # Check for existence of app configuration files
    # Create them from defaults if they don't exist
    $configDir = Join-Path $solutionDir "src/microsoft.iis.administration/config"

    if (-not(Test-Path $(Join-Path $configDir "appsettings.json"))) {
        Copy-Item $(Join-Path $configDir "appsettings.default.json") $(Join-Path $configDir "appsettings.json")

        Write-Host "appsettings.json created at $(Join-Path $configDir "appsettings.json")"
        Write-Host "Add users to the 'users' section to grant access to the application"
        Write-Host "Add ""manage.iis.net"" to the cors rule if you wish to manage IIS through browser"
        Write-Host "For more info about the security configuration visit https://docs.microsoft.com/en-us/iis-administration/configuration/appsettings.json#security"
    }

    if (-not(Test-Path $(Join-Path $configDir "modules.json"))) {
        Copy-Item $(Join-Path $configDir "modules.default.json") $(Join-Path $configDir "modules.json")
    }

    try {
        $solutionRoot = git rev-parse --show-toplevel
    } catch {
        Write-Warning "Unable to determine git root, using parent directory as solution root: $_"
        $solutionRoot = Join-Path $PSScriptRoot ".."
    }
    if (!$testRoot) {
        $testRoot = Join-Path $solutionRoot ".test"
    }
    if ($ConfigureTestEnvironment) {
        Write-Host "Configuring test environment"
        .\tests\Create-CcsInfrastructure.ps1 -TestRoot $testRoot

        $env = @{
            "iis_admin_test_dir" = ($TestRoot | ConvertTo-Json).Trim('"');
            "iis_admin_test_port" = $TestPort;
            "project_dir" = ($solutionRoot | ConvertTo-Json).Trim('"');
        }
        (Get-ChildItem Env:) | ForEach-Object { $env[$_.Name] = $_.Value }
        ReplaceTemplate ([System.IO.Path]::Combine($solutionRoot, "test", "appsettings.test.json.template")) $env
        ReplaceTemplate ([System.IO.Path]::Combine($solutionRoot, "test", "Microsoft.IIS.Administration.Tests", "test.config.json.template")) $env
    }

    if ($CertSetup) {
        Push-Location ([System.IO.Path]::Combine($solutionRoot, "scripts", "setup"))
        try {
            Write-Host "Installing ssl cert and binding"
            .\ensure-cert.ps1 -Port $TestPort
        } finally {
            Pop-Location
        }
    }
}
finally {
    Pop-Location
}
