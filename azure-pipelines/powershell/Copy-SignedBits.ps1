param(
    [string]
    $buildDir,

    [string]
    $publishDir
)

$ErrorActionPreference = "Stop"

if (!$buildDir -or !$publishDir) {
    try {
        $projectRoot = git rev-parse --show-toplevel
        Write-Information "Detected project root ${projectRoot} with git"
    } catch {
        $projectRoot = [System.IO.Path]::Combine($PSScriptRoot, "..", "..")
        Write-Information "Detected project root ${projectRoot} from script location"
    }

    if (!$buildDir) {
        $buildDir = Join-Path $projectRoot ".build"
    }

    if (!$publishDir) {
        $publishDir = Join-Path $projectRoot "dist"
    }
}

Push-Location $buildDir
try {
    foreach ($bitPath in Get-ChildItem -Recurse -Filter Microsoft.IIS.*.dll | Resolve-Path -Relative) {
        $publishedBit =  Join-Path $publishDir $bitPath
        if (!(Test-Path $publishedBit)) {
            Write-Error "Cannot find published bit $publishedBit"
        }
        Copy-Item -Path $bitPath -Destination $publishedBit -Force
    }
} finally {
    Pop-Location
}
