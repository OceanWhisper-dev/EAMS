<#
.SYNOPSIS
    Deploy lxc131 (192.168.101.31) - .NET API
.DESCRIPTION
    Backup current API -> deploy new version -> restart service -> verify.
.PARAMETER PublishDir
    Publish directory, default publish/v0.4.260618
.PARAMETER Host
    Target server IP, default 192.168.101.31
.PARAMETER ApiPath
    API deploy path, default /var/www/eams/api
.PARAMETER ServiceName
    systemd service name, default eams-api
.PARAMETER ApiPort
    API port, default 5106
.PARAMETER SkipVerify
    Skip health check verification
.PARAMETER AcceptHostKeys
    Auto-accept host keys
.EXAMPLE
    .\scripts\deploy-lxc131.ps1
    .\scripts\deploy-lxc131.ps1 -PublishDir "publish/v0.5.260701"
.NOTES
    Requires PuTTY (plink.exe + pscp.exe), auto-download if missing.
#>

param(
    [string]$PublishDir = "D:\Codes\Projects\eams2026\publish\v0.4.2026.0624",
    [string]$Hostname = "192.168.101.31",
    [string]$ApiPath = "/var/www/eams/api",
    [string]$ServiceName = "eams-api",
    [int]$ApiPort = 5106,
    [switch]$SkipVerify,
    [switch]$AcceptHostKeys
)

$Config = @{
    SshUser      = "root"
    BackupRoot   = "/var/backups/eams"
    PuTTY_Url    = "https://the.earth.li/~sgtatham/putty/latest/w64/"
}

function Write-Step { param([string]$Message, [string]$Status = "INFO")
    $colorMap = @{ INFO="Cyan"; OK="Green"; WARN="Yellow"; ERROR="Red"; HEADER="Magenta" }
    $c = $colorMap[$Status]; if (-not $c) { $c = "White" }
    Write-Host ("[{0}] {1}" -f $Status, $Message) -ForegroundColor $c
}

function Ensure-PuttyTool {
    param([string]$ExeName, [string]$UrlSuffix)
    $localPath = Join-Path $PSScriptRoot $ExeName
    if (Test-Path $localPath) { return $localPath }
    $sysPath = (Get-Command $ExeName -ErrorAction SilentlyContinue).Source
    if ($sysPath) { return $ExeName }
    Write-Step "$ExeName not found, downloading from official site..." "WARN"
    try {
        Invoke-WebRequest -Uri ($Config.PuTTY_Url + $UrlSuffix) -OutFile $localPath -UseBasicParsing
        Write-Step "Downloaded: $localPath" "OK"
        return $localPath
    } catch { Write-Step "Download failed: $_" "ERROR"; exit 1 }
}

function Get-SshPassword {
    $securePwd = Read-Host -Prompt "Enter root@${Hostname} password" -AsSecureString
    $cred = New-Object System.Management.Automation.PSCredential("user", $securePwd)
    return $cred.GetNetworkCredential().Password
}

# Auto-detect latest publish directory
if (-not $PSBoundParameters.ContainsKey('PublishDir')) {
    $publishRoot = Join-Path (Split-Path $PSScriptRoot -Parent) "publish"
    if (Test-Path $publishRoot) {
        $latest = Get-ChildItem $publishRoot -Directory | Where-Object { $_.Name -match '^v' } | Sort-Object Name -Descending | Select-Object -First 1
        if ($latest) { $PublishDir = "publish/$($latest.Name)" }
    }
}

# --- Entry ---
Clear-Host
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "  EAMS2026 - Deploy lxc131 (.NET API)" -ForegroundColor Cyan
Write-Host "  Target: ${Hostname}:${ApiPort}" -ForegroundColor Cyan
Write-Host "  Path:   $ApiPath" -ForegroundColor Cyan
Write-Host "  Pkg:    $PublishDir" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path "$PublishDir/api")) { Write-Step "Publish dir not found: $PublishDir/api" "ERROR"; exit 1 }

$plink = Ensure-PuttyTool plink.exe plink.exe
$pscp  = Ensure-PuttyTool pscp.exe  pscp.exe
$sshOpts = @("-batch")
if ($AcceptHostKeys) { $sshOpts += "-hostkey", "*" }
$sshUser = $Config.SshUser

function Invoke-Ssh {
    param([string]$RemoteCommand)
    $fullArgs = $sshOpts + @("-ssh", "-l", $sshUser) + $pwdArg + @($Hostname, $RemoteCommand)
    & $plink @fullArgs 2>&1
}

function Invoke-Scp {
    param([string]$LocalPath, [string]$RemotePath, [switch]$Recurse)
    $extra = @()
    if ($Recurse) { $extra += "-r" }
    $fullArgs = $sshOpts + @("-scp", "-l", $sshUser) + $pwdArg + $extra + @($LocalPath, "${Hostname}:${RemotePath}")
    & $pscp @fullArgs 2>&1
}

# --- Password ---
$plainPwd = Get-SshPassword
$pwdArg = @("-pw", $plainPwd)

# Test connection + auto-accept host key
Write-Step "Testing SSH connection..." "INFO"
$test = Invoke-Ssh "echo OK"
$testStr = $test -join "`n"
if ($testStr -match "^OK$") {
    Write-Step "Connection OK" "OK"
} elseif ($testStr -match "host key is not cached") {
    Write-Step "Host key not cached, auto-accepting..." "WARN"
    $hostKey = $null
    if ($testStr -match "ssh-\S+\s+\d+\s+(SHA256:\S+)") { $hostKey = "ssh-ed25519 255 " + $Matches[1] }
    elseif ($testStr -match "SHA256:\S+") { $hostKey = "ssh-ed25519 255 " + $Matches[0] }
    elseif ($testStr -match "(ssh-\S+\s+\d+\s+\S+)") { $hostKey = $Matches[1] }
    if ($hostKey) {
        Write-Step "Key fingerprint: $hostKey" "INFO"
        $sshOpts += "-hostkey", $hostKey
        $test = Invoke-Ssh "echo OK"
        if ($test -join "`n" -match "^OK$") { Write-Step "Connection OK" "OK" }
        else { Write-Step "Connection failed" "ERROR"; Write-Host "plink output: $test" -ForegroundColor Red; exit 1 }
    } else {
        Write-Step "Cannot extract key fingerprint, connection failed" "ERROR"; Write-Host "plink output: $test" -ForegroundColor Red; exit 1
    }
} else {
    Write-Step "Connection failed" "ERROR"; Write-Host "plink output: $test" -ForegroundColor Red; exit 1
}

# --- Step 1: Backup ---
$backupSuffix = (Get-Date -Format "yyyyMMdd_HHmmss")
$backupDir = "$($Config.BackupRoot)/api_$backupSuffix"

Write-Step "Step 1/5: Backup current API -> $backupDir" "HEADER"
Invoke-Ssh "if [ -d $ApiPath ]; then mkdir -p $($Config.BackupRoot) && cp -r $ApiPath $backupDir && echo 'BACKUP_OK'; else echo 'NO_EXIST'; fi" 2>&1 | ForEach-Object {
    if ($_ -match "BACKUP_OK") { Write-Step "Backup done" "OK" }
    elseif ($_ -match "NO_EXIST") { Write-Step "Remote dir not exist, skip backup" "WARN" }
}

# --- Step 2: Create target dir ---
Write-Step "Step 2/5: Create target directory" "HEADER"
Invoke-Ssh "mkdir -p $ApiPath/logs" | Out-Null

# --- Step 3: Transfer files (preserve production config) ---
Write-Step "Step 3/5: Transfer API files (preserving production config)" "HEADER"

