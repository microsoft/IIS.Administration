# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true)]
	[String]$Uri,

    [parameter(Mandatory=$true)]
	[String]$RootDirectory,
    
    [parameter(Mandatory=$true)]
    [int]$StartPort,

    [parameter()]
    [int]$Number = 100
)

function Get-ApiHeadersObject() {

	$apiKey = .\utils.ps1 Generate-AccessToken -url $Uri

	Write-Host $apiKey

	$reqHeaders = @{}
	$reqHeaders."Access-Token" = "Bearer " + $apiKey;
	$reqHeaders."Accept" = "application/hal+json"

	return $reqHeaders;
}

$rootDir = Get-Item $RootDirectory -ErrorAction Stop

if (-not($rootDir -is [System.IO.DirectoryInfo])) {
  throw "RootDirectory must be an existing directory"
}

Push-Location $rootDir

for($i = 1; $i -le $Number ; $i++){

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

$jsonDir = $rootDir.FullName;
for($i = 1; $i -le $Number; $i++) {

	$portNumber = $StartPort + $i;
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
			"protocol": "http"
		  } 
		] 
	}
"@; 

	$response = Invoke-RestMethod "$Uri/api/webserver/websites" -UseDefaultCredentials -Method Post -Body $newSite -ContentType "application/json" -Headers $apiHeaders;
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

	$response = Invoke-RestMethod "$Uri/api/webserver/webapps" -UseDefaultCredentials -Method Post -Body $newApp -ContentType "application/json" -Headers $apiHeaders;
	Write-Host $response | ConvertTo-Json;
}

