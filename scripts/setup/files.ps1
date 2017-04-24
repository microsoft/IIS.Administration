# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Remove-ItemForced",
                 "Copy-FileForced",
                 "Write-FileForced")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [string]
    $Source,
    
    [parameter()]
    [string]
    $Destination,
    
    [parameter()]
    [string]
    $Content
)

# Removes an item, acquiring access if necessary. Forces recursive delete.
# Path: The path of the target directory/file.
function Remove-ItemForced($_path) {
    
    if ([System.String]::IsNullOrEmpty($_path)) {
        throw "Path required"
    }

    if (-not($(Test-Path $_path))) {
        return
    }

    $originalAcl = Get-Acl $_path

    .\security.ps1 Add-SelfRights -Path $_path -Recurse

    try {
        Remove-Item -Recurse -Force $_path -ErrorAction Stop
    }
    catch {
        .\security.ps1 Set-AclForced -Path $_path -Acl $originalAcl -Recurse
        throw
    }
}

# Copys a file from the source to the destination, acquiring access if necessary. Does not work with directories
# Source: The source file
# Destination: The location for the file. If a file exists, the file will be overwritten.
function Copy-FileForced($_source, $_destination) {
    
    if ([System.String]::IsNullOrEmpty($_source)) {
        throw "Source required"
    }
    
    if ([System.String]::IsNullOrEmpty($_destination)) {
        throw "Destination required"
    }

    if (-not($(Test-Path $_source))) {
        return
    }

    $source = Get-Item $_source

    if (-not($source -is [System.IO.FileInfo])) {
        throw "Source must be a file"
    }

    if ($(Test-Path $_destination)) {
        Remove-ItemForced($_destination)
    }

    $parentPath = [System.IO.Path]::GetDirectoryName($_destination)
    $originalACl = Get-Acl $parentPath

    $newAcl = Get-Acl $parentPath
    .\security.ps1 Add-SelfRights -Path $parentPath

    try {
        Copy-Item $_source $_destination -Recurse -Force
    }
    finally {
        .\security.ps1 Set-AclForced -Path $parentPath -Acl $originalACl
    }
}

# Writes the content for the file at the specified path
# Path: The location of the file
# Content: A string value to write to the file
function Write-FileForced($_path, $_content) {

    if ([System.String]::IsNullOrEmpty($_path)) {
        throw "Path required"
    }

    if (-not($(Test-Path $_path))) {
        New-FileForced $_path
    }

    $originalAcl = Get-Acl $_path
    .\security.ps1 Add-SelfRights -Path $_path

    try {
        [System.IO.File]::WriteAllText($_path, $_content)
    }
    finally {
        .\security.ps1 Set-AclForced -Path $_path -Acl $originalAcl
    }
}

function New-FileForced($_path) {

    if ([System.String]::IsNullOrEmpty($_path)) {
        throw "Path required"
    }

    if ($(Test-Path $_path)) {
        throw "File exists: $_path"
    }

    $parentPath = [System.IO.Path]::GetDirectoryName($_path)
    $originalACl = Get-Acl -Path $parentPath

    $newAcl = Get-Acl $parentPath
    Add-CreateFileEntry $newAcl ([System.Security.Principal.WindowsIdentity]::GetCurrent().User)
    .\security.ps1 Set-AclForced -Path $parentPath -Acl $newAcl

    try {
        [System.IO.File]::Create($_path).Dispose()
    }
    finally {
        .\security.ps1 Set-AclForced -Path $parentPath -Acl $originalACl
    }
}

function Add-CreateFileEntry($_acl, $_identity) {
    $create = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$_identity, 
						[System.Security.AccessControl.FileSystemRights]"Write,Read,Delete,CreateFiles,CreateDirectories",
					    [System.Security.AccessControl.InheritanceFlags]::None, 
						[System.Security.AccessControl.PropagationFlags]::None,
						[System.Security.AccessControl.AccessControlType]::Allow)

    
    $_acl.AddAccessRule($create)
    return $acl
}

switch($Command)
{
    "Remove-ItemForced"
    {
        Remove-ItemForced $Path
    }
    "Copy-FileForced"
    {
        Copy-FileForced $Source $Destination
    }
    "Write-FileForced"
    {
        Write-FileForced $Path $Content
    }
    default
    {
        throw "Unknown command"
    }
}