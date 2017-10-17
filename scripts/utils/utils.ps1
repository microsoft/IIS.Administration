# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


#Requires -RunAsAdministrator
#Requires -Version 4.0
Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Generate-AccessToken")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $url
)

function Usage {
    Write-Host "Commands:"
    Write-Host "`tGenerate-AccessToken"
}

function Generate-AccessToken-Usage {
    'Generate-AccessToken -url <apiUrl>'
    Write-Host "-url:`t The url of the api that the key should be generated for."
}

function Generate-AccessToken-ParameterCheck {
    if ([system.string]::IsNullOrEmpty($url)) {
        Generate-AccessToken-Usage
        Exit
    }
}

function Generate-AccessToken($apiUrl) {
    $res = Invoke-WebRequest "$apiUrl/security/api-keys" -UseBasicParsing -UseDefaultCredentials -SessionVariable sess;
    $hTok = $res.headers."XSRF-TOKEN";

    if ($hTok -is [array]) {
        $hTok = $hTok[0]
    }

    $h = @{};
    $h."XSRF-TOKEN" = $htok;

    $res2 = Invoke-WebRequest "$apiUrl/security/api-keys" -UseBasicParsing -Headers $h -Method Post -UseDefaultCredentials -ContentType "application/json" -WebSession $sess -Body '{"expires_on": ""}';

    $jObj = ConvertFrom-Json ([System.Text.Encoding]::UTF8.GetString($res2.content))

    return $jObj.access_token;
}

switch($Command)
{
    "Generate-AccessToken"
    {
        Generate-AccessToken-ParameterCheck
        $key =  Generate-AccessToken $url
        return $key
    }
    default
    {
        Usage
    }
}

