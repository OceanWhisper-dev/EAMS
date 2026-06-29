<#
.SYNOPSIS
    EAMS2026 数据库差异对比与增量更新 — 对比开发环境和生产环境数据库，仅应用差异
.DESCRIPTION
    从开发库和生产库分别导出 schema-only dump，使用 apgdiff 对比生成差异 DDL，
    经人工确认后应用到生产库。不删除生产数据库，仅执行 CREATE/ALTER/DROP 语句。

    流程：
      1. 导出开发库 schema  → dev_schema.sql
      2. 导出生产库 schema  → prod_schema.sql
      3. apgdiff 对比       → diff.sql
      4. 人工审查 diff.sql
      5. 确认后应用到生产库

.PARAMETER DevHost
    开发库主机，默认 localhost
.PARAMETER DevDbName
    开发库名称，默认 eams2026
.PARAMETER DevDbUser
    开发库用户，默认 postgres
.PARAMETER DevDbPort
    开发库端口，默认 5432
.PARAMETER ProdHost
    生产库主机，默认 192.168.101.37
.PARAMETER ProdDbName
    生产库名称，默认 eams2026
.PARAMETER ProdDbUser
    生产库用户，默认 eams_user
.PARAMETER ProdDbPort
    生产库端口，默认 5432
.PARAMETER ProdSshUser
    生产服务器 SSH 用户，默认 root
.PARAMETER DiffOnly
    仅生成差异文件，不应用到生产库
.PARAMETER AutoApply
    跳过人工确认，自动应用差异（危险操作，慎用）
.PARAMETER OutputDir
    差异文件输出目录，默认 scripts/db-diff-output/
.PARAMETER AcceptHostKeys
    自动接受 SSH 主机密钥
.EXAMPLE
    .\scripts\db-diff.ps1
    .\scripts\db-diff.ps1 -DiffOnly
    .\scripts\db-diff.ps1 -DevDbName eams2026_dev -ProdDbName eams2026
.NOTES
    依赖：PuTTY (plink.exe), PostgreSQL (pg_dump.exe), Java (apgdiff.jar)
    apgdiff 首次运行会自动下载。
#>

param(
    [string]$DevHost     = "localhost",
    [string]$DevDbName   = "eams2026",
    [string]$DevDbUser   = "postgres",
    [int]$DevDbPort      = 5432,

    [string]$ProdHost    = "192.168.101.37",
    [string]$ProdDbName  = "eams2026",
    [string]$ProdDbUser  = "eams_user",
    [int]$ProdDbPort     = 5432,
    [string]$ProdSshUser = "root",

    [switch]$DiffOnly,
    [switch]$AutoApply,
    [string]$OutputDir   = "",
    [switch]$AcceptHostKeys
)

# ================================================================
#  路径配置
# ================================================================
$ScriptDir  = $PSScriptRoot
if (-not $ScriptDir) { $ScriptDir = "." }
$RepoRoot   = Split-Path $ScriptDir -Parent

if (-not $OutputDir) {
    $OutputDir = Join-Path $ScriptDir "db-diff-output"
}

$PgDump     = "D:\Scoop\apps\postgresql\current\bin\pg_dump.exe"
$PgBin      = "D:\Scoop\apps\postgresql\current\bin"

# apgdiff 配置
$ApgDiffJar   = Join-Path $ScriptDir "apgdiff-2.7.0.jar"
$ApgDiffUrl   = "https://github.com/fordfrog/apgdiff/releases/download/rel-2.7.0/apgdiff-2.7.0.jar"
$ApgDiffLatest = "https://github.com/fordfrog/apgdiff/releases/latest"

# PuTTY
$PuTTY_Url = "https://the.earth.li/~sgtatham/putty/latest/w64/"

# 时间戳
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

# ================================================================
#  辅助函数
# ================================================================
function Write-Step { param([string]$Message, [string]$Status = "INFO")
    $colorMap = @{ INFO="Cyan"; OK="Green"; WARN="Yellow"; ERROR="Red"; HEADER="Magenta" }
    $c = $colorMap[$Status]; if (-not $c) { $c = "White" }
    Write-Host ("[{0}] {1}" -f $Status, $Message) -ForegroundColor $c
}

