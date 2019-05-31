param(
    [string]
    $TestRoot = "$env:SystemDrive\tests\iisadmin"
)

$CCS_FOLDER_NAME = "CentralCertStore"
$CERTIFICATE_PASS = "abcdefg"
$CERTIFICATE_NAME = "IISAdminLocalTest"

function New-CcsSelfSignedCertificate($certName) {
    $command = Get-Command "New-SelfSignedCertificate"
    $cert = $null

    # Private key should be exportable
    if ($command.Parameters.Keys.Contains("KeyExportPolicy")) {
        Write-Host "Calling New-SelfSignedCertificate and create an exportable cert..."
        $cert = New-SelfSignedCertificate -KeyExportPolicy Exportable -DnsName $certName
    }
    else {
        Write-Host "Calling New-SelfSignedCertificate and saving cert to Cert:\LocalMachine\My..."
        $cert = New-SelfSignedCertificate -DnsName $certName -CertStoreLocation Cert:\LocalMachine\My
    }
    $cert
}

$ccsPath = [System.IO.Path]::Combine($TestRoot, $CCS_FOLDER_NAME)

if (-not(Test-Path $ccsPath)) {
    New-Item -Type Directory -Path $ccsPath -ErrorAction Stop | Out-Null
}

$cert = New-CcsSelfSignedCertificate -certName $CERTIFICATE_NAME
Get-ChildItem Cert:\LocalMachine\My\ | Where-Object {$_.Subject -eq "CN=$CERTIFICATE_NAME"} | Remove-Item
$bytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $CERTIFICATE_PASS)
$pfxPath = [System.IO.Path]::Combine($ccsPath, $CERTIFICATE_NAME + ".pfx")
[System.IO.File]::WriteAllBytes($pfxPath, $bytes)

Write-Host "Exported cert to $pfxPath"

# Check for ccs entry in hosts file to allow local testing of ccs binding
$hostFile = "C:\Windows\System32\drivers\etc\hosts"
$lines = [System.IO.File]::ReadAllLines($hostFile)
$containsCertHostName = $false
foreach ($line in $lines) {
    if ($_ -match $CERTIFICATE_NAME) {
        $containsCertHostName = $true
    }
}

if (-not($containsCertHostName)) {
    $lines += "127.0.0.1 $CERTIFICATE_NAME"
    [System.IO.File]::WriteAllLines($hostFile, $lines)
}
