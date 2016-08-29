# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(    
    [parameter(Mandatory=$true , Position=0)]
    [string]
    $OutputPath,
    
    [parameter()]
    [switch]
    $ConfigDebug
)

$applicationName = "Microsoft.IIS.Administration"

$defaultAppSettingsContent = @"
{
  "host_id": "",
  "host_name": "IIS Administration API",
  "site_creation_root": null,
  "administrators": [
    "Administrators",
    "IIS Administrators",
  ],
  "logging": {
    "enabled": true,
    "path": null,
    "min_level": "error",
    "file_name": "log-{Date}.txt"
  },
  "auditing": {
    "enabled": true,
    "path": null,
    "file_name": "audit-{Date}.txt"
  },
  "cors": {
    "rules": [
      {
        "origin": "https://manage.iis.net",
        "allow": true
      },
      {
        "origin": "https://manage.iisqa.net",
        "allow": true
      }
    ]
  }
}
"@

function Get-ScriptDirectory
{
    Split-Path $script:MyInvocation.MyCommand.Path
}

function DeletePreExistingFiles($targetPath)
{
    $items = Get-ChildItem $targetPath

	$confirmation = Read-Host "Remove the contents of $targetPath ? (y/n)"

	if($confirmation -ne 'y') {
		return
	}

    foreach($item in $items) {    
        Remove-Item ($item.FullName) -Recurse
    }
}

function DownloadAndUnzip($url, $outputName) {

    if ($url -eq $null) {
        throw "Url is required";
    }

    if ($outputName -eq $null) {
        throw "outputName is required";
    }
    
    Invoke-WebRequest -Uri $url -OutFile $outputName
    $shell = new-object -com shell.application
    $cdir = $(Get-Item .).FullName
    $nameSpace = Join-Path $cdir $outputName
    $zip = $shell.NameSpace($nameSpace)

    foreach($item in $zip.Items()) {
        $shell.NameSpace($cdir).copyhere($item)
    }

    Remove-Item $outputName
}

function Get-IISAdministrationHost($destinationDirectory) {

    if ($destinationDirectory -eq $null) {
        throw "Destination directory is required";
    }

    if (-not(Test-Path $destinationDirectory)) {
        New-Item -type directory -Path $destinationDirectory | out-null
    }

    pushd $destinationDirectory

    $url = "http://gitlab/jimmyca/Microsoft.IIS.Administration.Host/repository/archive.zip?ref=master"
    $folderName = "host"
    $output = "host.zip"

    mkdir $folderName -Force | Out-Null
    cd $folderName

    mkdir temp -force | Out-Null
    pushd temp

    DownloadAndUnzip $url $output

    $wrapper = Get-ChildItem

    Get-ChildItem $wrapper | foreach {Copy-Item $_.FullName .. -Recurse -Force}

    popd

    rmdir -Recurse -Force temp

    popd
}

$ProjectPath = $(Resolve-Path $(join-path $(Get-ScriptDirectory) ../src/Microsoft.IIS.Administration)).Path

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

try {
	$packagerPath = $(Resolve-Path $(join-path $(Get-ScriptDirectory) ../src/Packager)).Path

	dotnet publish $packagerPath -o "$(Join-Path $ProjectPath plugins)" --configuration $configuration

	if ($LASTEXITCODE -ne 0) {
		throw "Plugin build failed"
	}
}
catch {
	throw "Could not build plugins for publishing"
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

$defaultAppSettingsContent | Out-File (Join-Path $outputConfigPath "appsettings.json")

# Dlls required for plugins reside in the plugins folder at dev time
$pluginFolder = Join-Path $ProjectPath "plugins"
$outputPluginsFolder = Join-Path $applicationPath "plugins"

if(!(Test-Path $outputPluginsFolder)) {
    New-Item $outputPluginsFolder -ItemType Directory | Out-Null
}

# Copy the plugin dlls into the plugins directory
# Only copying in assemblies that aren't already present
Get-ChildItem $pluginFolder  | Copy-Item -Destination $outputPluginsFolder -Recurse -Force

# Copy setup.ps1
Copy-Item $(Join-Path $(Get-ScriptDirectory) setup) $OutputPath -Recurse -ErrorAction Stop

# Place version
$project = ConvertFrom-Json (Get-Content (Join-Path $ProjectPath project.json) -Raw)
$Project.version | Out-File (Join-Path $OutputPath "Version.txt")

# Place Admin Host
Get-IISAdministrationHost $OutputPath

# Remove all unnecessary files
Get-ChildItem $OutputPath *.pdb -Recurse | Remove-Item -Force | Out-Null
 Get-ChildItem -Recurse $OutputPath "*unix" | where {$_.Name -eq "unix"} | Remove-Item -Force -Recurse

# Ensure no intersection between plugin dlls and application dlls
$mainDlls = Get-ChildItem $applicationPath *.dll
$pluginDlls = Get-ChildItem $outputPluginsFolder *.dll

foreach ($pluginDll in $pluginDlls) {
	foreach ($mainDll in $mainDlls) {
		if ($mainDll.Name -eq $pluginDll.Name) {
			Remove-Item $pluginDll.FullName -Force | Out-Null
			break
		}
	}
}