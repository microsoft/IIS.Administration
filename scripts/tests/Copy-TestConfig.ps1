try {
    $projectRoot = git rev-parse --show-toplevel
} catch {
    Write-Warning "Error looking for project root $_, using script location instead"
    $projectRoot = [System.IO.Path]::Combine($PSScriptRoot, "..", "..")
}

$publishPath = Join-Path $projectRoot "dist"

Write-Host "Overwriting published config file with test configurations..."
$testConfig = [System.IO.Path]::Combine($projectRoot, "test", "appsettings.test.json")
$publishConfig = [System.IO.Path]::Combine($publishPath, "Microsoft.IIS.Administration", "config", "appsettings.json")
Copy-Item -Path $testconfig -Destination $publishConfig -Force
