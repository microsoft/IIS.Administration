# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

Param(
    [string]
    $Name,

    [string]
    $Password
)

# Find ccs test user
$localUsers = Get-WmiObject -Class Win32_UserAccount -Filter "LocalAccount = True"
$certUser = $localUsers | Where-Object {$_.Caption -match "$Name"}
$userExists = $certUser -ne $null
$Computer = [ADSI]"WinNT://$Env:COMPUTERNAME,Computer"

if ($userExists) {
    $Computer.Delete("User", $Name)
}

$ccsUser = $Computer.Create("User", $Name)
$ccsUser.SetPassword($Password)
$ccsUser.SetInfo()
$ccsUser.FullName = "Test account for IIS Administration API"
$ccsUser.SetInfo()
$ccsUser.UserFlags = 64 + 65536 # ADS_UF_PASSWD_CANT_CHANGE + ADS_UF_DONT_EXPIRE_PASSWD
$ccsUser.SetInfo()