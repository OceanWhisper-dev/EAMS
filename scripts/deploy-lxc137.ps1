﻿<#
.SYNOPSIS
    部署 lxc137 (192.168.101.37) — PostgreSQL 数据库
.DESCRIPTION
    备份当前数据库 → 导入新备份 → 验证。
    支持两种模式：
      - 全量模式（默认）：删除旧库 → 创建新库 → pg_restore 恢复
      - 增量模式（-Incremental）：对比 dump 文件与生产库 schema，仅应用差异 DDL，不删除数据库

    每个 LXC 独立脚本，支持独立密码。
.PARAMETER PublishDir
    发布包目录，默认 publish/v0.4.260618
.PARAMETER Host
    目标服务器 IP，默认 192.168.101.37
.PARAMETER DbName
    数据库名，默认 eams2026
.PARAMETER DbPort
    PostgreSQL 端口，默认 5432
.PARAMETER BackupOnly
    仅备份数据库，不恢复
.PARAMETER SkipBackup
    跳过备份（直接恢复）
.PARAMETER Incremental
    增量更新模式：对比 dump 文件与生产库 schema，仅应用差异，不删除数据库
.PARAMETER AcceptHostKeys
    自动接受主机密钥
.EXAMPLE
    .\scripts\deploy-lxc137.ps1
    .\scripts\deploy-lxc137.ps1 -BackupOnly
    .\scripts\deploy-lxc137.ps1 -Incremental
.NOTES
    依赖 PuTTY (plink.exe + pscp.exe)。
    增量模式额外依赖 Java (apgdiff.jar)。
    回滚：pg_restore -U postgres -d eams2026 /var/backups/eams/eams2026_before_deploy_YYYYMMDD_HHMMSS.dump
#>

param(
    [string]$PublishDir = "publish/v0.4.260618",
    [string]$Hostname = "192.168.101.37",
    [string]$DbName = "eams2026",
    [int]$DbPort = 5432,
    [switch]$BackupOnly,
    [switch]$SkipBackup,
    [switch]$Incremental,
    [switch]$AcceptHostKeys
)

$Config = @{
    SshUser    = "root"
    BackupRoot = "/db/backups/eams"
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
Write-Host "  EAMS2026 — 部署 lxc137 (PostgreSQL)" -ForegroundColor Cyan
Write-Host "  Target: ${Hostname}:${DbPort}" -ForegroundColor Cyan
Write-Host "  DB:     ${DbName}" -ForegroundColor Cyan
Write-Host "  Pkg:    $PublishDir" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

$dumpFile = "$PublishDir/database/eams2026_database.dump"
if (-not $BackupOnly -and -not (Test-Path $dumpFile)) {
    Write-Step "数据库备份文件不存在: $dumpFile" "ERROR"
    Write-Step "请先生成发布包（含数据库 dump）" "WARN"
    exit 1
}

# 选择模式
if (-not $BackupOnly -and -not $Incremental) {
    Write-Host "请选择部署模式:" -ForegroundColor Yellow
    Write-Host "  [1] 全量恢复 - 删除旧库并重建（推荐首次部署）" -ForegroundColor White
    Write-Host "  [2] 增量更新 - 仅应用 schema 差异（推荐更新部署）" -ForegroundColor White
    Write-Host "  [3] 仅备份 - 仅备份当前数据库" -ForegroundColor White
    Write-Host ""
    $choice = Read-Host "请输入选项 (1/2/3)"
    switch ($choice) {
        "1" { Write-Step "已选择: 全量恢复模式" "OK" }
        "2" { $Incremental = $true; Write-Step "已选择: 增量更新模式" "OK" }
        "3" { $BackupOnly = $true; Write-Step "已选择: 仅备份模式" "OK" }
        default { Write-Step "无效选项，默认全量恢复" "WARN" }
    }
    Write-Host ""
}

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
    param([string]$LocalPath, [string]$RemotePath)
    $fullArgs = $sshOpts + @("-scp", "-l", $sshUser) + $pwdArg + @($LocalPath, "${Hostname}:${RemotePath}")
    & $pscp @fullArgs 2>&1
}

