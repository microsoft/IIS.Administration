# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


$SALT_SIZE = 8
$KEY_SIZE = 32
$HASH_SIZE = 32

function Get-RandomBytes($bytesLength) {
    $bytes = New-Object "Byte[]" $bytesLength

    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()

    $rng.GetBytes($bytes)

    $rng.Dispose()

    return $bytes
}

function Calculate-Hash($key, $salt, $hashSize) {
    $iterations = 1000

    $deriver = New-Object "System.Security.Cryptography.Rfc2898DeriveBytes" -ArgumentList ([byte[]]$key, [byte[]]$salt, $iterations)

    $bytes = $deriver.GetBytes($hashSize)

    $deriver.Dispose()

    return $bytes
}

function ConvertTo-UrlSafeBase64($bytes) {
  $sb = New-Object "System.Text.StringBuilder" -ArgumentList $([System.Convert]::ToBase64String($bytes))

  $paddingIndex = $sb.Length
  for ($i = 0; $i -lt $sb.Length; $i++) {

    if ($sb[$i] -eq '=' -and $paddingIndex -eq $sb.Length) {
        $paddingIndex = $i
    }

    if ($sb[$i] -eq '+') {
        $sb[$i] = '-'
    }
    elseif ($sb[$i] -eq '/') {
        $sb[$i] = '_'
    }
  }

  $sb.ToString(0, $paddingIndex)
}



$salt = [byte[]](Get-RandomBytes $SALT_SIZE)
$key = [byte[]](Get-RandomBytes $KEY_SIZE)
$hash = [byte[]](Calculate-Hash $key $salt $HASH_SIZE)

$saltAndKey = New-Object "Byte[]" ($salt.Length + $key.Length)
[System.Buffer]::BlockCopy($salt, 0, $saltAndKey, 0, $salt.Length)
[System.Buffer]::BlockCopy($key, 0, $saltAndKey, $salt.Length, $key.Length)

@{
  AccessToken = ConvertTo-UrlSafeBase64 $saltAndKey;
  TokenHash = ConvertTo-UrlSafeBase64 $hash;
}