
Param(    
    [parameter()]
    [int]
    $Port,

    [Parameter()]
    [string]
    $CertHash
)

$output = @{}
$cert = $null
if (![String]::IsNullOrEmpty($CertHash)) {

    # User provided a certificate hash (thumbprint)
    # Retrieve the cert from the hash
    $itemPath = "Cert:\LocalMachine\My\$CertHash"
    $cert = Get-Item $itemPath -ErrorAction SilentlyContinue
    if (!$cert) {
        throw "Could not find certificate with hash $itemPath"
    }
    Write-Verbose "Using certificate with thumbprint $CertHash"
}
else {
    # Check for existing IIS Administration Certificate
    $cert = .\cert.ps1 Get-LatestIISAdminCertificate
    $certCreationName = $(.\globals.ps1 CERT_NAME)

    if ($cert) {
        #
        # Check if existing cert has sufficient lifespan left (3+ months)
        $expirationDate = $cert.NotAfter
        $remainingLifetime = $expirationDate - [System.DateTime]::Now

        if ($remainingLifetime.TotalDays -lt $(.\globals.ps1 CERT_EXPIRATION_WINDOW)) {
            Write-Verbose "The IIS Administration Certificate will expire in less than $(.\globals.ps1 CERT_EXPIRATION_WINDOW) days"
            $certCreationName = $(.\globals.ps1 CERT_NAME) + " " + [System.DateTime]::Now.Year.ToString()
            $cert = $null
        }
    }

    if (!$cert) {
        # No valid cert exists, we must create one to enable HTTPS

        Write-Verbose "Creating new IIS Administration Certificate"
        $cert = .\cert.ps1 New -Name $certCreationName
        $output.createdCertThumbprint = $cert.Thumbprint;

        Write-Verbose "Adding the certificate to trusted store"
        .\cert.ps1 AddToTrusted -Certificate $cert | Out-Null
    }
    else {
        # There is already a Microsoft IIS Administration Certificate on the computer that we can use for the API
        Write-Verbose "Using pre-existing IIS Administration Certificate"
    }
}
# Get the certificate currently bound on desired installation port if any
$preboundCert = .\net.ps1 GetSslBindingInfo -Port $Port
$useIisAdminCert = $true

if ($preboundCert) {
    # We will override the IIS Admin certificate only if the pre-existing binding was also an IIS Admin Certificate
    $useIisAdminCert = .\cert.ps1 Is-IISAdminCertificate -Thumbprint $preboundCert.CertificateHash
}

if ($useIisAdminCert) {
    # If a certificate is bound we delete it to bind our cert
    if ($preboundCert) {
        $output.preboundCertInfo = $preboundCert
        # Remove any preexisting HTTPS. binding on the specified port
        Write-Verbose "Deleting certificate from port $Port in HTTP.Sys"
        .\net.ps1 DeleteSslBinding -Port $Port | Out-Null
    }

    Write-Verbose "Binding Certificate to port $Port in HTTP.Sys"
    .\net.ps1 BindCert -Hash $cert.thumbprint -Port $Port -AppId $(.\globals.ps1 IIS_ADMINISTRATION_APP_ID)  | Out-Null
    $output.newBoundCertPort = $Port
}

$output.cert = $cert

return $output
