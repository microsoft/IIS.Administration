# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("ONECORE",
                 "IS_NANO",
                 "DEFAULT_ADMIN_ROOT_NAME",
                 "DEFAULT_INSTALL_PATH",
                 "DEFAULT_PORT",
                 "IIS_ADMINISTRATION_APP_ID",
                 "INSTALL_METHOD_KEY",
                 "INSTALL_METHOD_VALUE",
                 "DEFAULT_SERVICE_NAME",
                 "SERVICE_DESCRIPTION",
                 "CERT_NAME")]
    [string]
    $Command
)

$INSTALL_METHOD_KEY = "IIS_ADMIN_INSTALL_METHOD"

switch ($Command)
{
    "ONECORE"
    {
        return [System.Environment]::OSVersion.Version.Major -ge 10
    }
    "IS_NANO"
    {
        $EditionId = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion' -Name 'EditionID').EditionId

        return ($EditionId -eq "ServerStandardNano") -or
            ($EditionId -eq "ServerDataCenterNano") -or
            ($EditionId -eq "NanoServer") -or
            ($EditionId -eq "ServerTuva")
    }
    # Application directory name
    "DEFAULT_ADMIN_ROOT_NAME"
    {
        return "IIS Administration"
    }
    "DEFAULT_INSTALL_PATH"
    {
        return Join-Path $env:ProgramFiles $(.\globals.ps1 DEFAULT_ADMIN_ROOT_NAME)
    }
    "DEFAULT_PORT"
    {
        return 55539
    }
    # Application id for IIS Administration API
    "IIS_ADMINISTRATION_APP_ID"
    {
        return "{96486158-833e-4332-9432-18de8f5e66b4}"
    }
    # Key for retrieving the installation strategy used
    "INSTALL_METHOD_KEY"
    {
        return $INSTALL_METHOD_KEY
    }
    # Value of installation strategy
    "INSTALL_METHOD_VALUE"
    {
        $installMethod = Get-Variable -Name $INSTALL_METHOD_KEY -ErrorAction SilentlyContinue
        if ($installMethod -ne $null) {
            $installMethod = $installMethod.Value
        }
        return $installMethod
    }
    # Application service name
    "DEFAULT_SERVICE_NAME"
    {
        return "Microsoft IIS Administration"
    }
    "SERVICE_DESCRIPTION"
    {
        return "Management service for IIS."
    }
    "CERT_NAME"
    {
        return "Microsoft IIS Administration Server Certificate"
    }
    default
    {
        throw "Unknown command"
    }
}