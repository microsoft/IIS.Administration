# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(    
    [parameter()]
    [switch]
    $Interactive
)

function Get-ScriptDirectory
{
    Split-Path $script:MyInvocation.MyCommand.Path
}

function LinesToString($arr) {
    $s = "";

    foreach($line in $arr) {
        $s += $line + [System.Environment]::NewLine
    }

    return $s;
}

function Get-Lines($file, $lines) {
    
    if (-not($file -is [System.IO.FileInfo])) {
        throw "Expected file object"
    }

    $content = Get-Content $file.FullName | select -First $lines

    return $(LinesToString $content)
}

function Check-Licenses($fileExtension) {

    $baseDir = Get-ScriptDirectory

    $headerFilePath = Join-Path $baseDir "..\assets\license_header_$fileExtension.txt"
    $licenseHeaderFile = Get-Item $headerFilePath -ErrorAction Stop

    $header = Get-Content $headerFilePath
    $headerLength = $header.Length
    $header = LinesToString $header

    $files = Get-ChildItem $(Resolve-Path $(Join-Path $baseDir "..")).Path "*$fileExtension" -Recurse -File

    foreach ($file in $files) {
        $h = Get-Lines $file $headerLength

        if ($file.FullName.Contains("\obj\") -or
             $file.FullName.Contains("\lib\") -or
             $file.FullName.Contains("\PublishProfiles\")) {
            continue;
        }

        if ($h -ne $header) {
            Write-Warning "$($file.FullName) does not have the license header."

            if ($Interactive) {
	            $confirmation = Read-Host "Add license header to $($file.FullName) ? (y/n)"

	            if($confirmation -eq 'y') {
		            $content = Get-Content $($file.FullName)
                    $content = $header + $(LinesToString $content)
                    [System.IO.File]::WriteAllLines($file.FullName, $content)
                    Write-Host "Header added"
	            }
            }            
            $exitCode = 1
        }
    }
}

$exitCode = 0

Check-Licenses "cs"
Check-Licenses "js"
Check-Licenses "css"
Check-Licenses "cshtml"
Check-Licenses "ps1"

exit $exitCode

