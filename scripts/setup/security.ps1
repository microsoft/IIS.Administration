# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("AddUserToGroup",
                 "CreateLocalGroup",
                 "CurrentAdUser",
                 "GetLocalGroup",
                 "GroupEquals",
                 "RemoveLocalGroup",
                 "Set-Acls",
                 "Add-SelfRights")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Name,
    
    [parameter()]
    [string]
    $Description,
    
    [parameter()]
    [string]
    $AdPath,
    
    [parameter()]
    [System.Object]
    $Group,
    
    [parameter()]
    [string]
    $Path
)

# Nano Server does not support ADSI provider
# Nano Server has localgroup and localuser commands which can be used instead of ADSI provider

$TrustedInstallerSid = New-Object "System.Security.Principal.SecurityIdentifier" -ArgumentList "S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464"

# Function not available on Nano Server
# Retrieves the provider used to interact with Active Directory for the local machine.
function GetLocalAd {
    $server = "$env:COMPUTERNAME"
    return [ADSI]"WinNT://$server,computer"
}

function GroupCommandletsAvailable() {
    # For PS > 5.0
    $localGroupCommand = Get-Command "Get-LocalGroup"  -ErrorAction SilentlyContinue
    $newLocalGroupCommand = Get-Command "New-LocalGroup" -ErrorAction SilentlyContinue
    $removeLocalGroupCommand = Get-Command "Remove-LocalGroup" -ErrorAction SilentlyContinue
    $addLocalGroupMemberCommand = Get-Command "Add-LocalGroupMember" -ErrorAction SilentlyContinue

    return $($localGroupCommand -ne $null -and
                $newLocalGroupCommand -ne $null -and
                $removeLocalGroupCommand -ne $null -and
                $addLocalGroupMemberCommand -ne $null)
}

# Retrieve a local group given the group name
# Name: The name of the local group.
function GetLocalGroup($groupName) {
    $group = $null;

    if (-not($(GroupCommandletsAvailable))) {
	    if ([System.String]::IsNullOrEmpty($groupName)) {
		    throw "Name cannot be null"
	    }
    
        $localAd = GetLocalAd

        try {
            $group = $localAd.Children.Find($groupName, 'group')
        }
        catch {
            #COM Exception if group doesn't exit
        }
    }
    else {
        $group = Get-LocalGroup -Name $groupName -ErrorAction SilentlyContinue
    }

    return $group;
}

# Check if a group is equal to another group with the provided name and description.
# Group: The group to test for equality.
# Name: Used to test for equality, must be equal to the Group parameter's name property for a true result.
# Description: Used to test for equality, must be equal to the group paremeter's description property for a true result.
function GroupEquals($group, $_name, $desc) {
    
    $description = $null
    if (-not($(GroupCommandletsAvailable))) {
        # Using ADSI
        $description = $group.Properties["Description"].Value
    }
    else {
        # Using Local Group commands
        $description = $group.Description
    }

    return $group.Name -eq $_name -and $description -eq $desc
}

# Creates a local group with the specified name and description.
# Name: The name for the local group.
# Description: The description for the local group.
function CreateLocalGroup($_name, $desc) {

	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name cannot be null"
	}

    $group = GetLocalGroup $_name;

    if($group -ne $null) {
        throw "Group $_name already exists"
    }

    if (-not($(GroupCommandletsAvailable))) {
        $localAd = GetLocalAd

        $group = $localAd.Children.Add($_name, 'group')
        $group.Properties["Description"].Value = $desc
    
        $group.CommitChanges()
    }
    else {
        $group = New-LocalGroup -Name $_name
        net localgroup $_name /comment:$desc | Out-Null
    }

    return $group
}

# Deletes a local group by name.
# Name: The name of the local group to delete.
function RemoveLocalGroup($_name) {

	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name cannot be null"
	}

    $g = GetLocalGroup $_name

    if($g -ne $null) {
        if (-not($(GroupCommandletsAvailable))) {
            $localAd = GetLocalAd
            $localAd.Children.Remove($g.Path)
        }
        else {
            Remove-LocalGroup -Name $_name
        }
    }
}

