if ($env:iis_admin_solution_dir -eq $null) {
    throw "iis_admin_solution_dir not found"
}

$CCS_FOLDER_NAME = "CentralCertStore"
$CERTIFICATE_PASS = "abcdefg"
$CERTIFICATE_NAME = "IISAdminLocalTest"

function New-CcsSelfSignedCertificate($certName) {
    $command = Get-Command "New-SelfSignedCertificate"
    $cert = $null

    # Private key should be exportable
    if ($command.Parameters.Keys.Contains("KeyExportPolicy")) {
        $cert = New-SelfSignedCertificate -KeyExportPolicy Exportable -DnsName $certName
    }
    else {
        $cert = New-SelfSignedCertificate -DnsName $certName -CertStoreLocation Cert:\LocalMachine\My
    }
    $cert
}

$testRoot = $env:iis_admin_test_dir

if ([string]::IsNullOrEmpty($testRoot)) {
    $testRoot = "$env:SystemDrive\tests\iisadmin"
}

$ccsPath = [System.IO.Path]::Combine($testRoot, $CCS_FOLDER_NAME)

if (-not(Test-Path $ccsPath)) {
    New-Item -Type Directory -Path $ccsPath -ErrorAction Stop | Out-Null
}

$cert = New-CcsSelfSignedCertificate -certName $CERTIFICATE_NAME
Get-ChildItem Cert:\LocalMachine\My\ | Where-Object {$_.Subject -eq "CN=$CERTIFICATE_NAME"} | Remove-Item
$bytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $CERTIFICATE_PASS)
[System.IO.File]::WriteAllBytes([System.IO.Path]::Combine($ccsPath, $CERTIFICATE_NAME + ".pfx"), $bytes)

# Check for ccs entry in hosts file to allow local testing of ccs binding
$hostFile = "C:\Windows\System32\drivers\etc\hosts"
$lines = [System.IO.File]::ReadAllLines($hostFile)
$containsCertHostName = $false
$lines | ForEach-Object {
    if ($_ -match $CERTIFICATE_NAME) { 
        $containsCertHostName = $true
    }
}

if (-not($containsCertHostName)) {
    $lines += "127.0.0.1 $CERTIFICATE_NAME"
    [System.IO.File]::WriteAllLines($hostFile, $lines)
}