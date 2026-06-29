﻿﻿﻿﻿﻿<#
.SYNOPSIS
    EAMS2026 一键部署 — 串联脚本
.DESCRIPTION
    按顺序调用三个独立 LXC 部署脚本：lxc137(DB) → lxc131(API) → lxc132(Nginx)。
    每个 LXC 独立输入密码、独立备份、支持独立回滚。
.PARAMETER PublishDir
    发布包目录，默认自动检测 publish/ 下最新版本
.PARAMETER Lxc137Only
    仅部署 lxc137 (PostgreSQL)
.PARAMETER Lxc131Only
    仅部署 lxc131 (.NET API)
.PARAMETER Lxc132Only
    仅部署 lxc132 (Nginx)
.PARAMETER SkipDatabase
    跳过数据库（仅更新 API 和前端）
.PARAMETER SkipDotnet
    跳过后端（仅更新数据库和前端）
.PARAMETER SkipNginx
    跳过前端（仅更新数据库和 API）
.PARAMETER BackupOnly
    仅备份，不部署（仅对数据库有效，走 lxc137 -BackupOnly）
.PARAMETER Incremental
    增量更新数据库：仅应用 schema 差异，不删除生产库（走 lxc137 -Incremental）
.EXAMPLE
    .\scripts\deploy-production.ps1
    .\scripts\deploy-production.ps1 -SkipDatabase
    .\scripts\deploy-production.ps1 -Lxc131Only
    .\scripts\deploy-production.ps1 -Incremental
.NOTES
    三个子脚本位于 scripts/ 目录：
      deploy-lxc137.ps1 — PostgreSQL
      deploy-lxc131.ps1 — .NET API
      deploy-lxc132.ps1 — Nginx
#>

param(
    [string]$PublishDir = "publish/v0.4.260618",
    [switch]$Lxc137Only,
    [switch]$Lxc131Only,
    [switch]$Lxc132Only,
    [switch]$SkipDatabase,
    [switch]$SkipDotnet,
    [switch]$SkipNginx,
    [switch]$BackupOnly,
    [switch]$Incremental,
    [switch]$AcceptHostKeys
)

function Write-Step { param([string]$Message, [string]$Status = "INFO")
    $colorMap = @{ INFO="Cyan"; OK="Green"; WARN="Yellow"; ERROR="Red"; HEADER="Magenta" }
    $c = $colorMap[$Status]; if (-not $c) { $c = "White" }
    Write-Host ("[{0}] {1}" -f $Status, $Message) -ForegroundColor $c
}

# 自动检测最新的发布目录
if (-not $PSBoundParameters.ContainsKey('PublishDir')) {
    $publishRoot = Join-Path (Split-Path $PSScriptRoot -Parent) "publish"
    if (Test-Path $publishRoot) {
        $latest = Get-ChildItem $publishRoot -Directory | Where-Object { $_.Name -match '^v' } | Sort-Object Name -Descending | Select-Object -First 1
        if ($latest) { $PublishDir = "publish/$($latest.Name)" }
    }
}

Clear-Host
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  EAMS2026 一键生产部署 v0.4.260618 - 串联脚本" -ForegroundColor Cyan
Write-Host "  发布包: $PublishDir" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  子脚本说明（每个独立密码、独立备份、独立回滚）：" -ForegroundColor Yellow
Write-Host "  +----------+-----------------+------------------------+" -ForegroundColor Gray
Write-Host "  | LXC      | IP              | 角色                   |" -ForegroundColor Gray
Write-Host "  +----------+-----------------+------------------------+" -ForegroundColor Gray
Write-Host "  | lxc137   | 192.168.101.37  | PostgreSQL 数据库      |" -ForegroundColor Gray
Write-Host "  | lxc131   | 192.168.101.31  | .NET API               |" -ForegroundColor Gray
Write-Host "  | lxc132   | 192.168.101.32  | Nginx 前端             |" -ForegroundColor Gray
Write-Host "  +----------+-----------------+------------------------+" -ForegroundColor Gray
Write-Host ""

# 检查发布包
if (-not (Test-Path $PublishDir)) {
    Write-Step "发布包目录不存在: $PublishDir" "ERROR"
    exit 1
}

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = "." }

$lxc137Script = Join-Path $scriptDir "deploy-lxc137.ps1"
$lxc131Script = Join-Path $scriptDir "deploy-lxc131.ps1"
$lxc132Script = Join-Path $scriptDir "deploy-lxc132.ps1"

foreach ($s in @($lxc137Script, $lxc131Script, $lxc132Script)) {
    if (-not (Test-Path $s)) { Write-Step "子脚本未找到: $s" "ERROR"; exit 1 }
}

# 确定调用哪些子脚本
$run137 = $true; $run131 = $true; $run132 = $true

if ($Lxc137Only)    { $run131 = $false; $run132 = $false }
if ($Lxc131Only)    { $run137 = $false; $run132 = $false }
if ($Lxc132Only)    { $run137 = $false; $run131 = $false }
if ($SkipDatabase)  { $run137 = $false }
if ($SkipDotnet)    { $run131 = $false }
if ($SkipNginx)     { $run132 = $false }

# ================================================================
#  执行顺序：lxc137 -> lxc131 -> lxc132
# ================================================================
$exitCode = 0

# -- 1. lxc137 PostgreSQL --
if ($run137) {
    Write-Host "------------------------------------------------" -ForegroundColor Magenta
    Write-Host "  [1/3] 部署 lxc137 - PostgreSQL" -ForegroundColor Magenta
    Write-Host "------------------------------------------------" -ForegroundColor Magenta

    $params137 = @{ PublishDir = $PublishDir }
    if ($BackupOnly) { $params137.BackupOnly = $true }
    if ($Incremental) { $params137.Incremental = $true }
    if ($AcceptHostKeys) { $params137.AcceptHostKeys = $true }

    & $lxc137Script @params137
    if ($LASTEXITCODE -ne 0) {
        Write-Step "lxc137 部署失败，终止执行" "ERROR"; $exitCode = 1; exit 1
    }
    Write-Host ""
} else {
    Write-Step "跳过 lxc137 (PostgreSQL)" "WARN"
}

# -- 2. lxc131 .NET API --
if ($run131) {
    Write-Host "------------------------------------------------" -ForegroundColor Magenta
    Write-Host "  [2/3] 部署 lxc131 - .NET API" -ForegroundColor Magenta
    Write-Host "------------------------------------------------" -ForegroundColor Magenta

    $params131 = @{ PublishDir = $PublishDir }
    if ($AcceptHostKeys) { $params131.AcceptHostKeys = $true }

    & $lxc131Script @params131
    if ($LASTEXITCODE -ne 0) {
        Write-Step "lxc131 部署失败，终止执行" "ERROR"; $exitCode = 1; exit 1
    }
    Write-Host ""
} else {
    Write-Step "跳过 lxc131 (.NET API)" "WARN"
}

# -- 3. lxc132 Nginx --
if ($run132) {
    Write-Host "------------------------------------------------" -ForegroundColor Magenta
    Write-Host "  [3/3] 部署 lxc132 - Nginx 前端" -ForegroundColor Magenta
    Write-Host "------------------------------------------------" -ForegroundColor Magenta

    $params132 = @{ PublishDir = $PublishDir }
    if ($AcceptHostKeys) { $params132.AcceptHostKeys = $true }

    & $lxc132Script @params132
    if ($LASTEXITCODE -ne 0) {
        Write-Step "lxc132 部署失败，终止执行" "ERROR"; $exitCode = 1; exit 1
    }
    Write-Host ""
} else {
    Write-Step "跳过 lxc132 (Nginx)" "WARN"
}

# ================================================================
#  完成
# ================================================================
if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Green
    Write-Host "  全部部署完成！" -ForegroundColor Green
    Write-Host "================================================================" -ForegroundColor Green
    Write-Host "  访问地址: http://192.168.101.32:8266" -ForegroundColor Green
    Write-Host ""
    Write-Host "  独立子脚本（可单独执行，各输各的密码）：" -ForegroundColor Yellow
    Write-Host "    .\scripts\deploy-lxc137.ps1               # 仅数据库（全量）" -ForegroundColor Gray
    Write-Host "    .\scripts\deploy-lxc137.ps1 -Incremental  # 仅数据库（增量）" -ForegroundColor Gray
    Write-Host "    .\scripts\deploy-lxc131.ps1               # 仅 API" -ForegroundColor Gray
    Write-Host "    .\scripts\deploy-lxc132.ps1               # 仅 Nginx" -ForegroundColor Gray
    Write-Host "================================================================" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Step "部署过程出现错误，请检查后重试" "ERROR"
}

exit $exitCode