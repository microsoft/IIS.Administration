# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(    
    # The path to place the published app
    [parameter(Position = 0)]
    [string]
    $OutputPath,
    
    # Flag for publishing with the Debug configuration
    [parameter()]
    [switch]
    $ConfigDebug,
    
    # Flag to skip restoring the projects
    # Using this flag reduces publish time but can only be used after an initial publish
    # Any change in dependencies will require a restore
    [parameter()]
    [switch]
    $SkipRestore,
    
    # Flag to automatically remove the content located at the output path
    [parameter()]
    [switch]
    $SkipPrompt,
    
    # Flag to enable signing targets if any
    [parameter()]
    [string]
    $SignType,
    
    # Identity of the certificate to use for signing, ex: Contoso
    [parameter()]
    [string]
    $SigningIdentity,
    
    # Identity of the certificate to use for signing setup scripts
    [parameter()]
    [string]
    $ScriptSigningIdentity
)

$applicationName = "Microsoft.IIS.Administration"

function Get-ScriptDirectory
{
    Split-Path $script:MyInvocation.MyCommand.Path
}

function Get-SolutionDirectory
{
    return $(Resolve-Path $(Join-Path $(Get-ScriptDirectory) "../..")).Path
}

function Get-VersionObject
{
    $versionPath = $(Resolve-Path $(Join-Path $(Get-ScriptDirectory) "..\setup\version.json")).Path
    if (-not(Test-Path $versionPath)) {
        throw "Could not find version."
    }
    $versionFile = Get-Item $versionPath -ErrorAction Stop
    $versionText = [System.IO.File]::ReadAllText($versionFile.FullName)
    return ConvertFrom-Json $versionText
}

function DeletePreExistingFiles($targetPath)
{
    $items = Get-ChildItem $targetPath

	if (-not($SkipPrompt)) {
		$confirmation = Read-Host "Remove the contents of $targetPath ? (y/n)"

		if($confirmation -ne 'y') {
			return
		}
	}

    foreach($item in $items) {    
        Remove-Item ($item.FullName) -Recurse
    }
}

if ([string]::IsNullOrEmpty($OutputPath)) {
    $OutputPath = Join-Path $(Get-ScriptDirectory) bin
}

$ProjectPath = $(Resolve-Path $(join-path $(Get-SolutionDirectory) src/Microsoft.IIS.Administration)).Path

$ProjectPathExists = Test-Path $ProjectPath

if(!$ProjectPathExists) {
    throw "Project could not be found"
}

if(-not(Test-Path $OutputPath)) {
	New-Item -type Directory $OutputPath -ErrorAction Stop | out-null
}

$configFolderPath = Join-Path $ProjectPath "config"

$configPathExists = Test-Path $configFolderPath

if(!$configPathExists) {
    throw "Config folder does not exist"
}

if ([string]::IsNullOrEmpty($ScriptSigningIdentity)) {
  $ScriptSigningIdentity = $SigningIdentity
}

if (-not([string]::IsNullOrEmpty($SignType))) {
	try {
		$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue

		if ($msbuild -eq $null) {
			throw "MsBuild.exe not on path"
		}
	}
	catch {
		Write-Warning $_.Exception.Message
		throw "Could not find msbuild"
	}

	if ([string]::IsNullOrEmpty($SigningIdentity)) {
	  throw "SigningIdentity required to produce a signed build"
	}
}

try {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue

	if ($dotnet -eq $null) {
		throw ".NET SDK not installed"
	}
}
catch {
    Write-Warning $_.Exception.Message
    throw "Could not find dotnet tools"
}

DeletePreExistingFiles $OutputPath

$applicationPath = Join-Path $OutputPath $applicationName

New-Item -type Directory $applicationPath -ErrorAction Stop | out-null

$configuration = "Release"
if($ConfigDebug) {
	$configuration = "Debug"
}

if (-not($SkipRestore)) {
    dotnet restore $(Get-SolutionDirectory)

	if ($LASTEXITCODE -ne 0) {
		throw "Restore failed"
	}
}

try{
	if ([string]::IsNullOrEmpty($SignType)) {
		dotnet publish $ProjectPath --framework netcoreapp1.0 --output $applicationPath  --configuration $configuration
	}
	else {
		msbuild $ProjectPath /t:publish /p:Configuration=$Configuration /p:PublishDir=$applicationPath /p:SignType=$SignType /p:SigningIdentity=$SigningIdentity
	}

	if ($LASTEXITCODE -ne 0) {
		throw "Publish failed"
	}
}
catch {
    Write-Warning $_.Exception.Message
    throw "Publish failed"
}

