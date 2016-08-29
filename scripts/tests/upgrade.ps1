# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [string]
    $DistributablePath
)

function Get-ScriptDirectory {
    Split-Path $script:MyInvocation.MyCommand.Path
}

function Bump-Version($path) {
    $c = Get-Content $path -ErrorAction Stop
    $v = New-Object "System.Version" -ArgumentList $c
    $v = [System.Version]::new($v.Major, $v.Minor, $v.Build + 1)
    $v.ToString() | Out-File $path
}

if (-not(Test-Path (Join-Path $DistributablePath setup\setup.ps1))) {
    throw "Cannot find installation package"
}

$svc = Get-Service "Microsoft IIS Administration" -ErrorAction SilentlyContinue
if ($svc -ne $null) {
    throw "Cannot run test, service already installed."
}

Write-Host "Installing fresh"
Invoke-Expression "$DistributablePath\setup\setup.ps1 Install -Verbose -DistributablePath '$DistributablePath'" -ErrorAction Stop

Write-Host "Generating API Key"
$key = Invoke-Expression "$(Get-ScriptDirectory)\..\utils.ps1 Generate-AccessToken -url 'https://localhost:55539'" -ErrorAction Stop

Write-Host "Bumping version"
Bump-Version "$DistributablePath\Version.txt"

Write-Host "Upgrading"
Invoke-Expression "$DistributablePath\setup\setup.ps1 Install -Verbose -DistributablePath '$DistributablePath'" -ErrorAction Stop

Write-Host "Testing for successful upgrade using generated key"
$response = Invoke-WebRequest -Headers @{'Access-Token' = "Bearer $key"} -Uri "https://localhost:55539/api" -UseDefaultCredentials -Method Get -ErrorAction Stop
if ($response.StatusCode -ne 200) {
    throw $response
}

Write-Host "Uninstalling"
Invoke-Expression "$DistributablePath\setup\setup.ps1 Uninstall -Verbose" 