function Ensure-PuttyTool {
    param([string]$ExeName, [string]$UrlSuffix)
    $localPath = Join-Path $ScriptDir $ExeName
    if (Test-Path $localPath) { return $localPath }
    $sysPath = (Get-Command $ExeName -ErrorAction SilentlyContinue).Source
    if ($sysPath) { return $ExeName }
    Write-Step "$ExeName 未找到，正在从官网下载..." "WARN"
    try {
        Invoke-WebRequest -Uri ($PuTTY_Url + $UrlSuffix) -OutFile $localPath -UseBasicParsing
        Write-Step "下载完成: $localPath" "OK"
        return $localPath
    } catch { Write-Step "下载失败: $_" "ERROR"; exit 1 }
}

function Get-SshPassword {
    param([string]$TargetHost)
    $securePwd = Read-Host -Prompt "请输入 ${ProdSshUser}@${TargetHost} 的密码" -AsSecureString
    $cred = New-Object System.Management.Automation.PSCredential("user", $securePwd)
    return $cred.GetNetworkCredential().Password
}

function Get-PgPassword {
    param([string]$TargetHost, [string]$DbUser)
    $securePwd = Read-Host -Prompt "请输入 PostgreSQL 用户 ${DbUser}@${TargetHost} 的密码" -AsSecureString
    $cred = New-Object System.Management.Automation.PSCredential("user", $securePwd)
    return $cred.GetNetworkCredential().Password
}

# ================================================================
#  入口
# ================================================================
Clear-Host
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  EAMS2026 — 数据库差异对比与增量更新" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  开发库: ${DevHost}:${DevDbPort}/${DevDbName} (${DevDbUser})" -ForegroundColor Gray
Write-Host "  生产库: ${ProdHost}:${ProdDbPort}/${ProdDbName} (${ProdDbUser})" -ForegroundColor Gray
Write-Host "  输出:   $OutputDir" -ForegroundColor Gray
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# ================================================================
#  步骤 0：准备环境
# ================================================================
Write-Step "步骤 0/5：准备环境" "HEADER"

# 创建输出目录
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# 检查 pg_dump
if (-not (Test-Path $PgDump)) {
    Write-Step "pg_dump 未找到: $PgDump" "ERROR"
    Write-Step "请确认 PostgreSQL 已安装到 D:\Scoop\apps\postgresql\current" "ERROR"
    exit 1
}
Write-Step "pg_dump 就绪" "OK"

# 检查/下载 apgdiff
if (-not (Test-Path $ApgDiffJar)) {
    Write-Step "apgdiff 未找到，正在下载..." "WARN"
    try {
        Invoke-WebRequest -Uri $ApgDiffUrl -OutFile $ApgDiffJar -UseBasicParsing
        if (Test-Path $ApgDiffJar) {
            Write-Step "apgdiff 下载完成: $ApgDiffJar" "OK"
        } else {
            Write-Step "apgdiff 下载失败" "ERROR"
            Write-Step "请手动下载: $ApgDiffUrl" "WARN"
            Write-Step "放置到: $ScriptDir" "WARN"
            exit 1
        }
    } catch {
        Write-Step "apgdiff 下载失败: $_" "ERROR"
        Write-Step "请手动下载 $ApgDiffUrl 放到 $ScriptDir" "WARN"
        exit 1
    }
}

# 检查 Java
$javaExe = (Get-Command java -ErrorAction SilentlyContinue).Source
if (-not $javaExe) {
    Write-Step "Java 未找到，apgdiff 需要 Java 运行环境" "ERROR"
    Write-Step "请安装 Java 8+ 或 OpenJDK" "ERROR"
    exit 1
}
Write-Step "Java 就绪: $javaExe" "OK"

# 准备 SSH 工具（用于连接生产库）
$plink = Ensure-PuttyTool plink.exe plink.exe
$pscp  = Ensure-PuttyTool pscp.exe  pscp.exe
$sshOpts = @("-batch")
if ($AcceptHostKeys) { $sshOpts += "-hostkey", "*" }

# 获取密码
$sshPassword = Get-SshPassword $ProdHost
$pwdArg = @("-pw", $sshPassword)
$prodPassword = Get-PgPassword $ProdHost $ProdDbUser

