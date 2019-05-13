## This script would need to manually called after msbuild publish beause there is not "PostPublish" task in msbuild
param(
    [string]
    $solutionDir = [System.IO.Path]::Combine($PSScriptRoot, "..", ".."),

    [string]
    $publishDir = [System.IO.Path]::Combine($solutionDir, "dist")
)

function Move-SymbolsFiles {
    $symbolsDir = Join-Path $publishDir symbols
    if (!(Test-Path $symbolsDir)) {
        mkdir $symbolsDir | Out-Null
    }
    Get-ChildItem -Path "*.pdb" -Recurse -File | ForEach-Object { Move-Item  $_.FullName $symbolsDir -Force }
}

function Remove-DuplicateDlls {
    $prefix = '.\plugins\'
    foreach ($pluginDll in (Get-ChildItem -Path "plugins" -Recurse -File | Resolve-Path -Relative)) {
        if (!$pluginDll.StartsWith($prefix)) {
            throw "Unexpected prefix path detected for path: ${pluginDll}"
        }
        $appDll = $pluginDll.Substring($prefix.Length)
        if (Test-Path $appDll) {
            Remove-Item $pluginDll -Force
        }
    }
}

function Remove-PluginDependenciesFiles {
    Remove-Item -Path '.\plugins\*.deps.json'
}

function Remove-NonWindowsRuntime {
    foreach ($runtime in Get-ChildItem . -Name "runtimes" -Directory -Recurse) {
        foreach ($os in ((Resolve-Path $runtime) | Get-ChildItem)) {
            if (!$os.Name.StartsWith('win')) {
                Remove-Item $os.FullName -Force -Recurse
            }
        }
    }
}

function Copy-3rdPartyNotice {
    Copy-Item (Join-Path $solutionDir ThirdPartyNotices.txt) $publishDir
}

Push-Location (Join-Path $publishDir "Microsoft.IIS.Administration")
try {
    Move-SymbolsFiles
    Remove-DuplicateDlls
    Remove-PluginDependenciesFiles
    Remove-NonWindowsRuntime
    Copy-3rdPartyNotice
} finally {
    Pop-Location
}

