<#
.SYNOPSIS
    EAMS2026 发布打包脚本 — 构建后端/前端、备份数据库、生成发布包
.DESCRIPTION
    自动读取项目版本号，执行 dotnet publish + npm run build + pg_dump，
    将产物整理到 publish/v{版本号}_{YYYYMMDD_HHmm}/ 目录，生成 README.txt。
.PARAMETER SkipBuild
    跳过构建（仅打包已有产物）
.PARAMETER SkipDatabase
    跳过数据库备份
.PARAMETER PgPassword
    PostgreSQL 密码，默认 postgres
.EXAMPLE
    .\scripts\publish.ps1
    .\scripts\publish.ps1 -SkipBuild
    .\scripts\publish.ps1 -SkipDatabase
#>

param(
    [switch]$SkipBuild,
    [switch]$SkipDatabase,
    [string]$PgPassword = "postgres"
)

# ================================================================
#  路径配置
# ================================================================
$repoRoot = "D:\Codes\Projects\eams2026"
$publishRoot = Join-Path $repoRoot "publish"
$apiProject = Join-Path $repoRoot "src\EAMS2026.Api\EAMS2026.Api.csproj"
$webDir     = Join-Path $repoRoot "src\EAMS2026.Web"
$migrationDir = Join-Path $repoRoot "src\EAMS2026.Infrastructure\Migrations"
$docsDir    = Join-Path $repoRoot "docs"
$tempDir    = Join-Path $publishRoot "temp"
$dotnetExe  = "D:\Scoop\apps\dotnet8-sdk\current\dotnet.exe"
$nodePath   = "D:\Scoop\apps\nodejs-lts\current"
$pgBin      = "D:\Scoop\apps\postgresql\current\bin"

$env:NUGET_PACKAGES = Join-Path $repoRoot ".dotnet\.nuget\packages"

function Write-Step {
    param([string]$Message, [string]$Status = "INFO")
    $colorMap = @{ INFO = "Cyan"; OK = "Green"; WARN = "Yellow"; ERROR = "Red" }
    $c = $colorMap[$Status]
    if (-not $c) { $c = "White" }
    Write-Host ("[{0}] {1}" -f $Status, $Message) -ForegroundColor $c
}

# ================================================================
#  1. 读取项目版本号
# ================================================================
Write-Step "Reading project version..." "HEADER"

$version = $null
$csprojFiles = @()
$csprojFiles += Join-Path $repoRoot "src\EAMS2026.Api\EAMS2026.Api.csproj"
$csprojFiles += Join-Path $repoRoot "src\EAMS2026.Application\EAMS2026.Application.csproj"
$csprojFiles += Join-Path $repoRoot "src\EAMS2026.Infrastructure\EAMS2026.Infrastructure.csproj"
$csprojFiles += Join-Path $repoRoot "src\EAMS2026.Domain\EAMS2026.Domain.csproj"

foreach ($f in $csprojFiles) {
    if (Test-Path $f) {
        $content = Get-Content $f -Raw
        $m = [regex]::Match($content, '<Version>(.+?)</Version>')
        if ($m.Success -and $m.Groups[1].Value) {
            $version = $m.Groups[1].Value.Trim()
            Write-Step "  Found version $version in $([System.IO.Path]::GetFileName($f))" "OK"
            break
        }
    }
}