# 获取开发库密码
if ($DevHost -eq "localhost" -or $DevHost -eq "127.0.0.1") {
    $devPassword = Read-Host -Prompt "请输入本地 PostgreSQL 用户 ${DevDbUser} 的密码"
} else {
    $devPassword = Get-PgPassword $DevHost $DevDbUser
}

function Invoke-Ssh {
    param([string]$RemoteCommand)
    $fullArgs = $sshOpts + @("-ssh", "-l", $ProdSshUser) + $pwdArg + @($ProdHost, $RemoteCommand)
    & $plink @fullArgs 2>&1
}

function Invoke-Scp {
    param([string]$LocalPath, [string]$RemotePath)
    $fullArgs = $sshOpts + @("-scp", "-l", $ProdSshUser) + $pwdArg + @($LocalPath, "${ProdHost}:${RemotePath}")
    & $pscp @fullArgs 2>&1
}

# 测试 SSH 连接
Write-Step "测试 SSH 连接..." "INFO"
$sshTest = Invoke-Ssh "echo OK"
$sshTestStr = $sshTest -join "`n"
if ($sshTestStr -match "OK") {
    Write-Step "SSH 连接成功" "OK"
} else {
    Write-Step "SSH 连接失败: $sshTestStr" "ERROR"
    exit 1
}

# ================================================================
#  步骤 1：导出开发库 schema
# ================================================================
Write-Step "步骤 1/5：导出开发库 schema" "HEADER"

$devSchemaFile = Join-Path $OutputDir "dev_schema_${Timestamp}.sql"
$env:PGPASSWORD = $devPassword

$devArgs = @(
    "-h", $DevHost,
    "-p", $DevDbPort,
    "-U", $DevDbUser,
    "-d", $DevDbName,
    "--schema-only",
    "--no-owner",
    "--no-acl",
    "-f", $devSchemaFile
)

Write-Step "正在导出开发库 schema..." "INFO"
& $PgDump @devArgs 2>&1 | Out-Null
$devExit = $LASTEXITCODE

Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue

if ($devExit -ne 0 -or -not (Test-Path $devSchemaFile)) {
    Write-Step "开发库 schema 导出失败 (exit: $devExit)" "ERROR"
    exit 1
}
$devSize = (Get-Item $devSchemaFile).Length
Write-Step "开发库 schema 导出完成: ${devSize} bytes" "OK"

# ================================================================
#  步骤 2：导出生产库 schema
# ================================================================
Write-Step "步骤 2/5：导出生产库 schema" "HEADER"

$prodSchemaFile = Join-Path $OutputDir "prod_schema_${Timestamp}.sql"
$prodRemoteFile = "/tmp/prod_schema_${Timestamp}.sql"

Write-Step "正在从生产库导出 schema..." "INFO"
$dumpResult = Invoke-Ssh "PGPASSWORD='$prodPassword' pg_dump -h localhost -p $ProdDbPort -U $ProdDbUser -d $ProdDbName --schema-only --no-owner --no-acl -f $prodRemoteFile 2>&1; echo PGDUMP_EXIT:`$?"

if ($dumpResult -notmatch "PGDUMP_EXIT:0") {
    Write-Step "生产库 schema 导出失败: $dumpResult" "ERROR"
    exit 1
}

# 下载到本地
Write-Step "正在下载生产库 schema 文件..." "INFO"
$scpResult = Invoke-Scp $prodSchemaFile $prodRemoteFile
if ($LASTEXITCODE -ne 0) {
    # pscp 方向: 从远程下载到本地，需要调换参数
    # pscp 用法: pscp [options] user@host:source target
    $fullArgs = $sshOpts + @("-scp", "-l", $ProdSshUser) + $pwdArg + @("${ProdHost}:${prodRemoteFile}", $prodSchemaFile)
    & $pscp @fullArgs 2>&1 | Out-Null
}

if (-not (Test-Path $prodSchemaFile)) {
    Write-Step "生产库 schema 文件下载失败" "ERROR"
    exit 1
}

# 清理远程临时文件
Invoke-Ssh "rm -f $prodRemoteFile" | Out-Null

