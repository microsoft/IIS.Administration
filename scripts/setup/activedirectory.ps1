# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("AddUserToGroup",
                 "CreateLocalGroup",
                 "CurrentAdUser",
                 "GetLocalGroup",
                 "GroupEquals",
                 "RemoveLocalGroup")]
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
    $Group
)

function GetLocalAd {
    $server = "$env:COMPUTERNAME"
    return [ADSI]"WinNT://$server,computer"
}

function GetLocalGroup($groupName) {

	if ([System.String]::IsNullOrEmpty($groupName)) {
		throw "Name cannot be null"
	}

    $localAd = GetLocalAd

    $group = $null;

    try {
        $group = $localAd.Children.Find($groupName, 'group')
    }
    catch {
        #COM Exception if group doesn't exit
    }

    return $group;
}

function GroupEquals($group, $_name, $desc) {
    return $group.Name -eq $_name -and $group.Properties["Description"].Value -eq $desc
}

function CreateLocalGroup($_name, $desc) {

	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name cannot be null"
	}

    $localAd = GetLocalAd

    $group = GetLocalGroup $_name;

    if($group -ne $null) {

        throw "Group $_name already exists"
    }
    
    $group = $localAd.Children.Add($_name, 'group')
    $group.Properties["Description"].Value = $desc
    
    $group.CommitChanges()

    return $group
}

function RemoveLocalGroup($_name) {

	if ([System.String]::IsNullOrEmpty($_name)) {
		throw "Name cannot be null"
	}

    $localAd = GetLocalAd

    $g = GetLocalGroup $_name

    if($g -ne $null) {
        $localAd.Children.Remove($g.Path)
    }
}

function CurrentAdUser {
    return 'WinNT://' + [System.Environment]::UserDomainName + '/' +  [System.Environment]::UserName
}

function AddUserToGroup($userPath, $_group) {

	if ([System.String]::IsNullOrEmpty($userPath)) {
		throw "User path cannot be null"
	}

	if ($_group -eq $null) {
		throw "Group cannot be null"
	}

    try {
        $_group.Invoke('Add', @($userPath))
    }
    catch {

        # HRESULT -2147023518
        # The specified account name is already a member of the group.
        if($_.Exception.InnerException -eq $null -or $_.Exception.InnerException.HResult -ne -2147023518) {
            throw $_.Exception
        }
    }
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
    default
    {
        throw "Unknown command"
    }
}