$outputConfigPath = Join-Path $applicationPath "config"
$outputConfigPathExists = Test-Path $outputConfigPath

if(!$outputConfigPathExists) {
    New-Item $outputConfigPath -Type directory | Out-Null
}

Copy-Item (Join-Path $configFolderPath "modules.default.json") (Join-Path $outputConfigPath "modules.json")  -Force -ErrorAction Stop;
Copy-Item (Join-Path $configFolderPath "api-keys.default.json") (Join-Path $outputConfigPath "api-keys.json")  -Force -ErrorAction Stop;
Copy-Item (Join-Path $configFolderPath "appsettings.default.json") (Join-Path $outputConfigPath "appsettings.json")  -Force -ErrorAction Stop;

# Dlls required for plugins reside in the plugins folder at dev time
$pluginFolder = Join-Path $ProjectPath "plugins"
$outputPluginsFolder = Join-Path $applicationPath "plugins"

if(!(Test-Path $outputPluginsFolder)) {
    New-Item $outputPluginsFolder -ItemType Directory | Out-Null
}

# Publish plugins to the plugins directory
try {
	$packagerPath = $(Resolve-Path $(join-path $(Get-SolutionDirectory) src/Packager/Bundle)).Path

    if (-not($SkipRestore)) {
        dotnet restore $packagerPath

        if ($LASTEXITCODE -ne 0) {
            throw "Plugin restore failed"
        }
    }

	if ([string]::IsNullOrEmpty($SignType)) {
		dotnet publish $packagerPath --framework netcoreapp1.0 --output $outputPluginsFolder  --configuration $configuration
	}
	else {
		msbuild $packagerPath /t:publish /p:Configuration=$Configuration /p:PublishDir=$outputPluginsFolder /p:SignType=$SignType /p:SigningIdentity=$SigningIdentity
	}

	if ($LASTEXITCODE -ne 0) {
		throw "Plugin build failed"
	}
}
catch {
	throw "Could not build plugins for publishing"
}

# Copy setup
if ([string]::IsNullOrEmpty($SignType)) {
    Copy-Item $(Join-Path $(Get-SolutionDirectory) scripts/setup) $OutputPath -Recurse -ErrorAction Stop
}
else {
    $SetupProjectPath = $(Join-Path $(Get-SolutionDirectory) scripts/Microsoft.IIS.Administration.PsSetup.csproj)

    if (-not($SkipRestore)) {
        dotnet restore $SetupProjectPath

        if ($LASTEXITCODE -ne 0) {
            throw "Setup script restore failed"
        }
    }

    msbuild $SetupProjectPath /p:SignType=$SignType /p:SigningIdentity=$ScriptSigningIdentity
    Copy-Item $(Join-Path $(Get-SolutionDirectory) scripts/bin/signed/setup) $OutputPath -Recurse -ErrorAction Stop

    if ($LASTEXITCODE -ne 0) {
        throw "Setup script build failed"
    }
}

# Copy thirdpartynotices.txt
Copy-Item $(Join-Path $(Get-SolutionDirectory) ThirdPartyNotices.txt) $OutputPath -ErrorAction Stop

# Remove all unnecessary files
if (-not($ConfigDebug)) {
	Get-ChildItem $OutputPath *.pdb -Recurse | Remove-Item -Force | Out-Null
}

# Remove non-windows runtime dlls
$runtimeDirs = Get-ChildItem -Recurse $OutputPath runtimes
foreach ($runtimeDir in $runtimeDirs) {
    Get-ChildItem $runtimeDir.FullName | Where-Object { $_.name -ne "win" } | ForEach-Object { Remove-Item $_.FullName -Force -Recurse }
}

# Remove non dlls from plugins
Get-ChildItem $outputPluginsFolder -File | where {-not($_.Name -match ".dll$")} | Remove-Item -Force
Remove-Item (Join-Path $outputPluginsFolder Bundle.dll) -Force

$mainDlls = Get-ChildItem $applicationPath *.dll
$mainDlls += $(Get-ChildItem -Recurse $applicationPath/runtimes/*.dll)
$pluginDlls = Get-ChildItem -Recurse $outputPluginsFolder *.dll

# Ensure no intersection between plugin dlls and application dlls
foreach ($pluginDll in $pluginDlls) {
	foreach ($mainDll in $mainDlls) {
		if ($mainDll.Name -eq $pluginDll.Name) {
			Remove-Item $pluginDll.FullName -Force | Out-Null
			break
		}
	}
}

$publishVersion = $(Get-VersionObject).version
Write-Host "Finished publishing $applicationName $publishVersion"