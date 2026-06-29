using System.Data;
using System.Text;
using System.Data.SqlClient;
using Npgsql;

const string srcConnStr = "Server=db;Database=AppData;User ID=sa;Password=dfl_DB;TrustServerCertificate=True";
const string dstConnStr = "Host=localhost;Port=5432;Database=eams2026;Username=postgres;Password=postgres";

Console.WriteLine("=== 1. 读取 SQL Server 数据 ===");

await using var srcConn = new SqlConnection(srcConnStr);
await srcConn.OpenAsync();

var dtReports = await ReadTable(srcConn, "SELECT * FROM Reports ORDER BY reportID");
var dtClasses = await ReadTable(srcConn, "SELECT * FROM report_Class ORDER BY clsID");
var dtModules = await ReadTable(srcConn, "SELECT * FROM report_Module ORDER BY moduleID");
var dtFields = await ReadTable(srcConn, "SELECT * FROM report_Fields ORDER BY reportID, fieldIndex");
var dtFilters = await ReadTable(srcConn, "SELECT * FROM report_Filter ORDER BY reportID, filterIndex");
var dtOrders = await ReadTable(srcConn, "SELECT * FROM report_Order ORDER BY reportID, orderIndex");
var dtPerms = await ReadTable(srcConn, "SELECT * FROM report_Permissions ORDER BY reportID, AutoID");
var dtPivots = await ReadTable(srcConn, "SELECT * FROM report_PivotView ORDER BY reportID");

Console.WriteLine($"  Reports: {dtReports.Rows.Count}");
Console.WriteLine($"  Classes: {dtClasses.Rows.Count}");
Console.WriteLine($"  Modules: {dtModules.Rows.Count}");
Console.WriteLine($"  Fields:  {dtFields.Rows.Count}");
Console.WriteLine($"  Filters: {dtFilters.Rows.Count}");
Console.WriteLine($"  Orders:  {dtOrders.Rows.Count}");
Console.WriteLine($"  Perms:   {dtPerms.Rows.Count}");
Console.WriteLine($"  Pivots:  {dtPivots.Rows.Count}");

await srcConn.CloseAsync();

Console.WriteLine("\n=== 2. 生成 PostgreSQL SQL ===");

var sb = new StringBuilder();
sb.AppendLine("-- Report Migration Script");
sb.AppendLine("BEGIN;");
sb.AppendLine();

// Clear old data
sb.AppendLine("DELETE FROM rpt_chart;");
sb.AppendLine("DELETE FROM rpt_field_permission;");
sb.AppendLine("DELETE FROM rpt_permission;");
sb.AppendLine("DELETE FROM rpt_filter;");
sb.AppendLine("DELETE FROM rpt_sort;");
sb.AppendLine("DELETE FROM rpt_bookmark;");
sb.AppendLine("DELETE FROM rpt_execution_log;");
sb.AppendLine("DELETE FROM rpt_field;");
sb.AppendLine("DELETE FROM rpt_report;");
sb.AppendLine("DELETE FROM rpt_category;");
sb.AppendLine();

// 2b. Categories
sb.AppendLine("SELECT setval('rpt_category_id_seq', 1, false);");

var modMap = new Dictionary<int, long>();
int catSeq = 0;
foreach (DataRow row in dtModules.Rows)
{
    catSeq++;
    int mid = (int)row["moduleID"];
    string mname = EscapeSql((string)row["moduleName"]);
    sb.AppendLine($"INSERT INTO rpt_category (id, name, sort_order) VALUES ({catSeq}, '{mname}', {mid * 10});");
    modMap[mid] = catSeq;
}

var clsMap = new Dictionary<int, long>();
foreach (DataRow row in dtClasses.Rows)
{
    catSeq++;
    int cid = (int)row["clsID"];
    string cname = EscapeSql((string)row["clsName"]);
    sb.AppendLine($"INSERT INTO rpt_category (id, name, parent_id, sort_order) VALUES ({catSeq}, '{cname}', NULL, {cid * 10});");
    clsMap[cid] = catSeq;
}

var combMap = new Dictionary<string, long>();
foreach (DataRow modRow in dtModules.Rows)
{
    int mid = (int)modRow["moduleID"];
    string mname = EscapeSql((string)modRow["moduleName"]);
    foreach (DataRow clsRow in dtClasses.Rows)
    {
        int cid = (int)clsRow["clsID"];
        string cname = EscapeSql((string)clsRow["clsName"]);
        catSeq++;
        string key = $"{mid}_{cid}";
        combMap[key] = catSeq;
        long parentId = modMap[mid];
        sb.AppendLine($"INSERT INTO rpt_category (id, name, parent_id, sort_order) VALUES ({catSeq}, '{mname}-{cname}', {parentId}, {catSeq});");
    }
}
sb.AppendLine($"SELECT setval('rpt_category_id_seq', {catSeq}, true);");
sb.AppendLine();