# Returns a representation of the current user for use in group manipulation
function CurrentAdUser {
    return [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
}

# Adds a user to a local group.
# AdPath: the representation of the current user. Provided by CurrentAdUser.
# Group: The group to add the user to.
function AddUserToGroup($userPath, $_group) {

	if ([System.String]::IsNullOrEmpty($userPath)) {
		throw "User path cannot be null"
	}

	if ($_group -eq $null) {
		throw "Group cannot be null"
	}

    if (-not($(GroupCommandletsAvailable))) {
        $userPath = 'WinNT://' + $userPath.Replace("\", "/")

        try {
            $_group.Invoke('Add', @($userPath))
        }
        catch {
            # HRESULT -2147023518
            # The specified account name is already a member of the group.
            if($_.Exception.InnerException -eq $null -or ($_.Exception.InnerException.HResult -ne -2147023518 -and $_.Exception.InnerException.ErrorCode -ne -2147023518)) {
                throw $_.Exception
            }
        }
    }
    else {
        $existingMember = Get-LocalGroupMember -Group $_group.name | where {$_.Name -eq $userPath}

        if ($existingMember -eq $null) {
            Add-LocalGroupMember -Name $($_group.name) -Member $($userPath)
        }
    }
}

function Set-Acls($_path) {

	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path cannot be null"
	}
    
    if (-not(Test-Path $_path)) {
        throw "Directory $_path does not exist."
    }

    $dir = Get-Item -Path $_path
    $logsPath = [System.IO.Path]::Combine($dir.Parent.FullName, 'logs')
    $configPath = [System.IO.Path]::Combine($_path, 'Microsoft.IIS.Administration/config')

    $administrators = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid, $null)
    $system = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::LocalSystemSid, $null)
    $currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().User

    $currentUserRead = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$currentUser, 
						[System.Security.AccessControl.FileSystemRights]::ReadAndExecute,
					    [System.Security.AccessControl.InheritanceFlags]"ContainerInherit,ObjectInherit", 
						[System.Security.AccessControl.PropagationFlags]::None,
						[System.Security.AccessControl.AccessControlType]::Allow)
    
    $trustedInstallerFullControl = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$TrustedInstallerSid, 
						[System.Security.AccessControl.FileSystemRights]::FullControl,
					    [System.Security.AccessControl.InheritanceFlags]"ContainerInherit,ObjectInherit", 
						[System.Security.AccessControl.PropagationFlags]::None,
						[System.Security.AccessControl.AccessControlType]::Allow)
    
    $administratorsRead = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$administrators, 
						[System.Security.AccessControl.FileSystemRights]::ReadAndExecute,
					    [System.Security.AccessControl.InheritanceFlags]"ContainerInherit,ObjectInherit", 
						[System.Security.AccessControl.PropagationFlags]::None,
						[System.Security.AccessControl.AccessControlType]::Allow)
    
    $systemRead = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$system, 
						[System.Security.AccessControl.FileSystemRights]::ReadAndExecute,
					    [System.Security.AccessControl.InheritanceFlags]"ContainerInherit,ObjectInherit", 
						[System.Security.AccessControl.PropagationFlags]::None,
						[System.Security.AccessControl.AccessControlType]::Allow)
    
    $systemFullControl = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$system, 
						[System.Security.AccessControl.FileSystemRights]::FullControl,
					    [System.Security.AccessControl.InheritanceFlags]"ContainerInherit,ObjectInherit", 
						[System.Security.AccessControl.PropagationFlags]::None,
						[System.Security.AccessControl.AccessControlType]::Allow)

    # Set ACL for root folder (e.g. 1.0.39)    
    $acl = New-Object System.Security.AccessControl.DirectorySecurity
    # Remove rule inheritance for acl
    $acl.SetAccessRuleProtection($true, $false)
    # Remove all existing access rules
    $acl.Access | Foreach-Object { $acl.RemoveAccessRule($_) }
    $acl.AddAccessRule($currentUserRead)
    $acl.AddAccessRule($trustedInstallerFullControl)
    $acl.AddAccessRule($administratorsRead)
    $acl.AddAccessRule($systemRead)
    # Update the folder to use the new ACL
    Set-Acl -Path $_path -AclObject $acl

    # Set ACL For logs folder
    $acl = New-Object System.Security.AccessControl.DirectorySecurity
    # Remove rule inheritance for acl
    $acl.SetAccessRuleProtection($true, $false)
    # Remove all existing access rules
    $acl.Access | Foreach-Object { $acl.RemoveAccessRule($_) }
    $acl.AddAccessRule($currentUserRead)
    $acl.AddAccessRule($administratorsRead)
    $acl.AddAccessRule($trustedInstallerFullControl)
    $acl.AddAccessRule($systemFullControl)
    # Update the folder to use the new ACL
    Set-Acl -Path $logsPath -AclObject $acl

    Add-FullControl $system $configPath
}

function Add-FullControl($_identity, $_path) {

    if ([System.IO.File]::Exists($_path)) {
        $inherit = [System.Security.AccessControl.InheritanceFlags]::None
    }
    else {
        $inherit = [System.Security.AccessControl.InheritanceFlags]"ContainerInherit,ObjectInherit"
    }

    $fullControl = New-Object System.Security.AccessControl.FileSystemAccessRule(
						$_identity, 
						[System.Security.AccessControl.FileSystemRights]::FullControl,
					    $inherit, 
						[System.Security.AccessControl.PropagationFlags]::None,
						[System.Security.AccessControl.AccessControlType]::Allow)

    
    $acl = Get-Acl -Path $_path
    $acl.AddAccessRule($fullControl)
    Set-Acl -Path $_path -AclObject $acl
}

# Gives full control of the directory at the specified path to the caller.
# Path: The path of the target directory.
function Add-SelfRights($_path) {
    Add-FullControl ([System.Security.Principal.WindowsIdentity]::GetCurrent().User) $_path
}

switch($Command)
{
    "GetLocalGroup"
    {
        return GetLocalGroup $Name
    }
    "CreateLocalGroup"
    {
        return CreateLocalGroup $Name $Description
    }
    "RemoveLocalGroup"
    {
        return RemoveLocalGroup $Name
    }
    "CurrentAdUser"
    {
        return CurrentAdUser
    }
    "AddUserToGroup"
    {
        return AddUserToGroup $AdPath $Group
    }
    "GroupEquals"
    {
        return GroupEquals $Group $Name $Description
    }
    "Set-Acls"
    {
        return Set-Acls $Path
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