$prodSize = (Get-Item $prodSchemaFile).Length
Write-Step "生产库 schema 导出完成: ${prodSize} bytes" "OK"

# ================================================================
#  步骤 3：apgdiff 对比生成差异 SQL
# ================================================================
Write-Step "步骤 3/5：apgdiff 对比生成差异 SQL" "HEADER"

$diffFile = Join-Path $OutputDir "diff_${Timestamp}.sql"

Write-Step "正在对比 schema..." "INFO"
$apgdiffOutput = & $javaExe -jar $ApgDiffJar $prodSchemaFile $devSchemaFile 2>&1
$apgdiffExit = $LASTEXITCODE

# apgdiff 参数: old(prod) new(dev) → 生成从 prod 到 dev 的变更 SQL
if ($apgdiffExit -ne 0) {
    Write-Step "apgdiff 执行失败 (exit: $apgdiffExit)" "ERROR"
    Write-Step "输出: $apgdiffOutput" "ERROR"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($apgdiffOutput)) {
    Write-Step "开发库与生产库 schema 完全一致，无需更新！" "OK"
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Green
    Write-Host "  数据库 schema 无差异，无需更新" -ForegroundColor Green
    Write-Host "================================================================" -ForegroundColor Green
    exit 0
}

# 写入差异文件
$diffHeader = @"
-- ================================================================
--  EAMS2026 数据库差异 SQL
--  生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
--  源: 生产库 (${ProdHost}:${ProdDbName}) → 目标: 开发库 (${DevHost}:${DevDbName})
--  注意: 请人工审查后再执行！
-- ================================================================

"@

Set-Content -Path $diffFile -Value ($diffHeader + $apgdiffOutput) -Encoding UTF8

$diffLines = ($apgdiffOutput -split "`n").Count
Write-Step "差异 SQL 已生成: $diffFile (${diffLines} 行)" "OK"

# ================================================================
#  步骤 4：显示差异摘要
# ================================================================
Write-Step "步骤 4/5：差异摘要" "HEADER"

Write-Host ""
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Yellow

# 统计差异类型
$createCount = ([regex]::Matches($apgdiffOutput, 'CREATE\s+(TABLE|INDEX|SEQUENCE|FUNCTION|TRIGGER|VIEW|TYPE)', 'IgnoreCase')).Count
$alterCount  = ([regex]::Matches($apgdiffOutput, 'ALTER\s+TABLE', 'IgnoreCase')).Count
$dropCount   = ([regex]::Matches($apgdiffOutput, 'DROP\s+(TABLE|INDEX|SEQUENCE|FUNCTION|TRIGGER|VIEW|TYPE)', 'IgnoreCase')).Count

Write-Host "  差异统计:" -ForegroundColor White
Write-Host "    CREATE: $createCount 处" -ForegroundColor Green
Write-Host "    ALTER:  $alterCount 处" -ForegroundColor Yellow
Write-Host "    DROP:   $dropCount 处" -ForegroundColor Red
Write-Host ""

# 显示差异内容（前 60 行）
Write-Host "  差异 SQL 预览（前 60 行）:" -ForegroundColor White
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
$preview = $apgdiffOutput -split "`n" | Select-Object -First 60
foreach ($line in $preview) {
    $color = "Gray"
    if ($line -match '^\s*CREATE\s') { $color = "Green" }
    elseif ($line -match '^\s*ALTER\s') { $color = "Yellow" }
    elseif ($line -match '^\s*DROP\s') { $color = "Red" }
    elseif ($line -match '^\s*--') { $color = "DarkGray" }
    Write-Host "  $line" -ForegroundColor $color
}
if ($diffLines -gt 60) {
    Write-Host "  ... (共 ${diffLines} 行，完整内容见 $diffFile)" -ForegroundColor DarkGray
}
Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

if ($DiffOnly) {
    Write-Step "仅对比模式，差异文件已保存到: $diffFile" "OK"
    Write-Host ""
    Write-Host "  生产库 schema 文件: $prodSchemaFile" -ForegroundColor Gray
    Write-Host "  开发库 schema 文件: $devSchemaFile" -ForegroundColor Gray
    exit 0
}

