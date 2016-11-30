# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(    
    # The path to place the published app
    [parameter(Mandatory=$true , Position=0)]
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
    $SkipPrompt
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

function Bump-Version
{
    $v = Get-VersionObject
    $version = [System.Version]::New($v.version)
    $bumpedVersion = [System.Version]::New($version.Major, $version.Minor, $version.Build + 1)
    $v.version = $bumpedVersion.ToString()
    [System.IO.File]::WriteAllText($(Resolve-Path Join-Path $(Get-ScriptDirectory) "..\setup\version.json").Path, $(ConvertTo-Json $v))
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

function Extract-ZipArchive($archivePath, $outputPath) {
    if(-not(Test-Path $archivePath)) {
        throw "Archive not found at $archivePath"
    }
    if(-not(Test-Path $outputPath)) {
        New-Item -Type Directory $outputPath | Out-Null
    }

    $fullArchivePath = $(Get-Item $archivePath).FullName
    $fullOutputPath = $(Get-Item $outputPath).FullName

    $shell = new-object -com shell.application
    $zip = $shell.NameSpace($fullArchivePath)

    foreach($item in $zip.Items()) {
        $shell.NameSpace($fullOutputPath).copyhere($item)
    }
}

function DownloadAndUnzip($url, $outputPath) {

    if ($url -eq $null) {
        throw "Url is required";
    }

    if ($outputPath -eq $null) {
        throw "outputPath is required";
    }

    $fileName = [System.Guid]::NewGuid().ToString() + ".zip"
    
    Invoke-WebRequest -Uri $url -OutFile $fileName
    Extract-ZipArchive $fileName $outputPath
    Remove-Item -Force $fileName
}

function Get-IISAdministrationHost($destinationDirectory) {

    if ($destinationDirectory -eq $null) {
        throw "Destination directory is required";
    }

    if (-not(Test-Path $destinationDirectory)) {
        New-Item -type directory -Path $destinationDirectory | out-null
    }

    Push-Location $destinationDirectory
    try {
        $url = "https://www.nuget.org/api/v2/package/Microsoft.IIS.Host/1.0.0-rc1"
        $outputPath = "host"

        mkdir $outputPath | Out-Null
        cd $outputPath

        DownloadAndUnzip $url "temp"
        Get-ChildItem "temp" | %{
            if ($_.Name -eq "Win32" -or $_.Name -eq "OneCore") {
                Copy-Item -Recurse -Force $_.FullName .
            }
        }
        Remove-Item -Recurse -Force "temp"
        cd ..
    }
    finally {
        Pop-Location
    }
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

try {
    dotnet -v | Out-Null

	if ($LASTEXITCODE -ne 0) {
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
    dotnet publish $ProjectPath --framework netcoreapp1.0 --output $applicationPath  --configuration $configuration

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

copy (Join-Path $configFolderPath "modules.json") $outputConfigPath  -Force -ErrorAction Stop;

$defaultAppSettingsContent = [System.IO.File]::ReadAllText($(Join-Path $(Get-ScriptDirectory) "defaultAppSettings.json"))
$defaultAppSettingsContent | Out-File (Join-Path $outputConfigPath "appsettings.json")

$emptyApiKeysContent = [System.IO.File]::ReadAllText($(Join-Path $(Get-ScriptDirectory) "empty-api-keys.json"))
$emptyApiKeysContent | Out-File (Join-Path $outputConfigPath "api-keys.json")

# Dlls required for plugins reside in the plugins folder at dev time
$pluginFolder = Join-Path $ProjectPath "plugins"
$outputPluginsFolder = Join-Path $applicationPath "plugins"

if(!(Test-Path $outputPluginsFolder)) {
    New-Item $outputPluginsFolder -ItemType Directory | Out-Null
}

# Publish plugins to the plugins directory
try {
	$packagerPath = $(Resolve-Path $(join-path $(Get-SolutionDirectory) src/Packager)).Path

	dotnet publish $packagerPath -o $outputPluginsFolder --configuration $configuration

	if ($LASTEXITCODE -ne 0) {
		throw "Plugin build failed"
	}
}
catch {
	throw "Could not build plugins for publishing"
}

# Copy setup
Copy-Item $(Join-Path $(Get-SolutionDirectory) scripts/setup) $OutputPath -Recurse -ErrorAction Stop

# Copy thirdpartynotices.txt
Copy-Item $(Join-Path $(Get-SolutionDirectory) ThirdPartyNotices.txt) $OutputPath -ErrorAction Stop


# Place Admin Host
Get-IISAdministrationHost $OutputPath

# Copy applicationHost.config
Copy-Item $(Join-Path $(Get-SolutionDirectory) scripts/publish/applicationHost.config) $(Join-Path $OutputPath host) -Recurse -ErrorAction Stop

# Remove all unnecessary files
Get-ChildItem $OutputPath *.pdb -Recurse | Remove-Item -Force | Out-Null
Get-ChildItem -Recurse $OutputPath "*unix" | where {$_.Name -eq "unix"} | Remove-Item -Force -Recurse

# Remove non dlls from plugins
Get-ChildItem $outputPluginsFolder -File | where {-not($_.Name -match ".dll$")} | Remove-Item -Force

$mainDlls = Get-ChildItem $applicationPath *.dll
$pluginDlls = Get-ChildItem $outputPluginsFolder *.dll

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