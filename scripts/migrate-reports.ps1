<#
.SYNOPSIS
    报表数据迁移脚本：从 EAMS_4.6 SQL Server → EAMS2026 PostgreSQL
.DESCRIPTION
    迁移以下表:
    - Reports          → rpt_report
    - report_Class     → rpt_category
    - report_Fields    → rpt_field
    - report_Filter    → rpt_filter
    - report_Order     → rpt_sort
    - report_Permissions → rpt_permission
    - report_PivotView → rpt_chart (部分映射)
#>

$ErrorActionPreference = "Stop"

# ====== 连接参数 ======
$srcConnStr = "Server=db;Database=AppData;User ID=sa;Password=dfl_DB;TrustServerCertificate=True"
$dstConnStr = "Host=localhost;Port=5432;Database=eams2026;Username=postgres;Password=postgres"

# ====== 加载 ADO.NET 程序集 ======
Add-Type -AssemblyName System.Data
[System.Reflection.Assembly]::LoadWithPartialName("System.Data.SqlClient") | Out-Null

# ====== 读取 SQL Server 数据 ======
Write-Host "=== 1. 读取 SQL Server 数据 ===" -ForegroundColor Cyan
$srcConn = New-Object System.Data.SqlClient.SqlConnection($srcConnStr)
$srcConn.Open()

function Read-Table($sql) {
    $cmd = $srcConn.CreateCommand()
    $cmd.CommandText = $sql
    $da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $dt = New-Object System.Data.DataTable
    $da.Fill($dt) | Out-Null
    return $dt
}

$dtReports   = Read-Table "SELECT * FROM Reports ORDER BY reportID"
$dtClasses   = Read-Table "SELECT * FROM report_Class ORDER BY clsID"
$dtModules   = Read-Table "SELECT * FROM report_Module ORDER BY moduleID"
$dtFields    = Read-Table "SELECT * FROM report_Fields ORDER BY reportID, fieldIndex"
$dtFilters   = Read-Table "SELECT * FROM report_Filter ORDER BY reportID, filterIndex"
$dtOrders    = Read-Table "SELECT * FROM report_Order ORDER BY reportID, orderIndex"
$dtPerms     = Read-Table "SELECT * FROM report_Permissions ORDER BY reportID, AutoID"
$dtPivots    = Read-Table "SELECT * FROM report_PivotView ORDER BY reportID"

$srcConn.Close()

Write-Host "  Reports: $($dtReports.Rows.Count) 条"
Write-Host "  Classes: $($dtClasses.Rows.Count) 条"
Write-Host "  Modules: $($dtModules.Rows.Count) 条"
Write-Host "  Fields:  $($dtFields.Rows.Count) 条"
Write-Host "  Filters: $($dtFilters.Rows.Count) 条"
Write-Host "  Orders:  $($dtOrders.Rows.Count) 条"
Write-Host "  Perms:   $($dtPerms.Rows.Count) 条"
Write-Host "  Pivots:  $($dtPivots.Rows.Count) 条"

# ====== 2. 生成 PG INSERT SQL ======
Write-Host "`n=== 2. 生成 PostgreSQL 迁移 SQL ===" -ForegroundColor Cyan