if (-not $version) {
    Write-Step "Failed to read project version" "ERROR"
    exit 1
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmm"
$publishDirName = "v${version}_${timestamp}"
$publishDir = Join-Path $publishRoot $publishDirName

if (Test-Path $publishDir) {
    Write-Step "Publish dir exists, will overwrite: $publishDir" "WARN"
    Remove-Item "$publishDir\*" -Recurse -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
}

Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  EAMS2026 Publish v$version" -ForegroundColor Cyan
Write-Host "  Output: $publishDir" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# ================================================================
#  2. Build Backend API
# ================================================================
if (-not $SkipBuild) {
    Write-Step "[1/4] Building backend API..." "HEADER"

    $apiOut = Join-Path $publishDir "api"
    if (Test-Path $apiOut) { Remove-Item $apiOut -Recurse -Force -ErrorAction SilentlyContinue }

    & $dotnetExe publish $apiProject -c Release -o $apiOut --self-contained false -p:UseAppHost=false
    if ($LASTEXITCODE -ne 0) {
        Write-Step "Backend build failed" "ERROR"
        exit 1
    }
    Write-Step "  Backend build done" "OK"
} else {
    Write-Step "[1/4] Skipped backend build (-SkipBuild)" "WARN"
}

# ================================================================
#  3. Build Frontend Web
# ================================================================
if (-not $SkipBuild) {
    Write-Step "[2/4] Building frontend Web..." "HEADER"

    $env:Path = "$nodePath;$env:Path"
    Push-Location $webDir
    & npm run build
    $exitCode = $LASTEXITCODE
    Pop-Location

    if ($exitCode -ne 0) {
        Write-Step "Frontend build failed" "ERROR"
        exit 1
    }
    Write-Step "  Frontend build done" "OK"
} else {
    Write-Step "[2/4] Skipped frontend build (-SkipBuild)" "WARN"
}

# ================================================================
#  4. Backup Database
# ================================================================
if (-not $SkipDatabase) {
    Write-Step "[3/4] Backing up PostgreSQL database..." "HEADER"

    $dumpFile = Join-Path $publishDir "database\eams2026_database.dump"
    $pgDumpExe = Join-Path $pgBin "pg_dump.exe"

    if (-not (Test-Path $pgDumpExe)) {
        Write-Step "  pg_dump not found: $pgDumpExe" "WARN"
    } else {
        $env:PGPASSWORD = $PgPassword
        # 假设 $dumpFile 是完整的文件路径，例如 "C:\backups\2026-06-24\eams2026.dump"
        $directory = Split-Path $dumpFile -Parent
        # 如果目录不存在，则创建它（-Force 参数会创建路径中所有不存在的目录）
        New-Item -ItemType Directory -Path $directory -Force

        & $pgDumpExe -h localhost -p 5432 -U postgres -d eams2026 -F c -b -v -f $dumpFile

        Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue

        if ($LASTEXITCODE -eq 0 -and (Test-Path $dumpFile)) {
            $size = "{0:N2} MB" -f ((Get-Item $dumpFile).Length / 1MB)
            Write-Step "  DB backup done: $dumpFile ($size)" "OK"
        } else {
            Write-Step "  DB backup failed (exit code: $LASTEXITCODE)" "WARN"
        }
    }
} else {
    Write-Step "[3/4] Skipped database backup (-SkipDatabase)" "WARN"
}

# ================================================================
#  5. Organize publish files
# ================================================================
Write-Step "[4/4] Organizing publish files..." "HEADER"

$apiTarget    = Join-Path $publishDir "api"
$webTarget    = Join-Path $publishDir "web"
$databaseTarget = Join-Path $publishDir "database"
$docsTarget   = Join-Path $publishDir "docs"

New-Item -ItemType Directory -Path $apiTarget -Force | Out-Null
New-Item -ItemType Directory -Path $webTarget -Force | Out-Null
New-Item -ItemType Directory -Path $databaseTarget -Force | Out-Null
New-Item -ItemType Directory -Path $docsTarget -Force | Out-Null

if (Test-Path $apiTarget) {
    Write-Step "  API files already in place at api/" "OK"
}

$webDist = Join-Path $webDir "dist"
if (Test-Path $webDist) {
    Copy-Item "$webDist\*" $webTarget -Recurse -Force
    Write-Step "  Web -> web/" "OK"
}

if (Test-Path $migrationDir) {
    Copy-Item "$migrationDir\*.sql" $databaseTarget -Force
    Write-Step "  SQL scripts -> database/" "OK"
}

if (Test-Path (Join-Path $publishDir "eams2026_database.dump")) {
    Write-Step "  DB dump included in root" "OK"
}

if (Test-Path $docsDir) {
    Copy-Item "$docsDir\*" $docsTarget -Recurse -Force
    Write-Step "  Docs -> docs" "OK"
}

# 清理临时目录（如存在）
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}

# ================================================================
#  6. Generate README.txt
# ================================================================
Write-Step "Generating README.txt..." "HEADER"

$today = Get-Date -Format "yyyy-MM-dd"
$readmeLines = @(
    "================================================================",
    "  EAMS2026 Enterprise Management System - $publishDirName",
    "================================================================",
    "",
    "Release Date: $today",
    "Version: $version",
    "",
    "---------------------------------------------------------------",
    "Directory Structure",
    "---------------------------------------------------------------",
    "",
    "api/           - Backend API publish files",
    "  EAMS2026.Api.dll      Main program",
    "  appsettings.json      Config file (edit before deploy)",
    "",
    "web/          - Frontend static files",
    "  index.html            Entry page",
    "  assets/               JS/CSS assets",
    "",
    "database/     - Database migration scripts",
    "  xxx_*.sql                Run in numeric order",
    "",
    "docs/         - Documentation",
    "",
    "---------------------------------------------------------------",
    "Quick Deploy (Windows)",
    "---------------------------------------------------------------",
    "",
    "Option 1 (Recommended - deploy script):",
    "  cd D:\Codes\Projects\eams2026",
    "  .\scripts\deploy-production.ps1 -PublishDir publish/$publishDirName",
    "",
    "Option 2 (Manual):",
    "",
    "1. Database",
    "   Restore from backup:",
    "     createdb -h localhost -U postgres eams2026",
    "     pg_restore -h localhost -U postgres -d eams2026 eams2026_database.dump",
    "",
    "   Fresh deploy (structure only):",
    "     createdb -h localhost -U postgres eams2026",
    "     psql -h localhost -U postgres -d eams2026 -f database/001_InitialSchema.sql",
    "     ...(run all sql files in numeric order)",
    "",
    "2. Backend",
    "   Edit api/appsettings.json connection string",
    "   Create Windows service:",
    "     sc.exe create EAMS2026 binPath=`"%CD%\api\EAMS2026.Api.exe`" start=auto",
    "   Or run directly:",
    "     dotnet api/EAMS2026.Api.dll",
    "",
    "3. Frontend",
    "   Copy web/ to Nginx static directory",
    "",
    "---------------------------------------------------------------"
)
Set-Content -Path (Join-Path $publishDir "README.txt") -Value $readmeLines -Encoding ASCII
Write-Step "  README.txt generated" "OK"

# ================================================================
#  7. Update public.md
# ================================================================
$publicMd = Join-Path $publishRoot "public.md"
$publicLines = @(
    "Publish targets",
    "dotnet 192.168.101.31",
    "pg 192.168.101.37",
    "nginx 192.168.101.32",
    "",
    "Latest: $publishDirName ($today)",
    "  -> publish/$publishDirName/"
)
Set-Content -Path $publicMd -Value $publicLines -Encoding UTF8
Write-Step "  public.md updated" "OK"

# ================================================================
#  Done
# ================================================================
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  Publish complete!" -ForegroundColor Green
Write-Host "  Version: $version" -ForegroundColor Green
Write-Host "  Dir: $publishDir" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green

$fileCount = (Get-ChildItem $publishDir -Recurse -File | Measure-Object).Count
Write-Step "  Total $fileCount files" "OK"