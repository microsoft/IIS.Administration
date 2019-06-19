### Example
## Add Administrator: .\config-edit.ps1 -query '.security.users.administrators |= . + [\"USER\"]'
## Add Cors: .\config-edit.ps1 -query '.cors.rules |= . + [{\"origin\": \"URL\", "allow": true }]'

## Note that because of how powershell and jq escape sequence works, if you were to use double quoted string instead of single quote
## you would need to escape double quotation twice, etc: """" => ", """"my name""""" => "my name"

#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [string]
    $serviceName = "Microsoft IIS Administration",

    [string]
    $jqVersion = "1.6",

    [string]
    $jqSource = "https://github.com/stedolan/jq/releases/download/jq-{0}/jq-win{1}.exe",

    [string]
    $jqTarget,

    [Parameter(Mandatory=$true)]
    [string]
    $query,

    [switch]
    $quiet,

    [string]
    $administratorsSID = 'S-1-5-32-544',

    [switch]
    $wait
)

$ErrorActionPreference = "Stop"

function EnsureJQ {
    if ((!$jqTarget) -and (Get-Command "jq" -ErrorAction SilentlyContinue)) {
        return "jq"
    } else {
        if ([Environment]::Is64BitProcess) {
            $bitness = 64
        } else {
            $bitness = 32
        }
        $jqPath = $jqTarget;
        if (!$jqPath) {
            $jqPath = Join-Path $env:TEMP "jq.exe"
        }
        $downloadFrom = $jqSource -f $jqVersion, $bitness
        if (!(Test-Path $jqPath)) {
            Invoke-WebRequest -Uri $downloadFrom -OutFile $jqPath
        }
        return $jqPath
    }
}

function LogVerbose($msg){
    Write-Verbose $msg
}

function GetIISAdminHome($procs) {
    foreach ($proc in $procs) {
        $iisMainModule = $proc.Modules | Where-Object { $_.ModuleName -eq "Microsoft.IIS.Administration.dll" }
        if ($iisMainModule) {
            LogVerbose "IIS Admin module found at $($iisMainModule.FileName)"
            return Split-Path $iisMainModule.FileName
        }
    }
    throw "Unable to locate IIS Admin Home"
}

function ConvertTo-NTAccount($From)
{
    if ($From -is [System.Security.Principal.NTAccount]) {
        return $From
    }
    if ($From -is [System.Security.Principal.SecurityIdentifier]) {
        return ($From.Translate([System.Security.Principal.NTAccount]))
    }
    if (!($From -is [string])) {
        Throw "Don't know how to convert an object of type '$($From.GetType())' to an NTAccount"
    }
    try {
        # Try the symbolic format first.
        # For the symbolic format, translate twice, to make sure that
        # the value is valid.
        $acc = new-object System.Security.Principal.NTAccount($From)
        $sid = $acc.Translate([System.Security.Principal.SecurityIdentifier])
        return ($sid.Translate([System.Security.Principal.NTAccount]))
    } catch {
        $sid = new-object System.Security.Principal.SecurityIdentifier($From)
        return ($sid.Translate([System.Security.Principal.NTAccount]))
    }
}


function EnsureAcl($workingDirectory) {
    $apiHome = [System.IO.Path]::Combine($workingDirectory, "..")
    $modifyAcess = [System.Security.AccessControl.FileSystemRights]::Modify
    $allow = [System.Security.AccessControl.AccessControlType]::Allow
    $builtInAdministrators = (ConvertTo-NTAccount $administratorsSID).value
    $dirAcl = Get-Acl $apiHome
    $dirAccessGranted = $dirAcl.Access | Where-Object { ($_.IdentityReference.Value -eq $builtInAdministrators) -and ($_.AccessControlType -eq $allow) -and (($_.FileSystemRights -bAnd $modifyAcess) -eq $modifyAcess) }
    if (!$dirAccessGranted) {
        if (!$quiet) {
            $confirm = Read-Host "$builtInAdministrators will PERMANENTLY gain modify access to $apiHome, proceed? (Y/n)"
            if ($confirm -ne "y") {
                throw "User cancelled"
            }
        }

        $dirAccess = ($dirAcl.Access | Where-Object { ($_.IdentityReference.Value -eq $builtInAdministrators) -and ($_.AccessControlType -eq $allow) })[0]
        if (!$dirAccess) {
            throw "Unexpected, administators do not have an allowed rule on the $serviceName installed directory"
        }
        $newAccess = New-Object System.Security.AccessControl.FileSystemAccessRule -ArgumentList $dirAccess.IdentityReference, ($dirAccess.FileSystemRights -bOr $modifyAcess), ($dirAccess.InheritanceFlags), ($dirAccess.PropagationFlags), $allow
        $dirAcl.RemoveAccessRule($dirAccess)
        $dirAcl.SetAccessRule($newAccess)
        Set-Acl -Path $apiHome -AclObject $dirAcl | Out-Null
    }
}

$service = Get-WmiObject win32_service | Where-Object {$_.name -eq $serviceName}
if ($service) {
    if ($service.StartInfo.EnvironmentVariables -and $service.StartInfo.EnvironmentVariables["USE_CURRENT_DIRECTORY_AS_ROOT"] -and $service.StartInfo.WorkingDirectory) {
        $workingDirectory = $service.StartInfo.WorkingDirectory
    } else {
        $proc = Get-Process -id $service.ProcessId
        $workingDirectory = GetIISAdminHome $proc
    }
} else {
    ## dev-mode support, no restart can be perfomed
    LogVerbose "Dev mode, scanning processes for IIS Admin API"
    $devMode = $true
    $workingDirectory = GetIISAdminHome (Get-Process -ProcessName dotnet)
}

$configLocation = [System.IO.Path]::Combine($workingDirectory, "config", "appsettings.json")
if ($devMode -and !(Test-Path $configLocation)) {
    $configLocation = [System.IO.Path]::Combine($workingDirectory, "..", "..", "..", "config", "appsettings.json")
}
LogVerbose "Config Location $configLocation"

$jqExe = EnsureJQ
EnsureAcl $workingDirectory
$newContent = (Get-Content -Raw $configLocation | & $jqExe $query) -join "`n"
if (!(ConvertFrom-Json $newContent)) {
    throw "Invalid query string"
}
LogVerbose $newContent
$newContent -join "`n" | Out-File -Force $configLocation

Restart-Service -Name $serviceName -Confirm:(!$quiet)

if ($wait) {
    $retryCount = 10
    $retryPeriod = 10
    $started = $false
    while (!$started -and ($retryCount -gt 0)) {
        if ((Get-Service $serviceName).Status -eq [System.ServiceProcess.ServiceControllerStatus]::Running) {
            $started = $true
        } else {
            Start-Sleep $retryPeriod
            $retryCount--
        }
    }
    if (!$started) {
        throw "Timeout waiting for $serviceName to start"
    }
}
