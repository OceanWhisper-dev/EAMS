<#
.SYNOPSIS
    EAMS2026 项目一键备份脚本
.DESCRIPTION
    自动备份项目源码、文档、脚本和配置，排除构建产物和依赖。
    支持配置脱敏、数据库备份、版本自动命名。
    可选同步数据库备份到发布包 publish/{Version}/database/。
.PARAMETER Version
    备份版本标识，默认自动生成（如 0.4.2026.0624_20260624_143000）
.PARAMETER BackupDir
    备份输出目录，默认 backups\{Version}
.PARAMETER SkipDatabase
    跳过数据库备份
.PARAMETER SkipMaskConfig
    跳过配置文件脱敏
.PARAMETER UpdatePublishDump
    将数据库备份同步到发布包 publish/{Version}/database/eams2026_database.dump
.PARAMETER PublishDir
    发布包根目录，默认 publish/{Version}，需配合 -UpdatePublishDump 使用
.EXAMPLE
    .\scripts\backup.ps1
    .\scripts\backup.ps1 -Version "0.4.2026.0624_20260624_143000"
    .\scripts\backup.ps1 -SkipDatabase
    .\scripts\backup.ps1 -UpdatePublishDump
    .\scripts\backup.ps1 -Version "0.4.2026.0624_20260624_143000" -UpdatePublishDump -PublishDir "publish/0.4.2026.0624_20260624_143000"
#>

param(
    [string]$Version = "",
    [string]$BackupDir = "",
    [switch]$SkipDatabase,
    [switch]$SkipMaskConfig,
    [switch]$UpdatePublishDump,
    [string]$PublishDir = ""
)

# ---------- 配置 ----------
$ProjectRoot = "D:\Codes\Projects\eams2026"

# 自动生成 Version：项目版本号_当前日期时间
if (-not $Version) {
    $csprojPath = "$ProjectRoot\src\EAMS2026.Api\EAMS2026.Api.csproj"
    if (Test-Path $csprojPath) {
        $projectVersion = (Select-Xml -Path $csprojPath -XPath "//Version" | Select-Object -First 1).Node.InnerText
    }
    if (-not $projectVersion) { $projectVersion = "0.0.0.0" }
    $dateTimeStr = Get-Date -Format 'yyyyMMdd_HHmmss'
    $Version = "${projectVersion}_${dateTimeStr}"
}

if (-not $BackupDir) {
    $BackupDir = "$ProjectRoot\backups\$Version"
}

# 排除列表
$ExcludeDirs = @('bin','obj','node_modules','dist','logs','.dotnet','.dbg','Project_to_be_Imported')
$ExcludeFiles = @('*.tsbuildinfo','package-lock.json','backup_inventory.csv')

# 脱敏键名
$MaskedKeys = @('DefaultConnection','ConnectionString','ConnectionStrings','Jwt.*(Key|Secret|Token)','DingTalk.*(AppKey|AppSecret|Token)','Password','Secret','ApiKey','Hwatt.*(Connection|Password)')
$MaskPattern = '"(' + ($MaskedKeys -join '|') + ')"\s*:\s*".+?"'

# 数据库配置
$PgDump = "D:\Scoop\apps\postgresql\current\bin\pg_dump.exe"
$DbHost = "localhost"
$DbUser = "postgres"
$DbName = "eams2026"

# ---------- 辅助函数 ----------
function Write-Step {
    param([string]$Message, [string]$Status = "INFO")
    $colorMap = @{ INFO = "Cyan"; OK = "Green"; WARN = "Yellow"; ERROR = "Red" }
    $color = $colorMap[$Status]
    if (-not $color) { $color = "White" }
    Write-Host ("[{0}] {1}" -f $Status, $Message) -ForegroundColor $color
}

function Get-FileCount {
    param([string]$Path)
    return (Get-ChildItem -Path $Path -Recurse -File -ErrorAction SilentlyContinue).Count
}

