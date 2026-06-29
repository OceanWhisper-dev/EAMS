<#
.SYNOPSIS
    部署 lxc132 (192.168.101.32) — Nginx 前端
.DESCRIPTION
    备份当前前端文件 → 部署新版本 → 重载 Nginx → 验证。
    每个 LXC 独立脚本，支持独立密码。
.PARAMETER PublishDir
    发布包目录，默认 publish/v0.4.260618
.PARAMETER Host
    目标服务器 IP，默认 192.168.101.32
.PARAMETER WebPath
    前端部署路径，默认 /var/www/eams/web
.PARAMETER NginxPort
    Nginx 监听端口，默认 8266
.PARAMETER SkipVerify
    跳过验证
.PARAMETER AcceptHostKeys
    自动接受主机密钥
.EXAMPLE
    .\scripts\deploy-lxc132.ps1
.NOTES
    依赖 PuTTY (plink.exe + pscp.exe)，脚本会自动下载。
    回滚：将备份目录 /var/backups/eams/web_YYYYMMDD_HHMMSS/ 复制回 /var/www/eams/web/。
#>

param(
    [string]$PublishDir = "publish/v0.4.2026.0625f",
    [string]$Hostname = "192.168.101.32",
    [string]$WebPath = "/var/www/eams/web",
    [int]$NginxPort = 8266,
    [switch]$SkipVerify,
    [switch]$AcceptHostKeys
)

$Config = @{
    SshUser    = "root"
    BackupRoot = "/var/backups/eams"
    PuTTY_Url  = "https://the.earth.li/~sgtatham/putty/latest/w64/"
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
    Write-Step "$ExeName 未找到，正在从官网下载..." "WARN"
    try {
        Invoke-WebRequest -Uri ($Config.PuTTY_Url + $UrlSuffix) -OutFile $localPath -UseBasicParsing
        Write-Step "下载完成: $localPath" "OK"
        return $localPath
    } catch { Write-Step "下载失败: $_" "ERROR"; exit 1 }
}

function Get-SshPassword {
    $securePwd = Read-Host -Prompt "请输入 root@${Hostname} 的密码" -AsSecureString
    $cred = New-Object System.Management.Automation.PSCredential("user", $securePwd)
    return $cred.GetNetworkCredential().Password
}

# 自动检测最新的发布目录
if (-not $PSBoundParameters.ContainsKey('PublishDir')) {
    $publishRoot = Join-Path (Split-Path $PSScriptRoot -Parent) "publish"
    if (Test-Path $publishRoot) {
        $latest = Get-ChildItem $publishRoot -Directory | Where-Object { $_.Name -match '^v' } | Sort-Object Name -Descending | Select-Object -First 1
        if ($latest) { $PublishDir = "publish/$($latest.Name)" }
    }
}

# ── 入口 ──
Clear-Host
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "  EAMS2026 — 部署 lxc132 (Nginx 前端)" -ForegroundColor Cyan
Write-Host "  Target: ${Hostname}:${NginxPort}" -ForegroundColor Cyan
Write-Host "  Path:   $WebPath" -ForegroundColor Cyan
Write-Host "  Pkg:    $PublishDir" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path "$PublishDir/web")) { Write-Step "发布包目录不存在: $PublishDir/web" "ERROR"; exit 1 }

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

$plainPwd = Get-SshPassword
$pwdArg = @("-pw", $plainPwd)

# 测试连接 + 自动接受主机密钥
Write-Step "测试 SSH 连接..." "INFO"
$test = Invoke-Ssh "echo OK"
$testStr = $test -join "`n"
if ($testStr -match "^OK$") {
    Write-Step "连接成功" "OK"
} elseif ($testStr -match "host key is not cached") {
    Write-Step "主机密钥未缓存，正在自动接受..." "WARN"
    $hostKey = $null
    if ($testStr -match "ssh-\S+\s+\d+\s+(SHA256:\S+)") { $hostKey = "ssh-ed25519 255 " + $Matches[1] }
    elseif ($testStr -match "SHA256:\S+") { $hostKey = "ssh-ed25519 255 " + $Matches[0] }
    elseif ($testStr -match "(ssh-\S+\s+\d+\s+\S+)") { $hostKey = $Matches[1] }
    if ($hostKey) {
        Write-Step "密钥指纹: $hostKey" "INFO"
        $sshOpts += "-hostkey", $hostKey
        $test = Invoke-Ssh "echo OK"
        if ($test -join "`n" -match "^OK$") { Write-Step "连接成功" "OK" }
        else { Write-Step "连接失败" "ERROR"; Write-Host "plink 输出: $test" -ForegroundColor Red; exit 1 }
    } else {
        Write-Step "无法提取密钥指纹，连接失败" "ERROR"; Write-Host "plink 输出: $test" -ForegroundColor Red; exit 1
    }
} else {
    Write-Step "连接失败" "ERROR"; Write-Host "plink 输出: $test" -ForegroundColor Red; exit 1
}

# ── 步骤 1：备份当前版本 ──
$backupSuffix = (Get-Date -Format "yyyyMMdd_HHmmss")
$backupDir = "$($Config.BackupRoot)/web_$backupSuffix"

