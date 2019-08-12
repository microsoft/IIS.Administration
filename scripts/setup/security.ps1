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
                 "Add-SelfRights",
                 "Set-AclForced",
                 "Add-FullControl",
                 "EnsureLocalGroupMember")]

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
    $Path,
    
    [parameter()]
    [System.Security.AccessControl.FileSystemSecurity]
    $Acl,
    
    [parameter()]
    [System.Object]
    $Identity,
    
    [parameter()]
    [switch]
    $Recurse
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
            Write-Warning $_.Exception.Message
            #COM Exception if group doesn't exit
        }
    }
    else {
        $group = Get-LocalGroup -Name $groupName -ErrorAction SilentlyContinue
    }

    if ($group) {
        Write-Verbose "Group $groupName exists."
    } else {
        Write-Verbose "Group $groupName does not exist"
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
function CreateLocalGroup($_name, $desc, $skipIfExists = $false) {

	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name cannot be null"
	}

    $group = GetLocalGroup $_name;

    if($group) {
        if ($skipIfExists) {
            Write-Verbose "Group $_name already exists, returning..."
            return $group
        }
        throw "Group $_name already exists"
    }

    if (-not($(GroupCommandletsAvailable))) {
        $localAd = GetLocalAd

        $group = $localAd.Children.Add($_name, 'group')
        $group.Properties["Description"].Value = $desc
    
        $group.CommitChanges() | Out-Null
    }
    else {
        ## https://github.com/microsoft/IIS.Administration/issues/275
        ## Either
        ## 1. Something was creating the local group between line 141 and here
        ## 2. Something created the local group but the group policy is not updated
        ## causing $group object being null when checked, but New-LocalGroup actually failed
        try {
            $group = New-LocalGroup -Name $_name
        } catch {
            Write-Warning "New-LocalGroup -Name $_name threw exception $_, ignoring because group name would be used to add local group member"
        }
        net localgroup $_name /comment:$desc | Out-Null
    }

    if (!$group) {
        Write-Warning "CreateLocalGroup is returnning null, the group might not exist."
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
            return $localAd.Children.Remove($g.Path)
        }
        else {
            Remove-LocalGroup -Name $_name
            return $true
        }
    }
}

# Returns a representation of the current user for use in group manipulation
function CurrentAdUser {
    [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
}

# Adds a user to a local group.
# AdPath: the representation of the current user. Provided by CurrentAdUser.
# Group: The group to add the user to.
function AddUserToGroup($userPath, $_group, $groupName) {

	if ([System.String]::IsNullOrEmpty($userPath)) {
		throw "User path cannot be null"
	}

    if (-not($(GroupCommandletsAvailable))) {
        if (!$_group) {
            throw "Group cannot be null"
        }
        $userPath = 'WinNT://' + $userPath.Replace("\", "/")

        try {
            $_group.Invoke('Add', @($userPath))
        }
        catch {
            # HRESULT -2147023518
            # The specified account name is already a member of the group.
            if($_.Exception.InnerException -or ($_.Exception.InnerException.HResult -ne -2147023518 -and $_.Exception.InnerException.ErrorCode -ne -2147023518)) {
                throw $_.Exception
            }
        }
    }
    else {
        $existingMember = Get-LocalGroupMember -Group $groupName | Where {$_.Name -eq $userPath}
        if (!$existingMember) {
            Write-Verbose "Adding member $userPath to group $groupName"
            Add-LocalGroupMember -Name $($groupName) -Member $($userPath)
        } else {
            Write-Verbose "Member $userPath already exists in $groupName"
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

    Add-FullControl $configPath $system $true
}

# Gives full control of the directory at the specified path to the caller.
# Path: The path of the target directory.
# Recurse: Flag for whether or not full control will be forced for children objects
function Add-SelfRights($_path, $_recurse) {
    $acl = Get-Acl $_path
    Add-FullControl $_path ([System.Security.Principal.WindowsIdentity]::GetCurrent().User) $_recurse
}

# Sets an ACL for a given path. Privileges are enabled to allow manipulating the ACL if necessary
# Path: The path of the file system object to set the ACL for
# Acl: The acl to set for the object
# Recurse: Flag for whether or not the ACL will be forced for children objects
function Set-AclForced($_path, $_acl, $_recurse) {
    try {
        Set-Acl -AclObject $_acl -Path $_path -ErrorAction Stop
    }
    catch {
        _Set-AclForced $_path $_acl $_recurse
    }
}

function _Set-AclForced($_path, $_acl, $_recurse) {
    .\acl-util.ps1 Enable-AclUtil
    $item = Get-Item $_path -ErrorAction Stop
    $previousOwner = $item.GetAccessControl().GetOwner([System.Security.Principal.SecurityIdentifier])
    $newOwner = $_acl.GetOwner([System.Security.Principal.SecurityIdentifier])
    $administrators = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::BuiltinAdministratorsSid, $null)

    $takeOwnerShip = [Microsoft.IIS.Administration.Setup.AclUtil]::HasPrivilege([Microsoft.IIS.Administration.Setup.AclUtil]::TAKE_OWNERSHIP_PRIVILEGE)
    $restore = [Microsoft.IIS.Administration.Setup.AclUtil]::HasPrivilege([Microsoft.IIS.Administration.Setup.AclUtil]::RESTORE_PRIVILEGE)
    [Microsoft.IIS.Administration.Setup.AclUtil]::SetPrivilege([Microsoft.IIS.Administration.Setup.AclUtil]::TAKE_OWNERSHIP_PRIVILEGE, $true)
    [Microsoft.IIS.Administration.Setup.AclUtil]::SetPrivilege([Microsoft.IIS.Administration.Setup.AclUtil]::RESTORE_PRIVILEGE, $true)

    try {
        # If the ACL is being set recursively, the current identity must be the owner of the child objects/containers
        if ($_recurse -and ($item -is [System.IO.DirectoryInfo])) {
            Get-ChildItem $item.FullName -Recurse | ForEach-Object {
                $tAcl = $_.GetAccessControl('owner')
                $tAcl.SetOwner($administrators)
                $_.SetAccessControl($tAcl)
            }
        }

        # Obtain ownership of target
        $acl = $item.GetAccessControl('owner')
        $acl.SetOwner($administrators)
        $item.SetAccessControl($acl)

        # Set ACL
        Set-Acl -AclObject $_acl -Path $_path

    }
    finally {
        
        # Restore any ownership taken
        try {
                
            # If the provided ACL did not set an owner, restore to previous
            if ($newOwner -eq $null) {
                $acl = $item.GetAccessControl('owner')
                $acl.SetOwner($previousOwner)
                $item.SetAccessControl($acl)
            }

            # If acl is being set recursively, restore ownership of children
            if ($_recurse -and ($item -is [System.IO.DirectoryInfo])) {
                Get-ChildItem $item.FullName -Recurse | ForEach-Object {
                    $tAcl = $_.GetAccessControl('owner')
                    $tAcl.SetOwner($previousOwner)
                    $_.SetAccessControl($tAcl)
                }
            }
        }
        catch {
            # Fail state: owner will be the Administrators group
            Write-Warning "Could not restore owner for $($item.fullname): $($_.Exception.Message)"
        }

        # Revert any token privileges adjusted
        [Microsoft.IIS.Administration.Setup.AclUtil]::SetPrivilege([Microsoft.IIS.Administration.Setup.AclUtil]::TAKE_OWNERSHIP_PRIVILEGE, $takeOwnerShip)
        [Microsoft.IIS.Administration.Setup.AclUtil]::SetPrivilege([Microsoft.IIS.Administration.Setup.AclUtil]::RESTORE_PRIVILEGE, $restore)
    }
}

# Adds a full control entry to an ACL
# Path: The item to add full control to
# Identity: The identity who will be granted full control
# Recurse: Flag for whether or not full control will be forced for children objects
function Add-FullControl($_path, $_identity, $_recurse) {

	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path cannot be null"
	}

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

    $account = $_identity.Translate([System.Security.Principal.NTAccount])

    foreach ($access in $acl.Access) {

        if ($access.FileSystemRights -eq [System.Security.AccessControl.FileSystemRights]::FullControl -and
            $access.AccessControlType -eq [System.Security.AccessControl.AccessControlType]::Allow -and
            $access.IdentityReference -eq $account) {

            return
        }
    }
    
    $acl.AddAccessRule($fullControl)

    Set-AclForced $_path $acl $_recurse
}

## ensure the member/group exists in the specified group
function EnsureLocalGroupMember($groupName, $description, $AdPath) {
    $group = CreateLocalGroup $groupName $description $true
    return AddUserToGroup $AdPath $group $groupName
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
    "EnsureLocalGroupMember"
    {
        return EnsureLocalGroupMember $Name $Description $AdPath
    }
    "RemoveLocalGroup"
    {
        return RemoveLocalGroup $Name
    }
    "CurrentAdUser"
    {
        CurrentAdUser
    }
    "AddUserToGroup"
    {
        return AddUserToGroup $AdPath $Group $Group.Name
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
        return Add-SelfRights $Path $Recurse
    }
    "Set-AclForced"
    {
        return Set-AclForced $Path $Acl $Recurse
    }
    "Add-FullControl"
    {
        return Add-FullControl $Path $Identity $Recurse
    }
    default
    {
        throw "Unknown command"
    }
}

