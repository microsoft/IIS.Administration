## This is a workaround to issue https://github.com/dotnet/sdk/issues/3236
## Published directory is what we need to publish but all the signed bit are in build directory
## We will copy over the signed bits
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
        $buildDir = Join-Path $projectRoot ".builds"
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
        if (Test-Path $publishedBit) {
            Copy-Item -Path $builtBit -Destination $publishedBit -Force
            Write-Verbose "Copied built bit to $publishedBit"
        } else {
            Write-Warning "Cannot find published bit $publishedBit"
        }
    }
} finally {
    Pop-Location
}