$sb = New-Object System.Text.StringBuilder
$sb.AppendLine("-- ============================================================")
$sb.AppendLine("-- 报表数据迁移脚本")
$sb.AppendLine("-- 由 EAMS_4.6 SQL Server → EAMS2026 PostgreSQL")
$sb.AppendLine("-- 生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$sb.AppendLine("-- ============================================================")
$sb.AppendLine("BEGIN;")
$sb.AppendLine("")

# -- 2a. 清空旧数据（迁移用） --
$sb.AppendLine("-- 清空旧数据")
$sb.AppendLine("DELETE FROM rpt_chart;")
$sb.AppendLine("DELETE FROM rpt_field_permission;")
$sb.AppendLine("DELETE FROM rpt_permission;")
$sb.AppendLine("DELETE FROM rpt_filter;")
$sb.AppendLine("DELETE FROM rpt_sort;")
$sb.AppendLine("DELETE FROM rpt_bookmark;")
$sb.AppendLine("DELETE FROM rpt_execution_log;")
$sb.AppendLine("DELETE FROM rpt_field;")
$sb.AppendLine("DELETE FROM rpt_report;")
$sb.AppendLine("DELETE FROM rpt_category;")
$sb.AppendLine("")

# -- 2b. 分类 --
$sb.AppendLine("-- 插入分类（report_Class + report_Module 合并）")
$sb.AppendLine("SELECT setval('rpt_category_id_seq', 1, false);")
$catMap = @{}  # clsID → new category id
$catSeq = 0

# 先插入模块（作为父分类）
$modMap = @{}  # moduleID → new category id
foreach ($row in $dtModules.Rows) {
    $catSeq++
    $mid = [int]$row.moduleID
    $mname = $row.Name
    $sb.AppendFormat("INSERT INTO rpt_category (id, name, sort_order) VALUES ({0}, '{1}', {2});`n", $catSeq, $mname, $mid * 10)
    $modMap[$mid] = $catSeq
}

# 再插入类（作为子分类）
foreach ($row in $dtClasses.Rows) {
    $catSeq++
    $cid = [int]$row.clsID
    $cname = $row.Name
    # 为每个模块下都创建一个同名子分类（old clsID→new cat ID 用负值标记，后面按 module+class 组合）
    $sb.AppendFormat("INSERT INTO rpt_category (id, name, parent_id, sort_order) VALUES ({0}, '{1}', NULL, {2});`n", $catSeq, $cname, $cid * 10)
    $catMap[$cid] = $catSeq
}

# 为每个(module, class)组合创建具体分类条目
$combMap = @{} # "moduleID_clsID" → new category id
foreach ($modRow in $dtModules.Rows) {
    $mid = [int]$modRow.moduleID
    $mname = $modRow.Name
    foreach ($clsRow in $dtClasses.Rows) {
        $cid = [int]$clsRow.clsID
        $cname = $clsRow.Name
        $catSeq++
        $key = "$mid`_$cid"
        $combMap[$key] = $catSeq
        $parentId = $modMap[$mid]
        $sb.AppendFormat("INSERT INTO rpt_category (id, name, parent_id, sort_order) VALUES ({0}, '{1}-{2}', {3}, {4});`n", $catSeq, $mname, $cname, $parentId, $catSeq)
    }
}
$sb.AppendFormat("SELECT setval('rpt_category_id_seq', {0}, true);`n", $catSeq)
$sb.AppendLine("")

# -- 2c. 报表 --
$sb.AppendLine("-- 插入报表")
$rptIdMap = @{}  # old reportID → new auto id (keep same since we override)
$oldReportIDs = New-Object System.Collections.ArrayList
foreach ($row in $dtReports.Rows) {
    $rid = [int]$row.reportID
    $name = ($row.Name -replace "'", "''")
    $title = ($row.Title -replace "'", "''")
    $desc = if ($row.Description -and $row.Description -ne [DBNull]::Value) { ($row.Description -replace "'", "''") } else { "" }
    $queryBase = ($row.QueryBase -replace "'", "''")
    $clsID = [int]$row.clsID
    $moduleID = [int]$row.moduleID
    $combKey = "$moduleID`_$clsID"
    $catId = if ($combMap.ContainsKey($combKey)) { $combMap[$combKey] } else { "NULL" }
    $isProc = if ($queryBase.TrimStart().StartsWith("exec") -or $queryBase.TrimStart().StartsWith("dflproc")) { "'proc'" } else { "'sql'" }
    $isProcessWithParams = if ($row.isProcessWithParams -and $row.isProcessWithParams -ne [DBNull]::Value) { [bool]$row.isProcessWithParams } else { $false }
    $queryText = $queryBase
    if ($isProc -eq "'proc'") {
        $queryText = $queryBase  # proc name
    }
    
    $sb.AppendFormat("INSERT INTO rpt_report (id, name, title, description, category_id, query_type, query_text, query_datasource, is_system, status, is_deleted, created_at, created_by, updated_at, updated_by) VALUES ({0}, '{1}', '{2}', '{3}', {4}, {5}, '{6}', 'main', FALSE, 'published', FALSE, NOW(), 1, NOW(), 1);`n",
        $rid, $name, $title, $desc, $catId, $isProc, $queryText.Replace("'", "''"))
    
    $rptIdMap[$rid] = $rid
    [void]$oldReportIDs.Add($rid)
}
$sb.AppendFormat("SELECT setval('rpt_report_id_seq', {0}, true);`n", $(if ($dtReports.Rows.Count -gt 0) { [int]$dtReports.Rows[$dtReports.Rows.Count-1].reportID } else { 1 }))
$sb.AppendLine("")

# -- 2d. 字段 --
$sb.AppendLine("-- 插入字段")
$fieldSeq = 0
foreach ($row in $dtFields.Rows) {
    $fieldSeq++
    $rid = [int]$row.reportID
    $fname = ($row.fieldName -replace "'", "''")
    $ftitle = ($row.fieldTitle -replace "'", "''")
    $fidx = if ($row.fieldIndex -and $row.fieldIndex -ne [DBNull]::Value) { [int]$row.fieldIndex } else { 0 }
    $isDisplay = if ($row.isDisplay -and $row.isDisplay -ne [DBNull]::Value) { [bool]$row.isDisplay } else { $true }
    $isParam = if ($row.isParam -and $row.isParam -ne [DBNull]::Value) { [bool]$row.isParam } else { $false }
    
    $sb.AppendFormat("INSERT INTO rpt_field (id, report_id, field_name, field_title, field_type, sort_order, width, align, is_display, is_sortable, is_filterable) VALUES ({0}, {1}, '{2}', '{3}', 'string', {4}, 120, 'left', {5}, TRUE, {6});`n",
        $fieldSeq, $rid, $fname, $ftitle, $fidx, $(if ($isDisplay) { "TRUE" } else { "FALSE" }), $(if ($isParam) { "TRUE" } else { "FALSE" }))
}
if ($dtFields.Rows.Count -gt 0) {
    $sb.AppendFormat("SELECT setval('rpt_field_id_seq', {0}, true);`n", $fieldSeq)
}
$sb.AppendLine("")

# -- 2e. 过滤条件 --
$sb.AppendLine("-- 插入过滤条件")
$filterSeq = 0
foreach ($row in $dtFilters.Rows) {
    $filterSeq++
    $rid = [int]$row.reportID
    $fname = ($row.fieldName -replace "'", "''")
    $val = if ($row.Value -and $row.Value -ne [DBNull]::Value) { ($row.Value -replace "'", "''") } else { "" }
    $op = if ($row.comparisonOperator -and $row.comparisonOperator -ne [DBNull]::Value) { ($row.comparisonOperator -replace "'", "''") } else { "=" }
    $fidx = if ($row.filterIndex -and $row.filterIndex -ne [DBNull]::Value) { [int]$row.filterIndex } else { 0 }
    
    $sb.AppendFormat("INSERT INTO rpt_filter (id, report_id, field_name, label, default_value, operator, control_type, sort_order, is_required, is_deleted) VALUES ({0}, {1}, '{2}', '{2}', '{3}', '{4}', 'text', {5}, FALSE, FALSE);`n",
        $filterSeq, $rid, $fname, $val, $op, $fidx)
}
if ($dtFilters.Rows.Count -gt 0) {
    $sb.AppendFormat("SELECT setval('rpt_filter_id_seq', {0}, true);`n", $filterSeq)
}
$sb.AppendLine("")

# -- 2f. 排序 --
$sb.AppendLine("-- 插入排序")
$orderSeq = 0
foreach ($row in $dtOrders.Rows) {
    $orderSeq++
    $rid = [int]$row.reportID
    $fname = ($row.fieldName -replace "'", "''")
    $isAsc = if ($row.isAsc -and $row.isAsc -ne [DBNull]::Value) { [bool]$row.isAsc } else { $true }
    $oidx = if ($row.orderIndex -and $row.orderIndex -ne [DBNull]::Value) { [int]$row.orderIndex } else { 0 }
    
    $sb.AppendFormat("INSERT INTO rpt_sort (id, report_id, field_name, sort_order, is_asc, is_deleted) VALUES ({0}, {1}, '{2}', {3}, {4}, FALSE);`n",
        $orderSeq, $rid, $fname, $oidx, $(if ($isAsc) { "TRUE" } else { "FALSE" }))
}
if ($dtOrders.Rows.Count -gt 0) {
    $sb.AppendFormat("SELECT setval('rpt_sort_id_seq', {0}, true);`n", $orderSeq)
}
$sb.AppendLine("")

# -- 2g. 权限 --
$sb.AppendLine("-- 插入权限（UserID-based, access_type='view'）")
$permSeq = 0
foreach ($row in $dtPerms.Rows) {
    $permSeq++
    $rid = [int]$row.reportID
    $uid = [int]$row.UserID
    
    $sb.AppendFormat("INSERT INTO rpt_permission (id, report_id, principal_type, principal_id, access_type) VALUES ({0}, {1}, 'user', {2}, 'view');`n",
        $permSeq, $rid, $uid)
}
if ($dtPerms.Rows.Count -gt 0) {
    $sb.AppendFormat("SELECT setval('rpt_permission_id_seq', {0}, true);`n", $permSeq)
}
$sb.AppendLine("")

# -- 2h. PivotView → rpt_chart --
$sb.AppendLine("-- 插入图表（从PivotView映射）")
$chartSeq = 0
foreach ($row in $dtPivots.Rows) {
    $chartSeq++
    $rid = [int]$row.reportID
    $pvName = if ($row.pivotName -and $row.pivotName -ne [DBNull]::Value) { ($row.pivotName -replace "'", "''") } else { "透视分析" }
    $pvParams = if ($row.pivotParams -and $row.pivotParams -ne [DBNull]::Value) { ($row.pivotParams -replace "'", "''") } else { "" }
    
    $sb.AppendFormat("INSERT INTO rpt_chart (id, report_id, title, chart_type, options, is_deleted) VALUES ({0}, {1}, '{2}', 'table', '{3}', FALSE);`n",
        $chartSeq, $rid, $pvName, $pvParams)
}
if ($dtPivots.Rows.Count -gt 0) {
    $sb.AppendFormat("SELECT setval('rpt_chart_id_seq', {0}, true);`n", $chartSeq)
}

$sb.AppendLine("")
$sb.AppendLine("COMMIT;")

# ====== 写入临时 SQL 文件 ======
$sqlFile = Join-Path $PSScriptRoot "migrate-reports-generated.sql"
$sb.ToString() | Out-File -FilePath $sqlFile -Encoding UTF8
Write-Host "  SQL 文件已生成: $sqlFile" -ForegroundColor Green
Write-Host "  文件大小: $((Get-Item $sqlFile).Length / 1KB) KB"

# ====== 3. 执行迁移到 PostgreSQL ======
Write-Host "`n=== 3. 执行迁移到 PostgreSQL ===" -ForegroundColor Cyan
$psql = "D:\Scoop\apps\postgresql\current\bin\psql.exe"
$env:PGCLIENTENCODING = "UTF8"

$pgArgs = @(
    "-h", "localhost",
    "-p", "5432",
    "-U", "postgres",
    "-d", "eams2026",
    "-f", $sqlFile
)

Write-Host "  运行 psql ..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $psql -ArgumentList $pgArgs -NoNewWindow -Wait -PassThru

if ($proc.ExitCode -eq 0) {
    Write-Host "  迁移成功完成！" -ForegroundColor Green
} else {
    Write-Host "  迁移失败，退出码: $($proc.ExitCode)" -ForegroundColor Red
}

# ====== 4. 验证 ======
Write-Host "`n=== 4. 验证迁移结果 ===" -ForegroundColor Cyan
$env:PGCLIENTENCODING = "UTF8"
& $psql -h localhost -p 5432 -U postgres -d eams2026 -c "SELECT 'rpt_report' AS tbl, COUNT(*) AS cnt FROM rpt_report UNION ALL SELECT 'rpt_category', COUNT(*) FROM rpt_category UNION ALL SELECT 'rpt_field', COUNT(*) FROM rpt_field UNION ALL SELECT 'rpt_filter', COUNT(*) FROM rpt_filter UNION ALL SELECT 'rpt_sort', COUNT(*) FROM rpt_sort UNION ALL SELECT 'rpt_permission', COUNT(*) FROM rpt_permission UNION ALL SELECT 'rpt_chart', COUNT(*) FROM rpt_chart ORDER BY tbl;"

Write-Host "`n完成！" -ForegroundColor Cyan