// Collect valid report IDs
var validReportIds = new HashSet<int>();
foreach (DataRow row in dtReports.Rows)
    validReportIds.Add((int)row["reportID"]);

// 2c. Reports
sb.AppendLine("-- Reports");
int maxRptId = 0;
foreach (DataRow row in dtReports.Rows)
{
    int rid = (int)row["reportID"];
    maxRptId = Math.Max(maxRptId, rid);
    string name = EscapeSql(GetString(row, "Name"));
    string title = EscapeSql(GetString(row, "Title"));
    string desc = EscapeSql(GetString(row, "Description"));
    string queryBase = GetString(row, "QueryBase");
    int clsID = (int)row["clsID"];
    int moduleID = (int)row["moduleID"];
    string combKey = $"{moduleID}_{clsID}";
    string catId = combMap.ContainsKey(combKey) ? combMap[combKey].ToString() : "NULL";

    string queryType = queryBase.TrimStart().StartsWith("exec", StringComparison.OrdinalIgnoreCase)
        || queryBase.TrimStart().StartsWith("dflproc", StringComparison.OrdinalIgnoreCase)
        ? "'proc'" : "'sql'";

    sb.AppendLine($"INSERT INTO rpt_report (id, name, title, description, category_id, query_type, query_text, query_datasource, is_system, status, is_deleted, created_at, created_by, updated_at, updated_by) VALUES ({rid}, '{name}', '{title}', '{desc}', {catId}, {queryType}, '{EscapeSql(queryBase)}', 'main', FALSE, 'published', FALSE, NOW(), 1, NOW(), 1);");
}
sb.AppendLine($"SELECT setval('rpt_report_id_seq', {maxRptId}, true);");
sb.AppendLine();

// 2d. Fields
sb.AppendLine("-- Fields");
int fieldSeq = 0;
foreach (DataRow row in dtFields.Rows)
{
    int rid = (int)row["reportID"];
    if (!validReportIds.Contains(rid)) continue;
    fieldSeq++;
    string fname = EscapeSql(GetString(row, "fieldName"));
    string ftitle = EscapeSql(GetString(row, "fieldTitle"));
    int fidx = row["fieldIndex"] != DBNull.Value ? (int)row["fieldIndex"] : 0;
    bool isDisplay = row["isDisplay"] != DBNull.Value && (bool)row["isDisplay"];
    bool isParam = row["isParam"] != DBNull.Value && (bool)row["isParam"];

    sb.AppendLine($"INSERT INTO rpt_field (id, report_id, field_name, field_title, field_type, sort_order, width, align, is_display, is_sortable, is_filterable) VALUES ({fieldSeq}, {rid}, '{fname}', '{ftitle}', 'string', {fidx}, 120, 'left', {(isDisplay ? "TRUE" : "FALSE")}, TRUE, {(isParam ? "TRUE" : "FALSE")});");
}
if (fieldSeq > 0)
    sb.AppendLine($"SELECT setval('rpt_field_id_seq', {fieldSeq}, true);");
sb.AppendLine();

// 2e. Filters
sb.AppendLine("-- Filters");
int filterSeq = 0;
foreach (DataRow row in dtFilters.Rows)
{
    int rid = (int)row["reportID"];
    if (!validReportIds.Contains(rid)) continue;
    filterSeq++;
    string fname = EscapeSql(GetString(row, "fieldName"));
    string val = EscapeSql(GetString(row, "value"));
    string op = EscapeSql(GetString(row, "comparisonOperator"));
    int fidx = row["filterIndex"] != DBNull.Value ? (int)row["filterIndex"] : 0;

    sb.AppendLine($"INSERT INTO rpt_filter (id, report_id, field_name, label, default_value, operator, control_type, sort_order, is_required, is_deleted) VALUES ({filterSeq}, {rid}, '{fname}', '{fname}', '{val}', '{op}', 'text', {fidx}, FALSE, FALSE);");
}
if (filterSeq > 0)
    sb.AppendLine($"SELECT setval('rpt_filter_id_seq', {filterSeq}, true);");
sb.AppendLine();

