# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Write",
                 "Read")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [System.Collections.Hashtable]
    $Obj
)

function WriteHashTable($o, $filePath) {
	$xml = [xml]""
	$xml.AppendChild($xml.CreateXmlDeclaration("1.0", "UTF-8", $null)) | Out-Null
	$root = $xml.CreateElement("root")

    if ($o -ne $null) {
	    foreach($key in $o.keys) {            
            $xElem = $xml.CreateElement($key)
            $xElem.InnerText = $o[$key]
            $root.AppendChild($xElem) | Out-Null
        }  
    }   
    $xml.AppendChild($root) | Out-Null

	$xml.Save("$filePath") | Out-Null
}

function _Write($_obj, $_store) {

	if ($_obj -eq $null) {
		throw "Obj cannot be null"
	}

	if ([System.String]::IsNullOrEmpty($_store)) {
		throw "Path cannot be null"
	}

	if (-not(Test-Path $_store)) {
		New-Item -type File -Force $_store -ErrorAction Stop | Out-Null
	}

	$_store = $(Resolve-Path $_store).Path 

	WriteHashTable $_obj $_store

	return _Read $_store
}

function _Read($_store) {
	[xml]$xml = Get-Content $_store

	$ret = @{}

	foreach($property in $($xml.root | Get-Member -MemberType Property)) {
		$n = $property.Name
		$ret."$n" = $xml.root."$n"
	}

	return $ret
}

switch($Command)
{
    "Write"
    {
        return _Write $obj $Path
    }
	"Read"
	{
		return _Read $Path
	}
    default
    {
		throw "Unknown command"
    }
}

