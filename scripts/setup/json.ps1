# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Get-JsonContent",
                 "Set-JsonContent",
                 "Serialize",
                 "Deserialize",
                 "Add-Property",
                 "Remove-Property",
                 "To-HashObject")]
    [string]
    $Command,
    
    [parameter()]
    [string]
    $Path,
    
    [parameter()]
    [System.Array]
    $OldModules,
    
    [parameter()]
    [System.Array]
    $NewModules,
    
    [parameter()]
    [System.Array]
    $Modules,
    
    [parameter()]
    [System.Object]
    $JsonObject,
    
    [parameter()]
    [string]
    $Name,
    
    [parameter()]
    [System.Object]
    $Value,
    
    [parameter()]
    [string]
    $JsonString
)

# Returns an object representation parsed from the given string.
# JsonString: The string value to parse.
function Deserialize($_content) {
    $fromJsonCommand = Get-Command "ConvertFrom-Json" -ErrorAction SilentlyContinue
    if ($fromJsonCommand -ne $null) {
        ConvertFrom-Json $_content
    }
    else {
        Add-Type -assembly System.Web.Extensions
        $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
        $serializer.DeserializeObject($_content)
    }
}

# Returns the JSON representation of an object as a string.
# JsonObject: The object to serialize.
function Serialize($_jsonObject) {
    $toJsonCommand = Get-Command "ConvertTo-Json" -ErrorAction SilentlyContinue
    if ($toJsonCommand -ne $null) {
        ConvertTo-Json $_jsonObject -Depth 100
    }
    else {
        # Javascript serialize will throw a circular reference error if any part of the object being serialized was returned from a powershell function using the 'return' keyword
        Add-Type -assembly System.Web.Extensions
        $serializer = New-Object System.Web.Script.Serialization.JavaScriptSerializer
        $_jsonObject = To-HashObject $_jsonObject
        $serializer.Serialize($_jsonObject)
    }
}

# Returns the content of a file formatted with JSON as an object.
# Path: The path to the file.
function Get-JsonContent($_path)
{
	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path required"
	}

    if (-not(Test-Path $_path)) {
        throw "$_path not found."
    }

    $lines = Get-Content $Path

    $content = ""

    foreach ($line in $lines) {
        $content += $line
    }

    Deserialize $content
}

# Serializes an object and sets the content of the file at the given path to the serialized output.
# Path: The path of the file to write the JSON result to.
# JsonObject: The object to serialize.
function Set-JsonContent($_path, $_jsonObject) {

	if ([System.String]::IsNullOrEmpty($_path)) {
		throw "Path required"
	}

    if ($_jsonObject -eq $null) {
        throw "JsonObject required"
    }
    
    $content = Serialize $_jsonObject
    .\files.ps1 Write-FileForced -Path $_path -Content $content
}

# Removes a property from an object
# $JsonObject: The object to remove the property from
# $Name: The name of the property to remove
function Remove-Property($_jsonObject, $_key) {
    if (($_jsonObject | Get-Member "Remove" -MemberType Method) -ne $null) {
        $_jsonObject.Remove($_key)
    }
    else {
        $_jsonObject.PsObject.Properties.Remove("$_key")
    }
}

# Removes a property from an object
# JsonObject: The object to remove the property from
# Name: The name of the property to add
# Value: The name of the property to remove
function Add-Property($_jsonObject, $_name, $_value) {
    try {
        $_jsonObject."$_name" = $_value
    }
    catch {
        $_jsonObject | Add-Member -MemberType NoteProperty -Name $_name -Value $_value
    }
}

# Converts a deserialized object into a simple powershell object
# JsonObject: The object to transform
function To-HashObject($o) {
    $ret = New-Object 'System.Collections.Generic.Dictionary[string,Object]'
    $keys = $o.keys
    if ($keys -eq $null) {
        $keys = $o | Get-Member -MemberType "NoteProperty" | %{$_.Name}
    }
    foreach ($key in $keys) {
        $val = $o.$key
        if ($val -ne $null -and $val.keys -ne $null) {
            $val = To-HashObject $val
        }
        $ret.Add($key, $val)
    }
    $ret
}

switch ($Command)
{
    "Get-JsonContent"
    {
        Get-JsonContent $Path
    }
    "Set-JsonContent"
    {
        Set-JsonContent $Path $JsonObject
    }
    "Serialize"
    {
        Serialize $JsonObject
    }
    "Deserialize"
    {
        Deserialize $JsonString
    }
    "Add-Property"
    {
        Add-Property $JsonObject $Name $Value
    }
    "Remove-Property"
    {
        Remove-Property $JsonObject $Name
    }
    "To-HashObject" 
    {
        To-HashObject $JsonObject
    }
    default
    {
        throw "Unknown command"
    }
}

