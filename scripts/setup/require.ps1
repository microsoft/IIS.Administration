# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Is-Administrator")]
    [string]
    $Command
)

function Is-Administrator {
    if (-not(([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))) {
        throw "User must be an Administrator to continue."
    }
}

switch($Command)
{
    "Is-Administrator"
    {
        Is-Administrator
    }
    default
    {
        throw "Unknown command"
    }
}