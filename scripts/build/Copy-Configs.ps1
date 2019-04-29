param(
    [Parameter(Position = 0)]
    $projectDir = [System.IO.Path]::Combine($PSScriptRoot, "..", "..", "src", "Microsoft.IIS.Administration")
)

Push-Location $projectDir
try {
    foreach ($configFile in (Get-ChildItem -Path "config\*.default.json")) {
        $target = $configFile.FullName -replace '.default.json$', '.json'
        if (!(Test-Path $target)) {
            Copy-Item $configFile.FullName $target
        }
    }
} finally {
    Pop-Location
}
