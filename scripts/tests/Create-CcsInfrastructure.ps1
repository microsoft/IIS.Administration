if ($env:iis_admin_solution_dir -eq $null) {
    throw "iis_admin_solution_dir not found"
}

$CCS_FOLDER_NAME = "CentralCertStore"
$CERTIFICATE_PASS = "abcdefg"
$CERTIFICATE_NAME = "IISAdminLocalTest"
$CERT_USER_NAME = "IisAdminCcsTestR"
$CERT_USER_PASS = "IisAdmin*12@"

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

$ccsPath = [System.IO.Path]::Combine($env:iis_admin_solution_dir, "test", $CCS_FOLDER_NAME)

if (-not(Test-Path $ccsPath)) {
    New-Item -Type Directory -Path $ccsPath -ErrorAction Stop | Out-Null
}

$cert = New-CcsSelfSignedCertificate -certName $CERTIFICATE_NAME
Get-ChildItem Cert:\LocalMachine\My\ | Where-Object {$_.Subject -eq "CN=$CERTIFICATE_NAME"} | Remove-Item
$bytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $CERTIFICATE_PASS)
[System.IO.File]::WriteAllBytes([System.IO.Path]::Combine($ccsPath, $CERTIFICATE_NAME + ".pfx"), $bytes)

# Find ccs test user
$localUsers = Get-WmiObject -Class Win32_UserAccount -Filter "LocalAccount = True"
$certUser = $localUsers | Where-Object {$_.Caption -match "$CERT_USER_NAME$"}
$userExists = $certUser -ne $null

# Create ccs test user if it doesn't exist
if (-not($userExists)) {
    $Computer = [ADSI]"WinNT://$Env:COMPUTERNAME,Computer"

    $ccsUser = $Computer.Create("User", $CERT_USER_NAME)
    $ccsUser.SetPassword($CERT_USER_PASS)
    $ccsUser.SetInfo()
    $ccsUser.FullName = "Test account for IIS Administration Central Certificate Store"
    $ccsUser.SetInfo()
    $ccsUser.UserFlags = 64 + 65536 # ADS_UF_PASSWD_CANT_CHANGE + ADS_UF_DONT_EXPIRE_PASSWD
    $ccsUser.SetInfo()
}