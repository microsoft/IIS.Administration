# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(    
    [parameter(Mandatory=$true , Position=0)]
    [String]$Port
)

function Get-ApiKey(){	
	$res = Invoke-WebRequest "https://localhost:$Port/apikeys" -UseDefaultCredentials -SessionVariable sess;
	$hTok = $res.headers."XSRF-TOKEN";

	$h = @{};
	$h."XSRF-TOKEN" = $htok;

	$res2 = Invoke-WebRequest "https://localhost:$Port/apikeys" -Headers $h -Method Put -UseDefaultCredentials -WebSession $sess;
	$jObj = ConvertFrom-Json $res2.content

	return $jObj.value;
}

function Get-ApiHeaders() {

	$apiKey = Get-ApiKey;

	$reqHeaders = @{}
	$reqHeaders."Access-Token" = "Bearer " + $apiKey;
	$reqHeaders."Accept" = "application/hal+json"

	return $reqHeaders;
}

$apiHeaders = Get-ApiHeaders;

$sitesResponse = Invoke-RestMethod "https://localhost:$Port/api/webserver/websites" -UseDefaultCredentials -Headers $apiHeaders;

for($i = 1; $i -le 100; $i++) { 
    foreach($site in $sitesResponse.websites){ 
        if($site.name.equals("site$i")){ 
            $id = $site.id; 
            $response = Invoke-WebRequest "https://localhost:$Port/api/webserver/websites/$id" -Method delete -UseDefaultCredentials -Headers $apiHeaders;
            Write-Host "Delete status: " $response.StatusCode;
        } 
    } 
}