# Backup production appsettings.json before overwriting
$hasProdCfg = Invoke-Ssh "test -f $ApiPath/appsettings.json && echo YES"
if ($hasProdCfg -eq "YES") {
    Write-Step "Backing up production appsettings.json..." "INFO"
    Invoke-Ssh "cp $ApiPath/appsettings.json /tmp/appsettings.json.prod.bak" | Out-Null
    Write-Step "Production appsettings.json backed up" "OK"
}

Invoke-Scp -Recurse "$PublishDir/api/*" "$ApiPath/"
if ($LASTEXITCODE -ne 0) {
    # Try to restore backup if transfer failed
    if ($hasProdCfg -eq "YES") { Invoke-Ssh "cp /tmp/appsettings.json.prod.bak $ApiPath/appsettings.json 2>/dev/null" | Out-Null }
    Write-Step "Transfer failed" "ERROR"; exit 1
}

# Restore production appsettings.json
if ($hasProdCfg -eq "YES") {
    Invoke-Ssh "cp /tmp/appsettings.json.prod.bak $ApiPath/appsettings.json && rm -f /tmp/appsettings.json.prod.bak" | Out-Null
    Write-Step "Production appsettings.json restored" "OK"
} else {
    Write-Step "No existing appsettings.json found, using packaged version" "WARN"
    Write-Step "  Please verify: plink root@${Hostname} -pw xxx cat $ApiPath/appsettings.json" "WARN"
}

# Remove development config from production
Invoke-Ssh "rm -f $ApiPath/appsettings.Development.json" | Out-Null

Write-Step "Files transferred (backup kept at $backupDir)" "OK"

# --- Step 4: Configure systemd and restart ---
Write-Step "Step 4/5: Configure systemd service and restart" "HEADER"
$svcContent = @"
[Unit]
Description=EAMS2026 API Server
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=$ApiPath
ExecStart=/usr/bin/dotnet $ApiPath/EAMS2026.Api.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:${ApiPort}
StandardOutput=journal
StandardError=journal
SyslogIdentifier=eams-api
LimitNOFILE=65535

[Install]
WantedBy=multi-user.target
"@
$svcTemp = [System.IO.Path]::GetTempFileName()
$svcContent | Out-File $svcTemp -Encoding ASCII
Invoke-Scp $svcTemp "/etc/systemd/system/${ServiceName}.service"
Remove-Item $svcTemp -Force
$remoteCmd = "systemctl daemon-reload; systemctl enable " + $ServiceName + "; systemctl restart " + $ServiceName
Invoke-Ssh $remoteCmd | Out-Null
Start-Sleep -Seconds 2
$svcStatus = Invoke-Ssh "systemctl is-active ${ServiceName}"
if ($svcStatus -eq "active") { Write-Step "Service restarted and running" "OK" }
else { Write-Step "Service status: $svcStatus (check logs)" "WARN" }

# --- Step 5: Verify ---
if (-not $SkipVerify) {
    Write-Step "Step 5/5: Verify API is listening on port ${ApiPort}" "HEADER"
    Start-Sleep -Seconds 3
    $healthCmd = "ss -tlnp | grep " + $ApiPort + " | head -1"
    $listening = Invoke-Ssh $healthCmd
    if ($listening -match $ApiPort) { Write-Step "API is listening on port ${ApiPort}" "OK" }
    else { Write-Step "API not listening yet (check logs)" "WARN" }
}

# --- Done ---
Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "  lxc131 deploy complete" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host "  Logs:    journalctl -u ${ServiceName} -f" -ForegroundColor Gray
Write-Host "  Restart: systemctl restart ${ServiceName}" -ForegroundColor Gray
Write-Host "  Rollback: restore from backup" -ForegroundColor Yellow
Write-Host "           rm -rf $ApiPath/*; cp -r ${backupDir}/* $ApiPath/; systemctl restart ${ServiceName}" -ForegroundColor Yellow
Write-Host "  Backup:  ${backupDir}/" -ForegroundColor Gray
Write-Host "==============================================" -ForegroundColor Green

$plainPwd = $null; [System.GC]::Collect()
