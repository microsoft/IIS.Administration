## This script is used to finalize build/dist directory for publish. It does the following
## * Dedupe depenciece from root dir and plugin dir
## * Move symbol files
## * Copy 3rd Party notice
param(
    [string]
    $solutionDir = [System.IO.Path]::Combine($PSScriptRoot, "..", ".."),

    [string]
    $manifestDir = [System.IO.Path]::Combine($solutionDir, ".builds")
)

function Move-SymbolsFiles {
    $symbolsDir = Join-Path $manifestDir symbols
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
    Copy-Item (Join-Path $solutionDir ThirdPartyNotices.txt) $manifestDir
}

Push-Location (Join-Path $manifestDir "Microsoft.IIS.Administration")
try {
    Move-SymbolsFiles
    Remove-DuplicateDlls
    Remove-PluginDependenciesFiles
    Remove-NonWindowsRuntime
    Copy-3rdPartyNotice
} finally {
    Pop-Location
}
