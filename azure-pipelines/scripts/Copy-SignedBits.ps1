[CmdletBinding()]
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
Write-Verbose "Locate build directory $buildDir"
try {
    foreach ($bitPath in Get-ChildItem -Recurse -Filter *.dll | Resolve-Path -Relative) {
        Write-Verbose "Locate built bit $bitPath"
        $builtBit = Join-Path $buildDir $bitPath
        $publishedBit =  Join-Path $publishDir $bitPath
        if (!(Test-Path $publishedBit)) {
            Write-Error "Cannot find published bit $publishedBit"
        }
        Write-Verbose "Copied built bit to $publishedBit"
        Copy-Item -Path $builtBit -Destination $publishedBit -Force
    }
} finally {
    Pop-Location
}