function Get-TotalSize {
    param([string]$Path)
    $size = (Get-ChildItem -Path $Path -Recurse -File -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
    if ($size -gt 1GB) { return "{0:N2} GB" -f ($size / 1GB) }
    if ($size -gt 1MB) { return "{0:N2} MB" -f ($size / 1MB) }
    return "{0:N2} KB" -f ($size / 1KB)
}

# ---------- 主流程 ----------
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  EAMS2026 Project Backup" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor Cyan
Write-Host "  Output: $BackupDir" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Step 0: 检查项目目录
Write-Step "Checking project directory..."
if (-not (Test-Path $ProjectRoot)) {
    Write-Step "Project directory not found: $ProjectRoot" "ERROR"
    exit 1
}
Write-Step "Project directory OK" "OK"

# Step 1: 创建备份目录
Write-Step "Creating backup directory..."
$null = New-Item -ItemType Directory -Path $BackupDir -Force
Write-Step "Backup directory: $BackupDir" "OK"

# Step 2: 构建 robocopy 排除参数
$excludeDirArgs = @()
foreach ($d in $ExcludeDirs) { $excludeDirArgs += "/XD"; $excludeDirArgs += $d }
$excludeFileArgs = @()
foreach ($f in $ExcludeFiles) { $excludeFileArgs += "/XF"; $excludeFileArgs += $f }

# Step 3: 备份源码
Write-Step "Backing up src/ (source code)..."
$null = robocopy "$ProjectRoot\src" "$BackupDir\src" /E $excludeDirArgs $excludeFileArgs /NJH /NJS /NP 2>&1
if ($LASTEXITCODE -ge 8) {
    Write-Step "Source backup may be incomplete - check robocopy output" "WARN"
} else {
    Write-Step ("Source backup complete: {0} files, {1}" -f (Get-FileCount "$BackupDir\src"), (Get-TotalSize "$BackupDir\src")) "OK"
}

# Step 4: 备份文档
Write-Step "Backing up docs/..."
if (Test-Path "$ProjectRoot\docs") {
    $null = robocopy "$ProjectRoot\docs" "$BackupDir\docs" /E /NJH /NJS /NP 2>&1
    Write-Step ("Docs backup complete: {0} files" -f (Get-FileCount "$BackupDir\docs")) "OK"
} else {
    Write-Step "docs/ not found, skipped" "WARN"
}

# Step 5: 备份脚本
Write-Step "Backing up scripts/..."
if (Test-Path "$ProjectRoot\scripts") {
    $null = robocopy "$ProjectRoot\scripts" "$BackupDir\scripts" /E $excludeDirArgs /NJH /NJS /NP 2>&1
    Write-Step ("Scripts backup complete: {0} files" -f (Get-FileCount "$BackupDir\scripts")) "OK"
} else {
    Write-Step "scripts/ not found, skipped" "WARN"
}

# Step 6: 备份 IDE 配置
Write-Step "Backing up .trae/ (IDE config)..."
if (Test-Path "$ProjectRoot\.trae") {
    $null = robocopy "$ProjectRoot\.trae" "$BackupDir\.trae" /E /NJH /NJS /NP 2>&1
    Write-Step ".trae/ backup complete" "OK"
} else {
    Write-Step ".trae/ not found, skipped" "WARN"
}

Write-Step "Backing up .vscode/ (VSCode config)..."
if (Test-Path "$ProjectRoot\.vscode") {
    $null = robocopy "$ProjectRoot\.vscode" "$BackupDir\.vscode" /E /NJH /NJS /NP 2>&1
    Write-Step ".vscode/ backup complete" "OK"
} else {
    Write-Step ".vscode/ not found, skipped" "WARN"
}

# Step 7: 备份并脱敏配置文件
if (-not $SkipMaskConfig) {
    Write-Step "Backing up config files (masking sensitive values)..."
    $configFiles = @('appsettings.json', 'appsettings.Development.json')
    foreach ($cfgFile in $configFiles) {
        $srcFile = "$ProjectRoot\src\EAMS2026.Api\$cfgFile"
        $destFile = "$BackupDir\src\EAMS2026.Api\$cfgFile"
        if (Test-Path $srcFile) {
            $destDir = Split-Path $destFile -Parent
            if (-not (Test-Path $destDir)) { $null = New-Item -ItemType Directory -Path $destDir -Force }
            $content = Get-Content $srcFile -Raw
            $masked = $content -replace $MaskPattern, ('"$1":"***MASKED***"')
            [System.IO.File]::WriteAllText($destFile, $masked, [System.Text.UTF8Encoding]::new($false))
            Write-Step "  Masked: $cfgFile" "OK"
        }
    }
} else {
    Write-Step "Skipping config masking" "WARN"
}

# Step 8: 数据库备份
if (-not $SkipDatabase) {
    Write-Step "Backing up database..."
    $dbBackupFile = "$BackupDir\eams2026_database.backup"
    if (Test-Path $PgDump) {
        try {
            $null = & $PgDump -h $DbHost -U $DbUser -d $DbName -F c -f $dbBackupFile 2>&1
            if ($LASTEXITCODE -eq 0) {
                $dbItem = Get-Item $dbBackupFile
                Write-Step ("Database backup complete: {0:N2} MB" -f ($dbItem.Length / 1MB)) "OK"

                # 同步数据库备份到发布包（如果指定）
                if ($UpdatePublishDump) {
                    if (-not $PublishDir) { $PublishDir = "publish/$Version" }
                    $publishDbDir = "$ProjectRoot\$PublishDir\database"
                    if (Test-Path $publishDbDir) {
                        $publishDumpPath = "$publishDbDir\eams2026_database.dump"
                        Copy-Item -Force $dbBackupFile $publishDumpPath
                        Write-Step ("Publish dump synced: {0}\database\eams2026_database.dump ({1:N2} MB)" -f $PublishDir, ((Get-Item $publishDumpPath).Length / 1MB)) "OK"
                        Write-Step "  -> 部署时可用: pg_restore -h localhost -U postgres -d eams2026 database/eams2026_database.dump" "INFO"
                    } else {
                        Write-Step "Publish directory not found: $publishDbDir, skipping sync" "WARN"
                    }
                }
            } else {
                Write-Step "Database backup failed - check pg_dump parameters" "ERROR"
            }
        } catch {
            Write-Step ("Database backup error: {0}" -f $_) "ERROR"
        }
    } else {
        Write-Step "pg_dump not found, skipping database backup" "WARN"
    }
} else {
    Write-Step "Skipping database backup" "WARN"
}

# Step 9: 生成备份清单
Write-Step "Generating backup inventory..."
$inventoryPath = "$BackupDir\backup_inventory.csv"
Get-ChildItem $BackupDir -Recurse -File | Select-Object FullName, Length, @{N='LastWriteTime';E={$_.LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')}} | Export-Csv $inventoryPath -NoTypeInformation -Encoding UTF8
Write-Step "Backup inventory: $inventoryPath" "OK"

# Step 10: 完整性检查
Write-Host ""
$issues = @()
$totalFiles = (Get-ChildItem $BackupDir -Recurse -File | Where-Object { $_.FullName -notlike "*\backup_inventory.csv" }).Count

$keyFiles = @("src\EAMS2026.sln","src\EAMS2026.Api\Program.cs","src\EAMS2026.Web\src\App.vue","src\EAMS2026.Web\package.json")
foreach ($kf in $keyFiles) {
    if (-not (Test-Path "$BackupDir\$kf")) { $issues += "MISSING key file: $kf" }
}

$checkDirs = @('node_modules','bin','obj')
foreach ($cd in $checkDirs) {
    $found = (Get-ChildItem $BackupDir -Recurse -Directory -Filter $cd -ErrorAction SilentlyContinue).Count
    if ($found -gt 0) { $issues += "WARNING: backup contains $cd/ directory" }
}

if (-not $SkipMaskConfig) {
    $cfgFiles = Get-ChildItem "$BackupDir\src" -Recurse -Filter "appsettings*.json" -ErrorAction SilentlyContinue
    foreach ($cfg in $cfgFiles) {
        $text = Get-Content $cfg.FullName -Raw
        if ($text -match '"Password"\s*:\s*"[^*]') { $issues += "WARNING: $($cfg.Name) may not be masked" }
    }
}

Write-Step ("Total files: {0}, Total size: {1}" -f $totalFiles, (Get-TotalSize $BackupDir)) "INFO"
if ($issues.Count -eq 0) {
    Write-Step "All integrity checks passed" "OK"
} else {
    Write-Step "Issues found:" "WARN"
    foreach ($issue in $issues) { Write-Step "  - $issue" "WARN" }
}

# Step 11: 备份摘要
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Backup Complete!" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ("  Version:   {0}" -f $Version) -ForegroundColor Green
Write-Host ("  Location:  {0}" -f $BackupDir) -ForegroundColor Green
Write-Host ("  Files:     {0}" -f $totalFiles) -ForegroundColor Green
Write-Host ("  Size:      {0}" -f (Get-TotalSize $BackupDir)) -ForegroundColor Green
if ((-not $SkipDatabase) -and (Test-Path "$BackupDir\eams2026_database.backup")) {
    $dbFile = Get-Item "$BackupDir\eams2026_database.backup"
    Write-Host ("  Database:  {0:N2} MB" -f ($dbFile.Length / 1MB)) -ForegroundColor Green
    if ($UpdatePublishDump) {
        Write-Host ("  Publish:   synced to {0}\database\eams2026_database.dump" -f $PublishDir) -ForegroundColor Green
    }
}
Write-Host ""
Write-Host "Restore example:" -ForegroundColor Yellow
Write-Host ("  robocopy $BackupDir\src D:\Codes\Projects\eams2026\src /E") -ForegroundColor Gray
if ((-not $SkipDatabase) -and (Test-Path "$BackupDir\eams2026_database.backup")) {
    Write-Host ("  pg_restore -h localhost -U postgres -d eams2026 `"$BackupDir\eams2026_database.backup`"") -ForegroundColor Gray
}
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan