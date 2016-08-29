# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    
    [parameter(Mandatory=$true , Position=0)]
	[String]$RootDirectory,
    
    [parameter(Mandatory=$true , Position=1)]
    [String]$Port
)

$rootDir = Get-Item $RootDirectory -ErrorAction SilentlyContinue
if($rootDir -eq $null -or !($rootDir -is [System.IO.DirectoryInfo])){
	Write-Host "Usage: Create100Sites.ps1 -RootDirectory <absolue path for sites home> -Port <Port running the API>"
	exit
}

function Get-ApiHeadersObject() {

	$apiKey = .\utils.ps1 Generate-AccessToken -url "https://localhost:55539"

	Write-Host $apiKey

	$reqHeaders = @{}
	$reqHeaders."Access-Token" = "Bearer " + $apiKey;
	$reqHeaders."Accept" = "application/hal+json"

	return $reqHeaders;
}

Push-Location $rootDir

for($i = 1; $i -le 100 ; $i++){

	if(-not (Test-Path ("site$i")) ) {
		mkdir "site$i";

		Push-Location "site$i";
		for($j = 1; $j -le 5; $j++) {
			mkdir "application$j";

            Push-Location "application$j"
            echo "Site $i Application $j" | Out-File "index.html"
		    Pop-Location;
		}
		mkdir "wwwroot";
    
        Push-Location "wwwroot"
        echo "Site $i" | Out-File "index.html"
		Pop-Location;

		Pop-Location;
	}
}
Pop-Location



$apiHeaders = Get-ApiHeadersObject;

$jsonDir = $rootDir.fullname;
for($i = 1; $i -le 100; $i++) {

	$portNumber = 40000 + $i;
    $physicalPath = (Join-Path $jsonDir "site$i\wwwroot").Replace("\", "\\")
	 $newSite = @" 
	{ "name":"site$i", 
		"physical_path":"$physicalPath", 
		"bindings": 
		[ 
		{ 
			"ip_address": "*", 
			"port": "$portNumber", 
			"hostname": "", 
			"is_https": "false", 
			"certificate_hash": null, 
			"certificate_store_name": null 
		} 
		] 
	}
"@; 

	$response = Invoke-RestMethod "https://localhost:$Port/api/webserver/websites" -UseDefaultCredentials -Method Post -Body $newSite -ContentType "application/json" -Headers $apiHeaders;
	Write-Host $response | ConvertTo-Json;

    $physicalPath = (Join-Path $jsonDir "site$i\application1").Replace("\", "\\")
    $newApp = @"
    {
       "path": "app$i",
       "physical_path":"$physicalPath",
       "website": {
         "id": "$($response.id)"
       }
     }
"@;

	$response = Invoke-RestMethod "https://localhost:$Port/api/webserver/webapps" -UseDefaultCredentials -Method Post -Body $newApp -ContentType "application/json" -Headers $apiHeaders;
	Write-Host $response | ConvertTo-Json;
}