// 2f. Orders
sb.AppendLine("-- Orders");
int orderSeq = 0;
foreach (DataRow row in dtOrders.Rows)
{
    int rid = (int)row["reportID"];
    if (!validReportIds.Contains(rid)) continue;
    orderSeq++;
    string fname = EscapeSql(GetString(row, "fieldName"));
    bool isAsc = row["isAsc"] != DBNull.Value && (bool)row["isAsc"];
    int oidx = row["orderIndex"] != DBNull.Value ? (int)row["orderIndex"] : 0;

    sb.AppendLine($"INSERT INTO rpt_sort (id, report_id, field_name, direction, sort_order) VALUES ({orderSeq}, {rid}, '{fname}', '{(isAsc ? "asc" : "desc")}', {oidx});");
}
if (orderSeq > 0)
    sb.AppendLine($"SELECT setval('rpt_sort_id_seq', {orderSeq}, true);");
sb.AppendLine();

// 2g. Permissions
sb.AppendLine("-- Permissions");
int permSeq = 0;
foreach (DataRow row in dtPerms.Rows)
{
    int rid = (int)row["reportID"];
    if (!validReportIds.Contains(rid)) continue;
    permSeq++;
    int uid = (int)row["UserID"];
    sb.AppendLine($"INSERT INTO rpt_permission (id, report_id, principal_type, principal_id, access_type) VALUES ({permSeq}, {rid}, 'user', {uid}, 'view');");
}
if (permSeq > 0)
    sb.AppendLine($"SELECT setval('rpt_permission_id_seq', {permSeq}, true);");
sb.AppendLine();

// 2h. PivotView -> Chart
sb.AppendLine("-- Charts (from PivotView)");
int chartSeq = 0;
foreach (DataRow row in dtPivots.Rows)
{
    int rid = (int)row["reportID"];
    if (!validReportIds.Contains(rid)) continue;
    chartSeq++;
    string pvName = EscapeSql(GetString(row, "pivotName"));
    string pvParams = EscapeSql(GetString(row, "pivotParams"));

    sb.AppendLine($"INSERT INTO rpt_chart (id, report_id, title, chart_type, options, is_deleted) VALUES ({chartSeq}, {rid}, '{pvName}', 'table', '{pvParams}', FALSE);");
}
if (chartSeq > 0)
    sb.AppendLine($"SELECT setval('rpt_chart_id_seq', {chartSeq}, true);");

sb.AppendLine();
sb.AppendLine("COMMIT;");

// Write SQL file
string sqlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "migrate-reports-generated.sql");
sqlPath = Path.GetFullPath(sqlPath);
await File.WriteAllTextAsync(sqlPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"  SQL written to: {sqlPath}");

// Execute
Console.WriteLine("\n=== 3. Executing migration to PostgreSQL ===");
await using var dstConn = new NpgsqlConnection(dstConnStr);
await dstConn.OpenAsync();

// Split by statements and execute
string sql = sb.ToString();
await using var cmd = dstConn.CreateCommand();
cmd.CommandText = sql;
cmd.CommandTimeout = 60;
await cmd.ExecuteNonQueryAsync();

Console.WriteLine("  Migration executed successfully!");

// Verify
Console.WriteLine("\n=== 4. Verification ===");
var verify = await ReadTable(dstConn, @"
    SELECT 'rpt_report' AS tbl, COUNT(*)::int AS cnt FROM rpt_report
    UNION ALL SELECT 'rpt_category', COUNT(*)::int FROM rpt_category
    UNION ALL SELECT 'rpt_field', COUNT(*)::int FROM rpt_field
    UNION ALL SELECT 'rpt_filter', COUNT(*)::int FROM rpt_filter
    UNION ALL SELECT 'rpt_sort', COUNT(*)::int FROM rpt_sort
    UNION ALL SELECT 'rpt_permission', COUNT(*)::int FROM rpt_permission
    UNION ALL SELECT 'rpt_chart', COUNT(*)::int FROM rpt_chart
    ORDER BY tbl");
foreach (DataRow row in verify.Rows)
{
    Console.WriteLine($"  {row["tbl"]}: {row["cnt"]}");
}

Console.WriteLine("\nDone!");

// Helper methods
static string EscapeSql(string? value)
{
    if (string.IsNullOrEmpty(value)) return "";
    return value.Replace("'", "''");
}

static string GetString(DataRow row, string col)
{
    return row[col] != DBNull.Value ? row[col].ToString() ?? "" : "";
}

static async Task<DataTable> ReadTable(IDbConnection conn, string sql)
{
    var dt = new DataTable();
    if (conn is SqlConnection sqlConn)
    {
        await using var cmd = new SqlCommand(sql, sqlConn);
        await using var reader = await cmd.ExecuteReaderAsync();
        dt.Load(reader);
    }
    else if (conn is NpgsqlConnection pgConn)
    {
        await using var cmd = new NpgsqlCommand(sql, pgConn);
        await using var reader = await cmd.ExecuteReaderAsync();
        dt.Load(reader);
    }
    return dt;
}