[cmdletbinding()]
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
    foreach ($bitLocation in Get-ChildItem -Recurse -Filter Microsoft.IIS.*.dll) {
        $bitPath = Resolve-Path -LiteralPath $bitLocation.FullName -Relative
        Write-Verbose "Built bit ${bitPath} located"
        $publishedBit =  Join-Path $publishDir $bitPath
        if (!(Test-Path $publishedBit)) {
            Write-Verbose "Cannot find published bit $publishedBit"
            ## Plugin's dependecies were also in the build directory, ignoring
            if ($bitLocation.Directory.Name -eq "plugins" -and (Get-ChildItem $bitLocation.Directory.Parent.FullName $bitLocation.Name)) {
                Write-Verbose "Item is core library, skipping copy..."
                continue
            } else {
                Write-Error "${publishedBit} not found"
            }
        }
        Write-Verbose "Copying ${bitPath} to ${publishedBit}..."
        Copy-Item -Path $bitPath -Destination $publishedBit -Force
    }
} finally {
    Pop-Location
}