$plainPwd = Get-SshPassword
$pwdArg = @("-pw", $plainPwd)

Write-Step "测试 SSH 连接..." "INFO"
$test = Invoke-Ssh "echo OK"
# plink 输出可能是多行数组，转成单字符串以确保 -match 能填充 $Matches
$testStr = $test -join "`n"
if ($testStr -match "^OK$") {
    Write-Step "连接成功" "OK"
} elseif ($testStr -match "host key is not cached") {
    Write-Step "主机密钥未缓存，正在自动接受..." "WARN"
    $hostKey = $null
    if ($testStr -match "ssh-\S+\s+\d+\s+(SHA256:\S+)") {
        $hostKey = "ssh-ed25519 255 " + $Matches[1]
    } elseif ($testStr -match "SHA256:\S+") {
        $hostKey = "ssh-ed25519 255 " + $Matches[0]
    } elseif ($testStr -match "(ssh-\S+\s+\d+\s+\S+)") {
        $hostKey = $Matches[1]
    }
    if ($hostKey) {
        Write-Step "密钥指纹: $hostKey" "INFO"
        $sshOpts += "-hostkey", $hostKey
        $test = Invoke-Ssh "echo OK"
        if ($test -join "`n" -match "^OK$") { Write-Step "连接成功" "OK" }
        else { Write-Step "连接失败" "ERROR"; Write-Host "plink 输出: $test" -ForegroundColor Red; exit 1 }
    } else {
        Write-Step "无法提取密钥指纹，连接失败" "ERROR"
        Write-Host "plink 输出: $test" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Step "连接失败" "ERROR"; Write-Host "plink 输出: $test" -ForegroundColor Red; exit 1
}

# ── 创建备份目录 ──
Invoke-Ssh "mkdir -p $($Config.BackupRoot)" | Out-Null

# ── 步骤 1：备份当前数据库 ──
$backupSuffix = (Get-Date -Format "yyyyMMdd_HHmmss")
$remoteBackup = "$($Config.BackupRoot)/${DbName}_before_deploy_${backupSuffix}.dump"

if (-not $SkipBackup) {
    Write-Step "步骤 1/3：备份当前数据库 → ${DbName}_before_deploy_${backupSuffix}.dump" "HEADER"
    $pgTest = Invoke-Ssh "sudo -u postgres psql -d $DbName -c 'SELECT 1' -t 2>&1; echo EXIT:`$?"
    Write-Step "数据库检测: exit=$(("$pgTest") -replace '.*EXIT:','')" "INFO"
    $dbExists = if (("$pgTest") -match "EXIT:0") { "YES" } else { "" }
    if ($dbExists -eq "YES") {
        Invoke-Ssh "sudo -u postgres pg_dump -F c $DbName -f $remoteBackup 2>/dev/null; echo 'DONE'" | Out-Null
        Write-Step "数据库备份完成" "OK"
    } else {
        Write-Step "数据库 $DbName 不存在，跳过备份" "WARN"
    }
}

if ($BackupOnly) {
    Write-Step "仅备份模式完成。备份文件: ${remoteBackup}" "OK"
    Write-Host ""
    Write-Host "回滚命令:" -ForegroundColor Yellow
    Write-Host "  pg_restore -U postgres -d $DbName ${remoteBackup}" -ForegroundColor Gray
    $plainPwd = $null; [System.GC]::Collect()
    exit 0
}

# ── 步骤 2：传输 dump 并恢复 ──
if ($Incremental) {
    Write-Step "步骤 2/4：增量更新 — 对比 schema 差异" "HEADER"

    # 检查 apgdiff
    $apgdiffJar = Join-Path $PSScriptRoot "apgdiff.jar"
    $apgdiffUrl  = "https://github.com/fordfrog/apgdiff/releases/download/2.5.0-alpha.2/apgdiff-2.5.0-SNAPSHOT.jar"
    if (-not (Test-Path $apgdiffJar)) {
        Write-Step "apgdiff 未找到，正在下载..." "WARN"
        try {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            Invoke-WebRequest -Uri $apgdiffUrl -OutFile $apgdiffJar -UseBasicParsing -MaximumRedirection 5
            Write-Step "apgdiff 下载完成" "OK"
        } catch { Write-Step "apgdiff 下载失败: $_" "ERROR"; exit 1 }
    }
    $javaExe = (Get-Command java -ErrorAction SilentlyContinue).Source
    if (-not $javaExe) { Write-Step "增量模式需要 Java 运行环境" "ERROR"; exit 1 }

    # 传输 dump 文件到远程
    $remoteDump = "$($Config.BackupRoot)/eams2026_database_deploy.dump"
    Write-Step "正在传输数据库备份文件..." "INFO"
    $scpResult = Invoke-Scp (Resolve-Path $dumpFile) $remoteDump
    if ($LASTEXITCODE -ne 0) { Write-Step "传输失败: $scpResult" "ERROR"; exit 1 }
    Write-Step "传输完成" "OK"

    # 从 dump 文件提取 schema
    Write-Step "正在从 dump 文件提取 schema..." "INFO"
    $dumpSchemaRemote = "/tmp/dump_schema_${backupSuffix}.sql"
    Invoke-Ssh "sudo -u postgres pg_restore --schema-only --no-owner --no-acl --no-comments -f $dumpSchemaRemote $remoteDump 2>&1; echo SCHEMA_EXIT:`$?" | Out-Null

    # 导出生产库当前 schema
    Write-Step "正在导出生产库 schema..." "INFO"
    $prodSchemaRemote = "/tmp/prod_schema_${backupSuffix}.sql"
    Invoke-Ssh "sudo -u postgres pg_dump -d $DbName --schema-only --no-owner --no-acl --no-comments -f $prodSchemaRemote 2>&1; echo PRODDUMP_EXIT:`$?" | Out-Null

    # 下载两个 schema 文件到本地
    $localDumpSchema = Join-Path $env:TEMP "dump_schema_${backupSuffix}.sql"
    $localProdSchema = Join-Path $env:TEMP "prod_schema_${backupSuffix}.sql"

    Write-Step "正在下载 schema 文件..." "INFO"
    $fullArgs = $sshOpts + @("-scp", "-l", $Config.SshUser) + $pwdArg + @("${Hostname}:${dumpSchemaRemote}", $localDumpSchema)
    & $pscp @fullArgs 2>&1 | Out-Null
    $fullArgs = $sshOpts + @("-scp", "-l", $Config.SshUser) + $pwdArg + @("${Hostname}:${prodSchemaRemote}", $localProdSchema)
    & $pscp @fullArgs 2>&1 | Out-Null

    # 清理远程临时文件
    Invoke-Ssh "rm -f $dumpSchemaRemote $prodSchemaRemote" | Out-Null

    if (-not (Test-Path $localDumpSchema) -or -not (Test-Path $localProdSchema)) {
        Write-Step "schema 文件下载失败" "ERROR"; exit 1
    }

    # 清理 schema 文件，移除 psql 命令（apgdiff 不支持）
    Write-Step "正在清理 schema 文件..." "INFO"
    (Get-Content $localDumpSchema) | Where-Object { $_ -notmatch '^\\' } | Set-Content $localDumpSchema -Encoding UTF8
    (Get-Content $localProdSchema) | Where-Object { $_ -notmatch '^\\' } | Set-Content $localProdSchema -Encoding UTF8

    # apgdiff 对比：old=prod, new=dump → 生成从 prod 到 dump 的变更 SQL
    Write-Step "正在对比 schema 差异..." "INFO"
    $apgdiffOutput = & $javaExe -jar $apgdiffJar $localProdSchema $localDumpSchema 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Step "apgdiff 执行失败: $apgdiffOutput" "ERROR"; exit 1
    }

    if ([string]::IsNullOrWhiteSpace($apgdiffOutput)) {
        Write-Step "生产库与发布包 schema 完全一致，无需更新" "OK"
        Invoke-Ssh "rm -f $remoteDump" | Out-Null
        Remove-Item $localDumpSchema, $localProdSchema -Force -ErrorAction SilentlyContinue
    } else {
        # 统计差异
        $createCount = ([regex]::Matches($apgdiffOutput, 'CREATE\s+(TABLE|INDEX|SEQUENCE|FUNCTION|TRIGGER|VIEW|TYPE)', 'IgnoreCase')).Count
        $alterCount  = ([regex]::Matches($apgdiffOutput, 'ALTER\s+TABLE', 'IgnoreCase')).Count
        $dropCount   = ([regex]::Matches($apgdiffOutput, 'DROP\s+(TABLE|INDEX|SEQUENCE|FUNCTION|TRIGGER|VIEW|TYPE)', 'IgnoreCase')).Count

        Write-Step "差异统计: CREATE=$createCount, ALTER=$alterCount, DROP=$dropCount" "INFO"

        # 显示差异预览
        Write-Host ""
        Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Yellow
        Write-Host "  差异 SQL 预览（前 40 行）:" -ForegroundColor White
        Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
        $preview = $apgdiffOutput -split "`n" | Select-Object -First 40
        foreach ($line in $preview) {
            $color = "Gray"
            if ($line -match '^\s*CREATE\s') { $color = "Green" }
            elseif ($line -match '^\s*ALTER\s') { $color = "Yellow" }
            elseif ($line -match '^\s*DROP\s') { $color = "Red" }
            Write-Host "  $line" -ForegroundColor $color
        }
        $totalLines = ($apgdiffOutput -split "`n").Count
        if ($totalLines -gt 40) {
            Write-Host "  ... (共 ${totalLines} 行)" -ForegroundColor DarkGray
        }
        Write-Host "────────────────────────────────────────────────────────────────" -ForegroundColor Gray
        Write-Host ""

        # 确认执行
        Write-Host "  ⚠ 警告：即将对生产库 (${Hostname}:${DbName}) 执行上述差异 SQL！" -ForegroundColor Red
        $confirm = Read-Host -Prompt "  请输入 'YES' 确认执行（其他任意键取消）"
        if ($confirm -ne "YES") {
            Write-Step "用户取消执行，增量更新已跳过" "WARN"
            Invoke-Ssh "rm -f $remoteDump" | Out-Null
            Remove-Item $localDumpSchema, $localProdSchema -Force -ErrorAction SilentlyContinue
            $plainPwd = $null; [System.GC]::Collect()
            exit 0
        }

        # 上传差异 SQL 并执行
        Write-Step "步骤 3/4：应用差异 SQL" "HEADER"
        $localDiffFile = Join-Path $env:TEMP "diff_${backupSuffix}.sql"
        $diffHeader = @"
-- EAMS2026 增量更新 SQL
-- 生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
-- 从生产库 → 发布包 schema 差异

"@
        Set-Content -Path $localDiffFile -Value ($diffHeader + $apgdiffOutput) -Encoding UTF8

        $remoteDiffFile = "/tmp/diff_${backupSuffix}.sql"
        Write-Step "正在上传差异 SQL..." "INFO"
        $fullArgs = $sshOpts + @("-scp", "-l", $Config.SshUser) + $pwdArg + @($localDiffFile, "${Hostname}:${remoteDiffFile}")
        & $pscp @fullArgs 2>&1 | Out-Null

        # 执行差异 SQL
        Write-Step "正在应用差异 SQL..." "INFO"
        $applyResult = Invoke-Ssh "sudo -u postgres psql -d $DbName -v ON_ERROR_STOP=1 -f $remoteDiffFile 2>&1; echo APPLY_EXIT:`$?"
        if (("$applyResult") -match "APPLY_EXIT:0") {
            Write-Step "差异 SQL 应用成功" "OK"
        } else {
            Write-Step "差异 SQL 应用失败: $applyResult" "ERROR"
            Write-Step "生产库备份: ${remoteBackup}" "WARN"
            Write-Step "可回滚: pg_restore -U postgres -d $DbName ${remoteBackup}" "WARN"
            exit 1
        }

        # 清理
        Invoke-Ssh "rm -f $remoteDump $remoteDiffFile" | Out-Null
        Remove-Item $localDumpSchema, $localProdSchema, $localDiffFile -Force -ErrorAction SilentlyContinue
    }
} else {
    # ── 全量模式：删除旧库 → 创建新库 → pg_restore 恢复 ──
    Write-Step "步骤 2/3：全量恢复 — 删除旧库并重建" "HEADER"

    # 传输 dump 文件到远程
    $remoteDump = "$($Config.BackupRoot)/eams2026_database_deploy.dump"
    Write-Step "正在传输数据库备份文件..." "INFO"
    $scpResult = Invoke-Scp (Resolve-Path $dumpFile) $remoteDump
    if ($LASTEXITCODE -ne 0) { Write-Step "传输失败: $scpResult" "ERROR"; exit 1 }
    Write-Step "传输完成" "OK"

    # 恢复数据库
    $pgTest = Invoke-Ssh "sudo -u postgres psql -d $DbName -c 'SELECT 1' -t 2>&1; echo EXIT:`$?"
    Write-Step "数据库检测: exit=$(("$pgTest") -replace '.*EXIT:','')" "INFO"
    $dbExists = if (("$pgTest") -match "EXIT:0") { "YES" } else { "" }

    if ($dbExists -eq "YES") {
        Write-Step "数据库 $DbName 已存在，将删除后重建" "WARN"
        Write-Step "正在删除旧数据库..." "INFO"
        Invoke-Ssh "sudo -u postgres psql -d postgres -c `"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$DbName' AND pid <> pg_backend_pid();`" 2>/dev/null" | Out-Null
        $dropResult = Invoke-Ssh "sudo -u postgres dropdb --if-exists $DbName 2>&1; echo DROP_EXIT:`$?"
        if (("$dropResult") -notmatch "DROP_EXIT:0") {
            Write-Step "删除数据库失败: $dropResult" "ERROR"; exit 1
        }
        Write-Step "旧数据库已删除" "OK"
        Start-Sleep -Seconds 2
    }
    Write-Step "创建数据库 $DbName ..." "INFO"
    $createResult = Invoke-Ssh "sudo -u postgres createdb $DbName 2>&1; echo CREATE_EXIT:`$?"
    if (("$createResult") -notmatch "CREATE_EXIT:0") {
        Write-Step "创建数据库失败: $createResult" "ERROR"; exit 1
    }
    Write-Step "数据库创建成功" "OK"
    # 等待并验证数据库已就绪
    Start-Sleep -Seconds 1
    $verifyDb = Invoke-Ssh "sudo -u postgres psql -d $DbName -c 'SELECT 1' 2>&1; echo VERIFY_EXIT:`$?"
    if (("$verifyDb") -notmatch "VERIFY_EXIT:0") {
        Write-Step "数据库验证失败: $verifyDb" "ERROR"; exit 1
    }
    Write-Step "正在还原..." "INFO"
    $result = Invoke-Ssh "sudo -u postgres pg_restore -d $DbName --no-owner --no-acl ${remoteDump} 2>&1; echo RESTORE_EXIT:`$?"
    if (("$result") -match "RESTORE_EXIT:0") { Write-Step "数据库恢复成功" "OK" }
    else { Write-Step "数据库恢复失败（部分错误可忽略，查看上方输出）: $result" "ERROR"; exit 1 }

    # 清理远程临时文件
    Invoke-Ssh "rm -f ${remoteDump}" | Out-Null
}

# ── 验证 ──
$stepNum = if ($Incremental) { "4/4" } else { "3/3" }
Write-Step "步骤 ${stepNum}：验证数据库" "HEADER"
$psqlCmd = "sudo -u postgres psql -d $DbName -c `"SELECT count(*) FROM information_schema.tables WHERE table_schema='public';`" -t"
$tableCount = Invoke-Ssh $psqlCmd
Write-Step "数据库 $DbName 包含 $($tableCount.Trim()) 张表" "OK"

# ── 完成 ──
Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "  lxc137 部署完成" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host "  备份:  ${remoteBackup}" -ForegroundColor Gray
Write-Host "  回滚:  pg_restore -U postgres -d $DbName ${remoteBackup}" -ForegroundColor Yellow
Write-Host "==============================================" -ForegroundColor Green

$plainPwd = $null; [System.GC]::Collect()