# ================================================================
#  步骤 5：确认并应用差异
# ================================================================
Write-Step "步骤 5/5：应用差异到生产库" "HEADER"

if (-not $AutoApply) {
    Write-Host ""
    Write-Host "  ⚠ 警告：即将对生产库 (${ProdHost}:${ProdDbName}) 执行上述差异 SQL！" -ForegroundColor Red
    Write-Host ""

    $confirm = Read-Host -Prompt "  请输入 'YES' 确认执行（其他任意键取消）"
    if ($confirm -ne "YES") {
        Write-Step "用户取消执行" "WARN"
        Write-Host ""
        Write-Host "  差异 SQL 文件已保存: $diffFile" -ForegroundColor Gray
        Write-Host "  可手动执行: psql -h $ProdHost -U $ProdDbUser -d $ProdDbName -f $diffFile" -ForegroundColor Gray
        exit 0
    }
}

# 备份生产库（仅 schema）
Write-Step "正在备份生产库 schema（变更前）..." "INFO"
$backupFile = "/tmp/prod_schema_backup_${Timestamp}.sql"
$backupResult = Invoke-Ssh "PGPASSWORD='$prodPassword' pg_dump -h localhost -p $ProdDbPort -U $ProdDbUser -d $ProdDbName --schema-only --no-owner --no-acl -f $backupFile 2>&1; echo BACKUP_EXIT:`$?"
if ($backupResult -notmatch "BACKUP_EXIT:0") {
    Write-Step "schema 备份失败，但继续执行" "WARN"
} else {
    Write-Step "schema 备份完成: $backupFile" "OK"
}

# 上传差异 SQL 到生产服务器
Write-Step "正在上传差异 SQL 到生产服务器..." "INFO"
$remoteDiffFile = "/tmp/diff_${Timestamp}.sql"
$fullArgs = $sshOpts + @("-scp", "-l", $ProdSshUser) + $pwdArg + @($diffFile, "${ProdHost}:${remoteDiffFile}")
& $pscp @fullArgs 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Step "差异 SQL 上传失败" "ERROR"
    exit 1
}
Write-Step "上传完成" "OK"

# 执行差异 SQL
Write-Step "正在应用差异 SQL 到生产库..." "INFO"
$sqlContent = Get-Content $diffFile -Raw -Encoding UTF8
# 将 SQL 中的单引号转义，以便在 SSH 命令中传递
$escapedSql = $sqlContent -replace "'", "'\''"

# 通过 psql 执行
$applyResult = Invoke-Ssh "PGPASSWORD='$prodPassword' psql -h localhost -p $ProdDbPort -U $ProdDbUser -d $ProdDbName -v ON_ERROR_STOP=1 -f $remoteDiffFile 2>&1; echo APPLY_EXIT:`$?"

# 清理远程文件
Invoke-Ssh "rm -f $remoteDiffFile" | Out-Null

if ($applyResult -match "APPLY_EXIT:0") {
    Write-Step "差异 SQL 已成功应用到生产库！" "OK"
} else {
    Write-Step "差异 SQL 应用过程中出现错误:" "ERROR"
    Write-Host $applyResult -ForegroundColor Red
    Write-Host ""
    Write-Step "生产库 schema 备份: $backupFile" "WARN"
    Write-Step "可回滚: psql -h $ProdHost -U $ProdDbUser -d $ProdDbName -f $backupFile" "WARN"
    exit 1
}

# ================================================================
#  完成
# ================================================================
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  数据库增量更新完成！" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  开发库 schema: $devSchemaFile" -ForegroundColor Gray
Write-Host "  生产库 schema: $prodSchemaFile" -ForegroundColor Gray
Write-Host "  差异 SQL:      $diffFile" -ForegroundColor Gray
Write-Host "  生产库备份:    ${ProdHost}:$backupFile" -ForegroundColor Gray
Write-Host ""
Write-Host "  回滚命令（如需要）:" -ForegroundColor Yellow
Write-Host "    psql -h $ProdHost -U $ProdDbUser -d $ProdDbName -f $backupFile" -ForegroundColor Gray
Write-Host "================================================================" -ForegroundColor Green

# 清理密码变量
$sshPassword = $null; $prodPassword = $null; $devPassword = $null
[System.GC]::Collect()