Write-Step "步骤 1/4：备份当前前端文件 → $backupDir" "HEADER"
Invoke-Ssh "if [ -d $WebPath ]; then mkdir -p $($Config.BackupRoot) && cp -r $WebPath $backupDir && echo 'BACKUP_OK'; else echo 'NO_EXIST'; fi" | ForEach-Object {
    if ($_ -match "BACKUP_OK") { Write-Step "备份完成" "OK" }
    elseif ($_ -match "NO_EXIST") { Write-Step "远程目录不存在，跳过备份" "WARN" }
}

# ── 步骤 2：创建目录并传输文件 ──
Write-Step "步骤 2/4：传输前端文件" "HEADER"
Invoke-Ssh "mkdir -p $WebPath" | Out-Null
Invoke-Scp -Recurse "$PublishDir/web/*" "$WebPath/"
if ($LASTEXITCODE -ne 0) { Write-Step "传输失败" "ERROR"; exit 1 }
Write-Step "文件传输完成" "OK"

# ── 步骤 3：配置 Nginx 反向代理 ──
Write-Step "步骤 3/4：配置 Nginx 反向代理" "HEADER"
$dotnetHostFromConfig = "192.168.101.31"
$nginxConf = @"
upstream eams_api {
    server ${dotnetHostFromConfig}:5106;
    keepalive 32;
}

server {
    listen ${NginxPort};
    server_name _;
    charset utf-8;
    client_max_body_size 10M;

    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml;
    gzip_min_length 1024;

    root $WebPath;
    index index.html;

    location /api/ {
        proxy_pass http://eams_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade `$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host `$host;
        proxy_set_header X-Real-IP `$remote_addr;
        proxy_set_header X-Forwarded-For `$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto `$scheme;
        proxy_read_timeout 90s;
    }

    location /hubs/ {
        proxy_pass http://eams_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade `$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host `$host;
        proxy_set_header X-Real-IP `$remote_addr;
        proxy_set_header X-Forwarded-For `$proxy_add_x_forwarded_for;
        proxy_read_timeout 86400s;
    }

    location / {
        try_files `$uri `$uri/ /index.html;
    }

    location /assets/ {
        expires 30d;
        add_header Cache-Control "public, immutable";
    }

    access_log /var/log/nginx/eams_access.log;
    error_log /var/log/nginx/eams_error.log;
}
"@
$confTemp = [System.IO.Path]::GetTempFileName()
$nginxConf | Out-File $confTemp -Encoding ASCII
$nginxDir = Invoke-Ssh "test -d /etc/nginx/sites-enabled && echo 'sites' || echo 'conf.d'"
if ($nginxDir -eq "sites") {
    Invoke-Scp $confTemp "/etc/nginx/sites-available/eams2026" | Out-Null
    Invoke-Ssh "ln -sf /etc/nginx/sites-available/eams2026 /etc/nginx/sites-enabled/" | Out-Null
} else {
    Invoke-Scp $confTemp "/etc/nginx/conf.d/eams2026.conf" | Out-Null
}
Remove-Item $confTemp -Force

$nginxTest = Invoke-Ssh "nginx -t 2>&1"
if ($nginxTest -match "test is successful") {
    Invoke-Ssh "systemctl reload nginx || systemctl restart nginx" | Out-Null
    Write-Step "Nginx 配置完成并已重载" "OK"
} else {
    Write-Step "Nginx 配置测试失败：" "ERROR"
    Write-Step $nginxTest "ERROR"
    exit 1
}

# ── 步骤 4：验证 ──
if (-not $SkipVerify) {
    Write-Step "步骤 4/4：验证" "HEADER"
    Start-Sleep -Seconds 2
    $nginxStatus = Invoke-Ssh "systemctl is-active nginx"
    if ($nginxStatus -eq "active") {
        Write-Step "  Nginx 运行中" "OK"
        $portCheck = Invoke-Ssh "ss -tlnp | grep ':${NginxPort}' || echo 'NOT_FOUND'"
        if ($portCheck -ne "NOT_FOUND") { Write-Step "  端口 ${NginxPort} 已监听" "OK" }
        else { Write-Step "  端口 ${NginxPort} 未监听" "WARN" }
    } else { Write-Step "  Nginx 异常: $nginxStatus" "ERROR" }
}

# ── 完成 ──
Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "  lxc132 部署完成" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host "  访问:  http://${Hostname}:${NginxPort}" -ForegroundColor Green
Write-Host "  日志:  tail -f /var/log/nginx/eams_access.log" -ForegroundColor Gray
Write-Host "  回滚:  将备份复制回原位置" -ForegroundColor Yellow
Write-Host "         rm -rf $WebPath/* && cp -r ${backupDir}/* $WebPath/ && systemctl reload nginx" -ForegroundColor Yellow
Write-Host "  备份:  ${backupDir}/" -ForegroundColor Gray
Write-Host "==============================================" -ForegroundColor Green

$plainPwd = $null; [System.GC]::Collect()
