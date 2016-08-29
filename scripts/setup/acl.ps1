# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("SetAdminAcl",
                 "Add-SelfRights")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path
)

function SetupAcl($_path) {

	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path cannot be null"
	}
    
    if (-not(Test-Path $_path)) {
        throw "Directory $_path does not exist."
    }


    # Construct an access rule that allows full control for Administrators
    $sid = [System.Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid
    # Construct an access rule that allows full control for Local System
	$localSystemSid = [System.Security.Principal.WellKnownSidType]::LocalSystemSid
    $idRef = New-Object System.Security.Principal.SecurityIdentifier($sid, $null)
    $localSystemIdRef = New-Object System.Security.Principal.SecurityIdentifier($localSystemSid, $null)
    $fullControl = [System.Security.AccessControl.FileSystemRights]::FullControl
    $allow = [System.Security.AccessControl.AccessControlType]::Allow
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$idRef, 
						$fullControl,
					    "ContainerInherit,ObjectInherit",
						[System.Security.AccessControl.PropagationFlags]::None,
						$allow)
    $localSystemRule = New-Object System.Security.AccessControl.FileSystemAccessRule($localSystemIdRef,
					   $fullControl,
					   "ContainerInherit,ObjectInherit",
					   [System.Security.AccessControl.PropagationFlags]::None,
					   $allow)

    $acl = New-Object System.Security.AccessControl.DirectorySecurity
    # Remove rule inheritance for acl
    $acl.SetAccessRuleProtection($true, $false)
    # Remove all existing access rules
    $acl.Access | %{$acl.RemoveAccessRule($_)}
    # Add the rule for Administrators
    $acl.AddAccessRule($rule)
    # Add the rule for Local System
    $acl.AddAccessRule($localSystemRule)
    # Update the folder to use the new ACL
    Set-Acl -Path $_path -AclObject $acl
}

function Add-SelfRights($_path) {
    $objUser = New-Object System.Security.Principal.NTAccount($env:USERNAME)
    $idRef = $objUser.Translate([System.Security.Principal.SecurityIdentifier])
    $fullControl = [System.Security.AccessControl.FileSystemRights]::FullControl
    $allow = [System.Security.AccessControl.AccessControlType]::Allow

    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$idRef, 
						$fullControl,
					    "ContainerInherit,ObjectInherit",
						[System.Security.AccessControl.PropagationFlags]::None,
						$allow)

    $a = Get-Acl $_path
    $a.AddAccessRule($rule)
    Set-Acl -Path $_path -AclObject $a
}

switch($Command)
{
    "SetAdminAcl"
    {
        return SetupAcl $Path
    }
    "Add-SelfRights"
    {
        return Add-SelfRights $Path
    }
    default
    {
        throw "Unknown command"
    }
}

