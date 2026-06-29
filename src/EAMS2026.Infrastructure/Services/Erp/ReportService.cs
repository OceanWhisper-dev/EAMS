using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using EAMS2026.Application.Common.Interfaces.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Interfaces.Repositories.Erp;
using EAMS2026.Infrastructure.Data;
// System.Data.SqlClient 兼容 SQL Server 2005（Microsoft.Data.SqlClient 无法连接 SQL Server 2005）
#pragma warning disable CS0618
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EAMS2026.Infrastructure.Services.Erp;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly DbConnectionFactory _connectionFactory;
    private readonly HwattConnectionFactory _hwattConnectionFactory;
    private readonly ExcelService _excelService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reportRepository,
        DbConnectionFactory connectionFactory,
        HwattConnectionFactory hwattConnectionFactory,
        ExcelService excelService,
        ILogger<ReportService> logger)
    {
        _reportRepository = reportRepository;
        _connectionFactory = connectionFactory;
        _hwattConnectionFactory = hwattConnectionFactory;
        _excelService = excelService;
        _logger = logger;
    }

    // ========== 报表分类 ==========

    public async Task<IEnumerable<ReportCategoryDto>> GetCategoriesAsync()
        => await _reportRepository.GetCategoriesAsync();

    public async Task<(bool Success, string Message)> AddCategoryAsync(string name, long? parentId, int sortOrder, long operatorId)
    {
        await _reportRepository.AddCategoryAsync(name, parentId, sortOrder, operatorId);
        return (true, "分类创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateCategoryAsync(long id, string name, long? parentId, int sortOrder, long operatorId)
    {
        var ok = await _reportRepository.UpdateCategoryAsync(id, name, parentId, sortOrder, operatorId);
        return ok ? (true, "更新成功") : (false, "分类不存在");
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(long id)
    {
        var ok = await _reportRepository.DeleteCategoryAsync(id);
        return ok ? (true, "删除成功") : (false, "分类下有子分类或报表，无法删除");
    }

    // ========== 报表CRUD ==========

    public async Task<IEnumerable<ReportDto>> GetReportsAsync(long? categoryId, long userId)
        => await _reportRepository.GetReportsAsync(categoryId, userId);

    public async Task<ReportDetailDto?> GetReportDetailAsync(long id)
        => await _reportRepository.GetReportDetailAsync(id);

    public async Task<(bool Success, string Message, long Id)> AddReportAsync(ReportDetailDto report, long operatorId)
    {
        try
        {
            var id = await _reportRepository.AddReportAsync(report, operatorId);
            return (true, "报表创建成功", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建报表失败");
            return (false, $"创建失败: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Message)> UpdateReportAsync(ReportDetailDto report, long operatorId)
    {
        try
        {
            var ok = await _reportRepository.UpdateReportAsync(report, operatorId);
            return ok ? (true, "更新成功") : (false, "报表不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新报表失败");
            return (false, $"更新失败: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteReportAsync(long id)
    {
        var ok = await _reportRepository.DeleteReportAsync(id);
        return ok ? (true, "删除成功") : (false, "系统报表不可删除");
    }

    public async Task<(bool Success, string Message)> UpdateReportStatusAsync(long id, string status)
    {
        var ok = await _reportRepository.UpdateReportStatusAsync(id, status);
        return ok ? (true, "状态更新成功") : (false, "报表不存在");
    }

    // ========== 报表执行（核心） ==========

    public async Task<ReportExecuteResult> ExecuteReportAsync(long reportId, ReportExecuteRequest request, long userId)
    {
        _logger.LogInformation("[ExecuteReport] reportId={ReportId}, userId={UserId}", reportId, userId);

        var sw = Stopwatch.StartNew();

        // 1. 加载报表配置
        var report = await _reportRepository.GetReportDetailAsync(reportId)
            ?? throw new KeyNotFoundException($"报表 {reportId} 不存在");

        // 1.1 发布状态检查：未发布的报表仅管理员可查看
        if (report.Status != "published")
        {
            await EnsureAdminAsync(userId, "报表未发布，仅管理员可查看");
        }

        // 2. 权限检查
        var hasView = await _reportRepository.HasPermissionAsync(reportId, userId, "view");
        _logger.LogInformation("[ExecuteReport] hasView={HasView}", hasView);
        if (!hasView)
            throw new UnauthorizedAccessException("无权限查看此报表");

        // 3. 业务员数据权限：如果报表有 salesperson 类型的过滤参数，自动注入当前用户业务员编码
        var salespersonFilters = report.Filters.Where(f => f.ControlType == "salesperson").ToList();
        if (salespersonFilters.Count > 0)
        {
            var salespersonInfo = await GetCurrentUserSalespersonInfoAsync(userId);
            foreach (var sf in salespersonFilters)
            {
                if (salespersonInfo.IsSalesperson && salespersonInfo.Type != "supervisor" && !string.IsNullOrEmpty(salespersonInfo.SalespersonCode))
                {
                    request.Params[sf.FieldName] = salespersonInfo.SalespersonCode;
                    _logger.LogInformation("[ExecuteReport] 业务员 userId={UserId} 自动注入 {Field}={Code}",
                        userId, sf.FieldName, salespersonInfo.SalespersonCode);
                }
            }
        }

        // 4. 执行查询
        var result = await ExecuteQueryInternal(report, request);

        sw.Stop();

        // 5. 记录日志
        var paramsJson = JsonSerializer.Serialize(request.Params);
        await _reportRepository.AddExecutionLogAsync(reportId, userId, paramsJson,
            result.Rows.Count, (int)sw.ElapsedMilliseconds, true, null);

        result.ExecutionInfo = new ReportExecutionInfo
        {
            DurationMs = (int)sw.ElapsedMilliseconds,
            RowCount = result.Rows.Count
        };

        return result;
    }

    public async Task<ReportExecuteResult> PreviewReportAsync(ReportPreviewRequest request)
    {
        var sw = Stopwatch.StartNew();

        var queryType = request.QueryType ?? "sql";
        var sqlText = request.QueryText;

        _logger.LogInformation("[PreviewReport] queryType={QueryType}, datasource={Datasource}, paramsCount={ParamsCount}",
            queryType, request.QueryDatasource, request.Params?.Count ?? 0);

        if (request.Params?.Count > 0)
        {
            foreach (var (key, value) in request.Params)
                _logger.LogDebug("[PreviewReport]    param {Key} = {Value} ({Type})", key, value, value?.GetType().Name ?? "null");
        }

        // 生成过滤条件（SQL 类型且有关联参数时）
        if (queryType != "proc" && request.Filters?.Count > 0 && request.Params?.Count > 0)
        {
            var (modifiedSql, extraParams) = BuildFilterConditions(sqlText, request.Filters, request.Params);
            if (modifiedSql != sqlText)
            {
                sqlText = modifiedSql;
                foreach (var (k, v) in extraParams)
                    request.Params[k] = v;
            }
        }

        ReportExecuteResult result;

        if (queryType == "proc")
        {
            _logger.LogInformation("[PreviewReport] 执行存储过程: {QueryText}", sqlText);
            result = await ExecuteStoredProcedure(sqlText, new ReportExecuteRequest
            {
                Params = request.Params!,
                Pagination = request.Page.HasValue
                    ? new ReportPagination { Page = request.Page.Value, PageSize = request.PageSize ?? 50 }
                    : null
            }, request.QueryDatasource);
        }
        else
        {
            // sql 类型
            if (string.IsNullOrWhiteSpace(sqlText))
                throw new ArgumentException("SQL查询语句不能为空");

            ReportSqlEngine.ValidateSql(sqlText);

            _logger.LogInformation("[PreviewReport] 执行SQL: {SqlPreview}", sqlText.Length > 200 ? sqlText[..200] + "..." : sqlText);
            result = await ExecuteRawSql(sqlText, new ReportExecuteRequest
            {
                Params = request.Params!,
                Pagination = request.Page.HasValue
                    ? new ReportPagination { Page = request.Page.Value, PageSize = request.PageSize ?? 50 }
                    : null
            }, request.QueryDatasource);
        }

        // 用前端传来的字段配置覆盖列标题
        if (request.Fields?.Count > 0)
        {
            var fieldMap = request.Fields
                .GroupBy(f => f.FieldName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var col in result.Columns)
            {
                if (fieldMap.TryGetValue(col.Field, out var field))
                {
                    if (!string.IsNullOrEmpty(field.FieldTitle))
                        col.Title = field.FieldTitle;
                    if (field.Width > 0)
                        col.Width = field.Width;
                    if (!string.IsNullOrEmpty(field.Align))
                        col.Align = field.Align;
                    col.IsSortable = field.IsSortable;
                    col.SummaryType = field.SummaryType;
                    col.FormatPattern = field.FormatPattern;
                }
            }
        }

        // 根据字段的 summaryType 重新计算汇总行（用于测试查询也能显示汇总结果）
        var summaryColumns = result.Columns.Where(c => !string.IsNullOrEmpty(c.SummaryType)).ToList();
        if (summaryColumns.Count > 0)
        {
            var newSummary = new Dictionary<string, object?>();
            foreach (var col in summaryColumns)
            {
                newSummary[col.Title] = ComputeSummaryValue(result.Rows, col.Field, col.SummaryType!);
            }
            if (newSummary.Count > 0)
                result.Summary = newSummary;
        }

        sw.Stop();
        result.ExecutionInfo = new ReportExecutionInfo
        {
            DurationMs = (int)sw.ElapsedMilliseconds,
            RowCount = result.Rows.Count
        };

        _logger.LogInformation("[PreviewReport] 完成, 耗时={DurationMs}ms, 行数={RowCount}",
            result.ExecutionInfo.DurationMs, result.ExecutionInfo.RowCount);

        // 临时调试：检查第一行数据是否有 DBNull
        if (result.Rows.Count > 0)
        {
            try
            {
                var sample = System.Text.Json.JsonSerializer.Serialize(result.Rows[0]);
                _logger.LogInformation("[PreviewReport] 第一行JSON预览 (前200): {Json}", sample.Length > 200 ? sample[..200] : sample);
                _logger.LogInformation("[PreviewReport] 第一行JSON总长度: {Len}", sample.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PreviewReport] 序列化测试行失败");
            }
        }
        _logger.LogInformation("[PreviewReport] 列数={ColCount}, 列名={ColNames}",
            result.Columns.Count, string.Join(", ", result.Columns.Select(c => c.Field)));

        return result;
    }

    public async Task<byte[]> ExportReportAsync(long reportId, ReportExportRequest request, long userId)
    {
        _logger.LogInformation("[ExportReportAsync] 开始导出报表 {ReportId}, 格式={Format}, 参数数={ParamCount}",
            reportId, request.Format, request.Params?.Count ?? 0);

        if (request.Params?.Count > 0)
        {
            foreach (var (key, value) in request.Params)
                _logger.LogDebug("[ExportReportAsync]    param {Key} = {Value} ({Type})", key, value, value?.GetType().Name ?? "null");
        }

        var report = await _reportRepository.GetReportDetailAsync(reportId)
            ?? throw new KeyNotFoundException($"报表 {reportId} 不存在");

        // 发布状态检查：未发布的报表仅管理员可导出
        if (report.Status != "published")
        {
            await EnsureAdminAsync(userId, "报表未发布，仅管理员可导出");
        }

        // 导出权限检查
        var hasExport = await _reportRepository.HasPermissionAsync(reportId, userId, "export");
        _logger.LogInformation("[ExportReportAsync] hasExport={HasExport}", hasExport);
        if (!hasExport)
            throw new UnauthorizedAccessException("无权限导出此报表");

        var executeRequest = new ReportExecuteRequest { Params = request.Params! };

        // 业务员数据权限：如果报表有 salesperson 类型的过滤参数，自动注入当前用户业务员编码
        var salespersonFilters = report.Filters.Where(f => f.ControlType == "salesperson").ToList();
        if (salespersonFilters.Count > 0)
        {
            var salespersonInfo = await GetCurrentUserSalespersonInfoAsync(userId);
            foreach (var sf in salespersonFilters)
            {
                if (salespersonInfo.IsSalesperson && salespersonInfo.Type != "supervisor" && !string.IsNullOrEmpty(salespersonInfo.SalespersonCode))
                {
                    executeRequest.Params![sf.FieldName] = salespersonInfo.SalespersonCode;
                    _logger.LogInformation("[ExportReportAsync] 业务员 userId={UserId} 自动注入 {Field}={Code}",
                        userId, sf.FieldName, salespersonInfo.SalespersonCode);
                }
            }
        }

        var result = await ExecuteQueryInternal(report, executeRequest);

        _logger.LogInformation("[ExportReportAsync] 查询结果: {RowCount} 行, {ColCount} 列",
            result.Rows.Count, result.Columns.Count);

        // 字段级权限过滤
        await ApplyFieldPermissions(reportId, userId, result);

        _logger.LogInformation("[ExportReportAsync] 权限过滤后: {RowCount} 行, {ColCount} 列",
            result.Rows.Count, result.Columns.Count);

        var exportData = request.Format.ToLower() switch
        {
            "xlsx" => _excelService.ExportToExcel(result.Columns, result.Rows),
            "csv" => ExportToCsv(result.Columns, result.Rows),
            _ => throw new ArgumentException($"不支持的导出格式: {request.Format}")
        };

        _logger.LogInformation("[ExportReportAsync] 导出完成, 数据大小={Size} bytes", exportData.Length);
        return exportData;
    }

    // ========== 收藏 ==========

    public async Task<IEnumerable<ReportDto>> GetBookmarksAsync(long userId)
        => await _reportRepository.GetBookmarksAsync(userId);

    public async Task<bool> ToggleBookmarkAsync(long userId, long reportId)
    {
        var bookmarks = await _reportRepository.GetBookmarksAsync(userId);
        var exists = bookmarks.Any(b => b.Id == reportId);
        if (exists)
            return await _reportRepository.RemoveBookmarkAsync(userId, reportId);
        else
            return await _reportRepository.AddBookmarkAsync(userId, reportId);
    }

    // ========== 内部方法 ==========

    /// <summary>根据数据源名称动态创建数据库连接</summary>
    private static Dictionary<string, object?> NormalizeParams(Dictionary<string, object?>? raw)
    {
        if (raw == null || raw.Count == 0) return [];

        var result = new Dictionary<string, object?>(raw.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in raw)
        {
            result[key] = ConvertJsonElement(value);
        }
        return result;
    }

    private static object? ConvertJsonElement(object? value)
    {
        if (value is System.Text.Json.JsonElement je)
        {
            return je.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => je.GetString(),
                System.Text.Json.JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                System.Text.Json.JsonValueKind.Null => null,
                _ => je.GetRawText()
            };
        }
        return value;
    }

    private async Task<IDbConnection> CreateDataSourceConnectionAsync(string? datasource)
    {
        // 无数据源，使用主库（PostgreSQL）
        if (string.IsNullOrWhiteSpace(datasource))
            return _connectionFactory.CreateConnection();

        // 查找数据源配置
        var ds = await _reportRepository.GetDataSourceByNameAsync(datasource);
        if (ds == null)
        {
            _logger.LogWarning("数据源 '{Datasource}' 不存在，回退到主库", datasource);
            return _connectionFactory.CreateConnection();
        }

        if (ds.DbType == "postgresql")
            return new NpgsqlConnection(ds.ConnectionString);

        if (ds.DbType == "sqlserver")
        {
            if (global::System.OperatingSystem.IsLinux())
            {
                // Linux：使用 ODBC + FreeTDS（绕过 OpenSSL 3.0 的 TLS 1.0 限制）
                var odbcStr = ErpConnectionFactory.ConvertToOdbcConnectionString(ds.ConnectionString);
                return new OdbcCompatibleConnection(new OdbcConnection(odbcStr));
            }
            else
            {
                // Windows：使用 System.Data.SqlClient 直接连接
                var connStr = ErpConnectionFactory.AppendSqlServerCompat(ds.ConnectionString);
                return new SqlConnection(connStr);
            }
        }

        _logger.LogWarning("不支持的数据源类型 '{DbType}'，回退到主库", ds.DbType);
        return _connectionFactory.CreateConnection();
    }

    private async Task<ReportExecuteResult> ExecuteQueryInternal(ReportDetailDto report, ReportExecuteRequest request)
    {
        ReportExecuteResult result;

        if (report.QueryType == "sql")
        {
            var sqlText = report.QueryText;

            // 对 SQL 类型需要拼接过滤条件到 WHERE 子句
            if (report.Filters.Count > 0 && request.Params?.Count > 0)
            {
                var (modifiedSql, extraParams) = BuildFilterConditions(sqlText, report.Filters, request.Params);
                if (modifiedSql != sqlText)
                {
                    sqlText = modifiedSql;
                    foreach (var (k, v) in extraParams)
                        request.Params[k] = v;
                }
            }

            // 应用排序：请求排序 > 报表默认排序
            if (request.Sort == null || request.Sort.Count == 0)
            {
                var defaultSorts = report.Sorts
                    .OrderBy(s => s.SortOrder)
                    .Select(s => new ReportSortOption { Field = s.FieldName, Direction = s.Direction })
                    .ToList();
                if (defaultSorts.Count > 0)
                {
                    request.Sort = defaultSorts;
                    _logger.LogInformation("[ExecuteQueryInternal] 应用报表默认排序: {Sorts}",
                        string.Join(", ", defaultSorts.Select(s => $"{s.Field} {s.Direction}")));
                }
            }

            result = await ExecuteRawSql(sqlText, request, report.QueryDatasource);
        }
        else if (report.QueryType == "proc")
        {
            // 存储过程：过滤条件通过参数传递，无需拼接 SQL
            result = await ExecuteStoredProcedure(report.QueryText, request, report.QueryDatasource);
        }
        else
        {
            throw new NotSupportedException($"不支持的查询类型: {report.QueryType}");
        }

        // 用报表字段配置的显示标题覆盖原始列名
        if (report.Fields.Count > 0)
        {
            var fieldMap = report.Fields
                .GroupBy(f => f.FieldName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var col in result.Columns)
            {
                if (fieldMap.TryGetValue(col.Field, out var field))
                {
                    if (!string.IsNullOrEmpty(field.FieldTitle))
                        col.Title = field.FieldTitle;
                    if (field.Width > 0)
                        col.Width = field.Width;
                    if (!string.IsNullOrEmpty(field.Align))
                        col.Align = field.Align;
                    col.IsSortable = field.IsSortable;
                    col.SummaryType = field.SummaryType;
                    col.FormatPattern = field.FormatPattern;
                }
            }
        }

        // 根据字段的 summaryType 重新计算汇总行
        // sum: 求和, count: 去重计数（文本字段不同值算一次）
        var summaryColumns = result.Columns.Where(c => !string.IsNullOrEmpty(c.SummaryType)).ToList();
        if (summaryColumns.Count > 0)
        {
            var newSummary = new Dictionary<string, object?>();
            foreach (var col in summaryColumns)
            {
                newSummary[col.Title] = ComputeSummaryValue(result.Rows, col.Field, col.SummaryType!);
            }
            if (newSummary.Count > 0)
                result.Summary = newSummary;
        }

        return result;
    }

    private async Task<ReportExecuteResult> ExecuteRawSql(string sql, ReportExecuteRequest request, string? datasource)
    {
        ReportSqlEngine.ValidateSql(sql);

        // 动态创建连接
        using var conn = await CreateDataSourceConnectionAsync(datasource);

        // 判断是否是 SQL Server（原生 SqlClient 或 Linux 上的 ODBC 连接至 SQL Server）
        var isSqlServer = conn is SqlConnection || conn is OdbcCompatibleConnection;

        _logger.LogInformation("[ExecuteRawSql] datasource={Datasource}, isSqlServer={IsSqlServer}, sql={Sql}", datasource ?? "main", isSqlServer, sql);

        var parameters = new DynamicParameters();
        var normalizedParams = NormalizeParams(request.Params);
        _logger.LogInformation("[ExecuteRawSql] 归一化参数数: {Count}", normalizedParams.Count);

        // FreeTDS + SQL Server 2005: ODBC 参数绑定机制存在严重兼容性问题，
        // DateTime 和字符串参数均无法正确传递。解决方案：将所有参数内联到 SQL 中，
        // 完全绕过 ODBC 参数绑定。
        var odbcInlineKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (conn is OdbcCompatibleConnection && normalizedParams.Count > 0)
        {
            foreach (var (key, value) in normalizedParams)
            {
                var safeKey = SanitizeParamName(key);
                var literal = value switch
                {
                    DateTime dtVal => $"{{ts '{dtVal:yyyy-MM-dd HH:mm:ss}'}}",
                    null => "NULL",
                    string s when DateTime.TryParse(s, out var parsed) => $"{{ts '{parsed:yyyy-MM-dd HH:mm:ss}'}}",
                    string s => $"'{s.Replace("'", "''")}'",
                    int or long or short or byte => value.ToString()!,
                    float or double or decimal => ((double)Convert.ChangeType(value, typeof(double))).ToString("G"),
                    bool b => b ? "1" : "0",
                    _ => $"'{value?.ToString()?.Replace("'", "''") ?? ""}'"
                };
                sql = sql.Replace($"@{safeKey}", literal, StringComparison.OrdinalIgnoreCase);
                odbcInlineKeys.Add(safeKey);
                _logger.LogInformation("[ExecuteRawSql] ODBC内联: @{Key} -> {Literal}", safeKey, literal);
            }
        }

        if (normalizedParams.Count > 0)
        {
            foreach (var (key, value) in normalizedParams)
            {
                var safeKey = SanitizeParamName(key);
                _logger.LogDebug("[ExecuteRawSql]    param {Key} -> {SafeKey} = {Value} ({Type})", key, safeKey, value, value?.GetType().Name ?? "null");

                if (odbcInlineKeys.Contains(safeKey))
                    continue;

                if (value is DateTime dtVal)
                {
                    parameters.Add(safeKey, dtVal);
                }
                else if (value is string str && DateTime.TryParse(str, out var dt))
                {
                    parameters.Add(safeKey, dt);
                }
                else
                    parameters.Add(safeKey, value);
            }
        }

        // 处理排序参数 — 如果请求中有排序，覆盖 SQL 中原有的 ORDER BY
        var sqlForPagination = BuildOrderByClause(sql, request.Sort, isSqlServer);

        // 分页处理
        var page = request.Pagination?.Page ?? 1;
        var pageSize = request.Pagination?.PageSize ?? 0;

        _logger.LogInformation("[ExecuteRawSql] page={Page}, pageSize={PageSize}", page, pageSize);

        IEnumerable<dynamic> rows;
        int total = 0;

        if (pageSize > 0)
        {
            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS _count";
            total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
            _logger.LogInformation("[ExecuteRawSql] COUNT结果: {Total}", total);

            var offset = (page - 1) * pageSize;
            string pagedSql;
            if (isSqlServer)
            {
                // 兼容 SQL Server 2005+ 的 ROW_NUMBER 分页方式
                // 移除原始 SQL 末尾的 ORDER BY (子查询中不允许 ORDER BY)
                var cleanSql = Regex.Replace(sqlForPagination, @"\bORDER\s+BY\s+[\s\S]*$", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                var start = offset + 1;
                var end = offset + pageSize;
                if (conn is OdbcCompatibleConnection)
                {
                    pagedSql = $"SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS _rn FROM ({cleanSql}) AS _inner) AS _outer WHERE _rn BETWEEN {start} AND {end}";
                }
                else
                {
                    pagedSql = $"SELECT * FROM (SELECT *, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS _rn FROM ({cleanSql}) AS _inner) AS _outer WHERE _rn BETWEEN @Start AND @End";
                    parameters.Add("Start", start);
                    parameters.Add("End", end);
                }
            }
            else
            {
                if (conn is OdbcCompatibleConnection)
                {
                    pagedSql = $"{sqlForPagination} LIMIT {pageSize} OFFSET {offset}";
                }
                else
                {
                    pagedSql = $"{sqlForPagination} LIMIT @Limit OFFSET @Offset";
                    parameters.Add("Limit", pageSize);
                    parameters.Add("Offset", offset);
                }
            }
            _logger.LogInformation("[ExecuteRawSql] 分页SQL: {Sql}", pagedSql);
            rows = await conn.QueryAsync(pagedSql, parameters);
        }
        else
        {
            _logger.LogInformation("[ExecuteRawSql] 无分页SQL: {Sql}", sqlForPagination);
            rows = await conn.QueryAsync(sqlForPagination, parameters);
        }

        // 转换为列和行
        var (columns, resultRows, summary) = ConvertRowsToColumns(rows);

        _logger.LogInformation("[ExecuteRawSql] 返回列数={ColCount}, 行数={RowCount}", columns.Count, resultRows.Count);

        return new ReportExecuteResult
        {
            Columns = columns,
            Rows = resultRows,
            Summary = summary,
            Pagination = pageSize > 0
                ? new ReportPaginationResult { Page = page, PageSize = pageSize, Total = total }
                : null
        };
    }

    private async Task<ReportExecuteResult> ExecuteStoredProcedure(string procText, ReportExecuteRequest request, string? datasource)
    {
        using var conn = await CreateDataSourceConnectionAsync(datasource);

        // 支持多存储过程：按行拆分，每行一个 proc
        var procNames = procText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.StartsWith("exec ", StringComparison.OrdinalIgnoreCase)
                ? line["exec ".Length..].Trim()
                : line)
            .ToList();

        if (procNames.Count == 0)
            throw new ArgumentException("存储过程名称不能为空");

        _logger.LogInformation("[ExecuteStoredProcedure] 将执行 {Count} 个存储过程: {Names}",
            procNames.Count, string.Join(", ", procNames));

        var allColumns = new List<ReportColumnDto>();
        var allRows = new List<Dictionary<string, object?>>();
        var isFirstProc = true;

        foreach (var procName in procNames)
        {
            var allParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var requiredParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parameters = await BuildStoredProcedureParameters(procName, allParamNames, requiredParams, request.Params, conn);

            _logger.LogInformation("[ExecuteStoredProcedure] 执行 {ProcName}, 参数数={ParamCount}",
                procName, parameters.ParameterNames.Count());
            foreach (var pn in parameters.ParameterNames)
                _logger.LogDebug("[ExecuteStoredProcedure]    -> 参数名: {ParamName}", pn);

            var rowList = (await conn.QueryAsync(procName, parameters, commandType: CommandType.StoredProcedure)).ToList();

            _logger.LogInformation("[ExecuteStoredProcedure] {ProcName} 返回 {RowCount} 行", procName, rowList.Count);

            if (rowList.Count == 0) continue;

            foreach (IDictionary<string, object?> row in rowList)
            {
                if (isFirstProc)
                {
                    foreach (var key in row.Keys)
                        allColumns.Add(new ReportColumnDto { Field = key, Title = key, Type = InferType(row[key]) });
                    isFirstProc = false;
                }
                allRows.Add(new Dictionary<string, object?>(row));
            }
        }

        _logger.LogInformation("[ExecuteStoredProcedure] 全部完成, 总计 {RowCount} 行, {ColCount} 列",
            allRows.Count, allColumns.Count);

        return new ReportExecuteResult { Columns = allColumns, Rows = allRows };
    }

    private async Task ApplyFieldPermissions(long reportId, long userId, ReportExecuteResult result)
    {
        using var conn = _connectionFactory.CreateConnection();
        var hiddenFields = await conn.QueryAsync<string>(
            @"SELECT fp.field_name FROM erp_rpt_field_permission fp
              WHERE fp.report_id = @ReportId AND fp.is_visible = FALSE
                AND ((fp.principal_type = 'user' AND fp.principal_id = @UserId)
                  OR (fp.principal_type = 'role' AND fp.principal_id IN (
                      SELECT ur.role_id FROM sys_user_roles ur WHERE ur.user_id = @UserId)))",
            new { ReportId = reportId, UserId = userId });

        var hiddenSet = new HashSet<string>(hiddenFields, StringComparer.OrdinalIgnoreCase);
        if (hiddenSet.Count == 0) return;

        result.Columns.RemoveAll(c => hiddenSet.Contains(c.Field));
        foreach (var row in result.Rows)
        {
            foreach (var field in hiddenSet)
                row.Remove(field);
        }
    }

    private static string InferType(object? value) => value switch
    {
        int or long or decimal or float or double => "number",
        DateTime or DateOnly => "date",
        bool => "boolean",
        _ => "string"
    };

    private static byte[] ExportToCsv(List<ReportColumnDto> columns, List<Dictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Title))));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(row.GetValueOrDefault(c.Field)?.ToString() ?? ""))));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // ========== 数据源配置 ==========

    public async Task<IEnumerable<ReportDataSourceDto>> GetDataSourcesAsync()
    {
        return await _reportRepository.GetDataSourcesAsync();
    }

    public async Task<ReportDataSourceDto?> GetDataSourceAsync(long id)
    {
        return await _reportRepository.GetDataSourceAsync(id);
    }

    public async Task<(bool Success, string Message, long Id)> AddDataSourceAsync(CreateDataSourceRequest request, long operatorId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return (false, "数据源名称不能为空", 0);
            if (string.IsNullOrWhiteSpace(request.ConnectionString))
                return (false, "连接字符串不能为空", 0);

            var existing = await _reportRepository.GetDataSourceByNameAsync(request.Name);
            if (existing != null)
                return (false, "数据源名称已存在", 0);

            var id = await _reportRepository.AddDataSourceAsync(request, operatorId);
            return (true, "创建成功", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建数据源失败");
            return (false, $"创建失败: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Message)> UpdateDataSourceAsync(long id, UpdateDataSourceRequest request, long operatorId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ConnectionString))
                return (false, "连接字符串不能为空");

            var ok = await _reportRepository.UpdateDataSourceAsync(id, request, operatorId);
            return ok ? (true, "更新成功") : (false, "数据源不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新数据源失败");
            return (false, $"更新失败: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteDataSourceAsync(long id)
    {
        var ok = await _reportRepository.DeleteDataSourceAsync(id);
        return ok ? (true, "删除成功") : (false, "数据源不存在");
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(string dbType, string connectionString)
    {
        try
        {
            var ok = await _reportRepository.TestConnectionAsync(dbType, connectionString);
            return ok ? (true, "连接成功") : (false, "不支持的数据库类型");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据源连接测试失败");
            return (false, $"连接失败: {ex.Message}");
        }
    }

    // ========== 业务员 ==========

    public async Task<List<SalespersonDto>> GetSalespersonsAsync(string? datasource)
    {
        var list = new List<SalespersonDto>();
        try
        {
            // 如果未指定数据源，自动查找 ERP SQL Server 数据源
            if (string.IsNullOrWhiteSpace(datasource))
            {
                var erpDs = await _reportRepository.GetDataSourceByNameAsync("erp");
                if (erpDs == null || erpDs.DbType != "sqlserver")
                {
                    // 尝试查找任意启用的 SQL Server 数据源
                    var allDs = await _reportRepository.GetDataSourcesAsync();
                    erpDs = allDs.FirstOrDefault(d => d.DbType == "sqlserver" && d.IsEnabled);
                }
                if (erpDs != null)
                    datasource = erpDs.Name;
            }

            using var conn = await CreateDataSourceConnectionAsync(datasource);
            if (conn is SqlConnection or OdbcCompatibleConnection)
            {
                // U8 Person 表：cPersonCode=编码, cPersonName=名称, cDepCode=部门编码
                // U8 Department 表：cDepCode=部门编码, cDepName=部门名称
                // 仅筛选业务部人员（部门名称含"业务"）
                var rows = await conn.QueryAsync<(string Code, string Name)>(
                    @"SELECT p.cPersonCode, p.cPersonName
                      FROM Person p
                      INNER JOIN Department d ON d.cDepCode = p.cDepCode
                      WHERE d.cDepName LIKE N'%业务%'
                      ORDER BY p.cPersonCode");
                foreach (var (code, name) in rows)
                    list.Add(new SalespersonDto { Code = code, Name = name });
            }
            _logger.LogInformation("[GetSalespersons] 数据源={Ds}, 获取了 {Count} 个业务员", datasource, list.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取业务员列表失败");
        }
        return list;
    }

    public async Task<CurrentUserSalespersonDto> GetCurrentUserSalespersonInfoAsync(long userId)
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            var row = await conn.QueryFirstOrDefaultAsync(
                @"SELECT sp.salesperson_code, sp.salesperson_name, sp.type
                  FROM erp_settings_salesperson_map sp
                  INNER JOIN sys_users u ON u.employee_id = sp.employee_id
                  WHERE u.id = @UserId AND u.is_deleted = FALSE",
                new { UserId = userId });

            var code = row?.salesperson_code as string;
            var name = row?.salesperson_name as string;
            var type = row?.type as string;

            return new CurrentUserSalespersonDto
            {
                IsSalesperson = code != null,
                SalespersonCode = code,
                SalespersonName = name,
                Type = type
            };
        }
        catch (Exception ex) when (ex.Message.Contains("does not exist"))
        {
            _logger.LogWarning(ex, "业务员映射表不存在，返回默认值");
            return new CurrentUserSalespersonDto { IsSalesperson = false };
        }
    }

    public async Task<List<SalespersonMappingDto>> GetSalespersonMappingsAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<SalespersonMappingDto>(
            @"SELECT sp.employee_id AS EmployeeId, e.name AS EmployeeName,
                     e.employee_no AS EmployeeNo, d.name AS DepartmentName,
                     sp.salesperson_code AS SalespersonCode, sp.salesperson_name AS SalespersonName,
                     sp.type AS Type, sp.created_at AS CreatedAt
              FROM erp_settings_salesperson_map sp
              INNER JOIN sys_employees e ON e.id = sp.employee_id
              LEFT JOIN sys_departments d ON d.id = e.department_id
              ORDER BY e.name");
        return rows.AsList();
    }

    public async Task<(bool Success, string Message)> SaveSalespersonMappingAsync(SaveSalespersonMappingRequest request)
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM erp_settings_salesperson_map WHERE employee_id = @EmployeeId",
                new { EmployeeId = request.EmployeeId });

            if (exists > 0)
            {
                await conn.ExecuteAsync(
                    @"UPDATE erp_settings_salesperson_map SET salesperson_code = @Code, salesperson_name = @Name, type = @Type
                      WHERE employee_id = @EmployeeId",
                    new { Code = request.SalespersonCode, Name = request.SalespersonName, Type = request.Type, EmployeeId = request.EmployeeId });
            }
            else
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO erp_settings_salesperson_map (employee_id, salesperson_code, salesperson_name, type)
                      VALUES (@EmployeeId, @Code, @Name, @Type)",
                    new { EmployeeId = request.EmployeeId, Code = request.SalespersonCode, Name = request.SalespersonName, Type = request.Type });
            }

            _logger.LogInformation("[SaveSalespersonMapping] 保存映射 employeeId={Id}, code={Code}, name={Name}, type={Type}",
                request.EmployeeId, request.SalespersonCode, request.SalespersonName, request.Type);
            return (true, "保存成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存业务员映射失败");
            return (false, $"保存失败: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteSalespersonMappingAsync(long employeeId)
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteAsync(
                "DELETE FROM erp_settings_salesperson_map WHERE employee_id = @EmployeeId",
                new { EmployeeId = employeeId });
            _logger.LogInformation("[DeleteSalespersonMapping] 删除映射 employeeId={Id}", employeeId);
            return (true, "删除成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除业务员映射失败");
            return (false, $"删除失败: {ex.Message}");
        }
    }

    // ========== 过滤条件生成 ==========

    /// <summary>
    /// 根据过滤配置生成 WHERE 条件并拼接到 SQL（使用参数化查询替代字符串拼接）
    /// </summary>
    private (string Sql, Dictionary<string, object?> ExtraParams) BuildFilterConditions(
        string sql,
        List<ReportFilterDto>? filters,
        Dictionary<string, object?> currentParams)
    {
        var extraParams = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (filters == null || filters.Count == 0)
            return (sql, extraParams);

        var conditions = new List<string>();
        var paramIndex = 0;

        foreach (var filter in filters)
        {
            // 根据 fieldName 查找参数值（大小写不敏感）
            if (!currentParams.TryGetValue(filter.FieldName, out var rawValue))
                continue;
            if (rawValue == null)
                continue;

            // 跳过空字符串
            if (rawValue is string s && string.IsNullOrEmpty(s))
                continue;

            var op = filter.Operator?.ToLowerInvariant() ?? "eq";
            var field = filter.FieldName;

            // 字段名校验：仅允许字母、数字、下划线、点号（防止 SQL 注入）
            if (!SafeFieldNameRegex.IsMatch(field))
            {
                _logger.LogWarning("[BuildFilterConditions] 跳过不安全的字段名 '{Field}'", field);
                continue;
            }

            // 自动检测：如果值是数组类型，强制走 between（兜底前端未传正确 operator 的情况）
            var isArrayValue = rawValue is System.Text.Json.JsonElement je
                && je.ValueKind == System.Text.Json.JsonValueKind.Array;
            if (isArrayValue && op == "eq")
            {
                op = "between";
                _logger.LogInformation("[BuildFilterConditions] 自动检测到数组值，将操作符改为 between");
            }

            // 将值归一化为普通类型
            var normalized = ConvertJsonElement(rawValue);

            switch (op)
            {
                case "eq":
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} = @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
                case "ne":
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} != @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
                case "gt":
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} > @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
                case "ge":
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} >= @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
                case "lt":
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} < @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
                case "le":
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} <= @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
                case "like":
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} LIKE @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
                case "between":
                {
                    var (start, end) = ExtractBetweenValues(rawValue);
                    if (start != null && end != null)
                    {
                        var pStart = $"_bf{paramIndex++}";
                        var pEnd = $"_bf{paramIndex++}";
                        conditions.Add($"{field} BETWEEN @{pStart} AND @{pEnd}");
                        extraParams[pStart] = start;
                        extraParams[pEnd] = end;
                        _logger.LogInformation("[BuildFilterConditions] between: {Field} 从 {Start} ~ {End}", field, start, end);
                    }
                    break;
                }
                case "in":
                {
                    var items = ExtractInValues(rawValue);
                    if (items.Count > 0)
                    {
                        var placeholders = new List<string>();
                        foreach (var item in items)
                        {
                            var pName = $"_bf{paramIndex++}";
                            placeholders.Add($"@{pName}");
                            extraParams[pName] = item;
                        }
                        conditions.Add($"{field} IN ({string.Join(", ", placeholders)})");
                    }
                    break;
                }
                default:
                {
                    var pName = $"_bf{paramIndex++}";
                    conditions.Add($"{field} = @{pName}");
                    extraParams[pName] = normalized;
                    break;
                }
            }
        }

        if (conditions.Count == 0)
            return (sql, extraParams);

        var filterClause = string.Join(" AND ", conditions);
        var modifiedSql = AppendWhereClause(sql, filterClause);

        _logger.LogInformation("[BuildFilterConditions] 生成条件: {Conditions}", filterClause);
        _logger.LogInformation("[BuildFilterConditions] 修改后SQL: {Sql}", modifiedSql);
        foreach (var (k, v) in extraParams)
            _logger.LogInformation("[BuildFilterConditions] 额外参数 {Key} = {Value} ({Type})", k, v, v?.GetType().Name ?? "null");

        return (modifiedSql, extraParams);
    }

    /// <summary>
    /// 将过滤条件拼接到 SQL 的 WHERE 子句之后
    /// </summary>
    private static string AppendWhereClause(string sql, string filterClause)
    {
        sql = sql.TrimEnd(' ', '\t', '\n', '\r', ';');

        // 从右向左查找 ORDER BY / GROUP BY / HAVING / WINDOW / LIMIT / OFFSET / FOR 等尾部子句
        var tailMatch = Regex.Match(sql,
            @"\s+(ORDER\s+BY|GROUP\s+BY|HAVING\s+|WINDOW\s+|LIMIT\s+|OFFSET\s+|FOR\s+|UNION\s+)\s+",
            RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

        string mainPart;
        string? suffix = null;

        if (tailMatch.Success)
        {
            mainPart = sql[..tailMatch.Index];
            suffix = sql[tailMatch.Index..];
        }
        else
        {
            mainPart = sql;
        }

        // 移除末尾的 -- 行注释（避免注释吃掉后续追加的条件）
        mainPart = Regex.Replace(mainPart, @"\s*--\s*[^\n]*$", "", RegexOptions.IgnoreCase);
        mainPart = mainPart.TrimEnd();

        // 在主部分中查找 WHERE（简单方式：匹配最后一个顶层 WHERE）
        if (Regex.IsMatch(mainPart, @"\bWHERE\b", RegexOptions.IgnoreCase))
            mainPart += $"\n    AND ({filterClause})";
        else
            mainPart += $"\n    WHERE ({filterClause})";

        return suffix != null ? mainPart + "\n" + suffix : mainPart;
    }

    /// <summary>
    /// 从 daterange 值中提取 start/end（支持数组或 JSON 字符串）
    /// </summary>
    private static (object? Start, object? End) ExtractBetweenValues(object? value)
    {
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var items = je.EnumerateArray().ToList();
            if (items.Count >= 2)
                return (ParseDateValue(items[0]), ParseDateValue(items[1]));
            return (null, null);
        }

        // JSON 字符串数组：["2026-05-31T16:00:00.000Z","2026-06-07T16:00:00.000Z"]
        if (value is string jsonStr && jsonStr.StartsWith('[') && jsonStr.EndsWith(']'))
        {
            try
            {
                var arr = JsonSerializer.Deserialize<List<string>>(jsonStr);
                if (arr?.Count >= 2)
                    return (ParseDateTimeString(arr[0]), ParseDateTimeString(arr[1]));
            }
            catch { }
        }

        return (null, null);
    }

    private static object? ParseDateValue(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            var str = element.GetString();
            return ParseDateTimeString(str);
        }
        return element.GetRawText();
    }

    private static object? ParseDateTimeString(string? str)
    {
        if (string.IsNullOrEmpty(str)) return null;
        // 尝试解析 ISO 日期
        if (DateTime.TryParse(str, out var dt))
            return dt;
        return str;
    }

    /// <summary>
    /// 将值格式化为 SQL 字面量（防注入：转义单引号）
    /// </summary>
    private static string FormatSqlLiteral(object? value)
    {
        if (value == null) return "NULL";
        if (value is string s) return $"'{s.Replace("'", "''")}'";
        if (value is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
        if (value is bool b) return b ? "1" : "0";
        if (value is short or int or long or float or double or decimal) return value.ToString()!;
        return $"'{value.ToString()?.Replace("'", "''")}'";
    }

    /// <summary>
    /// 将参数名中的特殊字符替换为下划线（如 som.ddate -> som_ddate）
    /// </summary>
    private static string SanitizeParamName(string name)
    {
        return name.Replace('.', '_').Replace('-', '_').Replace(' ', '_')
                   .Replace('(', '_').Replace(')', '_');
    }

    /// <summary>
    /// 从 in 值中提取列表
    /// </summary>
    private static List<object?> ExtractInValues(object? value)
    {
        var result = new List<object?>();
        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in je.EnumerateArray())
                result.Add(item.ValueKind == System.Text.Json.JsonValueKind.String ? item.GetString() : item.GetRawText());
        }
        return result;
    }

    /// <summary>
    /// 构建存储过程参数，从 sys.parameters 收集元数据并过滤请求参数
    /// </summary>
    private async Task<DynamicParameters> BuildStoredProcedureParameters(
        string procName,
        HashSet<string> allParamNames,
        HashSet<string> requiredParams,
        Dictionary<string, object?>? requestParams,
        IDbConnection conn)
    {
        var parameters = new DynamicParameters();

        // 从 sys.parameters 收集所有参数名（用于过滤多余的请求参数）
        if (conn is SqlConnection or OdbcCompatibleConnection)
        {
            try
            {
                var paramRows = await conn.QueryAsync<(string Name, bool HasDefault)>(
                    @"SELECT SUBSTRING(p.name, 2, LEN(p.name)) AS Name,
                             p.has_default_value AS HasDefault
                      FROM sys.parameters p
                      WHERE p.object_id = OBJECT_ID(@ProcName)
                      ORDER BY p.parameter_id",
                    new { ProcName = procName });
                foreach (var (name, hasDefault) in paramRows)
                {
                    allParamNames.Add(name);
                    if (!hasDefault)
                        requiredParams.Add(name);
                }
                _logger.LogInformation("[ExecuteStoredProcedure] {ProcName} 从 sys.parameters 返回 {Count} 个参数: {Names}",
                    procName, allParamNames.Count, string.Join(", ", allParamNames));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "无法获取存储过程 {ProcName} 的参数元数据", procName);
            }
        }

        // 用户传入的参数（只传与 SP 参数名匹配的，避免传入过滤器显示名称等无关参数）
        var normalizedParams = NormalizeParams(requestParams);
        _logger.LogInformation("[ExecuteStoredProcedure] {ProcName} 接收了 {Count} 个请求参数: {ParamKeys}",
            procName, normalizedParams.Count, string.Join(", ", normalizedParams.Keys));
        foreach (var (k, v) in normalizedParams)
            _logger.LogDebug("[ExecuteStoredProcedure]    请求参数 {Key} = {Value} ({Type})", k, v, v?.GetType().Name ?? "null");
        if (normalizedParams.Count > 0)
        {
            foreach (var (key, value) in normalizedParams)
            {
                // 有 allParamNames 时做过滤，无法获取元数据时不过滤（回退兼容）
                if (allParamNames.Count > 0 && !allParamNames.Contains(key))
                {
                    _logger.LogInformation("[ExecuteStoredProcedure]    忽略参数 {Key}: 存储过程无此参数", key);
                    continue;
                }
                if (value is DateTime dtVal2)
                {
                    parameters.Add(key, dtVal2);
                }
                else if (value is string str && DateTime.TryParse(str, out var dt))
                {
                    parameters.Add(key, dt);
                }
                else
                    parameters.Add(key, value);
            }
        }

        // 自动填充缺失的必需参数为 NULL
        foreach (var pName in requiredParams)
        {
            if (!normalizedParams.ContainsKey(pName))
                parameters.Add(pName, null);
        }

        return parameters;
    }

    /// <summary>
    /// 处理排序参数 — 如果请求中有排序，覆盖 SQL 中原有的 ORDER BY
    /// </summary>
    private string BuildOrderByClause(string sql, List<ReportSortOption>? sorts, bool isSqlServer)
    {
        if (sorts?.Count is null or 0) return sql;

        var orderClauses = new List<string>();
        foreach (var s in sorts)
        {
            var dir = s.Direction?.ToUpperInvariant() == "DESC" ? "DESC" : "ASC";
            var field = isSqlServer
                ? $"[{s.Field.Replace("]", "]]")}]"
                : $"\"{s.Field.Replace("\"", "\"\"")}\"";
            orderClauses.Add($"{field} {dir}");
        }

        var result = sql;
        // 从 result 移除最外层 ORDER BY 并追加新的 ORDER BY
        result = Regex.Replace(result, @"\s+ORDER\s+BY\s+[\s\S]*$", "", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.RightToLeft);
        result = result.TrimEnd(' ', '\t', '\n', '\r', ';');
        result += $"\nORDER BY {string.Join(", ", orderClauses)}";
        _logger.LogInformation("[ExecuteRawSql] 应用排序: {OrderBy}", string.Join(", ", orderClauses));
        return result;
    }

    /// <summary>
    /// 将查询结果行转换为列和行，并计算自动汇总
    /// </summary>
    private static (List<ReportColumnDto> Columns, List<Dictionary<string, object?>> Rows, Dictionary<string, object?>? Summary) ConvertRowsToColumns(IEnumerable<dynamic> rows)
    {
        var columns = new List<ReportColumnDto>();
        var resultRows = new List<Dictionary<string, object?>>();
        var isFirst = true;

        foreach (IDictionary<string, object?> row in rows)
        {
            if (isFirst)
            {
                foreach (var key in row.Keys)
                {
                    columns.Add(new ReportColumnDto
                    {
                        Field = key,
                        Title = key,
                        Type = InferType(row[key]),
                        IsSortable = true
                    });
                }
                isFirst = false;
            }
            var dict = new Dictionary<string, object?>(row);
            // DBNull 会导致 System.Text.Json 序列化异常，转换为 null
            foreach (var k in dict.Keys.Where(k => dict[k] is DBNull).ToList())
                dict[k] = null;
            resultRows.Add(dict);
        }

        // 计算自动汇总
        Dictionary<string, object?>? summary = null;
        var numericColumns = columns.Where(c => c.Type is "number" or "money").ToList();
        var textColumns = columns.Where(c => c.Type is "string" or "date" or "boolean").ToList();
        if (numericColumns.Any() || textColumns.Any())
        {
            summary = new Dictionary<string, object?>();
            foreach (var col in numericColumns)
                summary[col.Field] = ComputeSummaryValue(resultRows, col.Field, "sum");
            foreach (var col in textColumns)
                summary[col.Field] = ComputeSummaryValue(resultRows, col.Field, "count");
        }

        return (columns, resultRows, summary);
    }

    /// <summary>
    /// 计算单列的汇总值
    /// </summary>
    /// <param name="rows">数据行列表</param>
    /// <param name="field">字段名</param>
    /// <param name="summaryType">汇总类型: "sum"（求和）或 "count"（去重计数）</param>
    /// <returns>sum 返回 decimal, count 返回 int</returns>
    private static object ComputeSummaryValue(
        List<Dictionary<string, object?>> rows,
        string field,
        string summaryType)
    {
        if (summaryType == "sum")
        {
            return rows.Sum(r =>
            {
                if (r.TryGetValue(field, out var v) && v is IConvertible c)
                    return Convert.ToDecimal(c);
                return 0m;
            });
        }

        // count: 去重计数（如多行相同值的只计1次）
        return rows
            .Select(r => r.TryGetValue(field, out var v) && v != null && !Convert.IsDBNull(v)
                ? (v is string s && string.IsNullOrEmpty(s) ? null : v)
                : null)
            .Where(v => v != null)
            .Distinct()
            .Count();
    }

    // ========== 报表权限 ==========

    public async Task<List<ReportPermissionDto>> GetReportPermissionsAsync(long reportId)
    {
        var perms = await _reportRepository.GetPermissionsAsync(reportId);
        var permList = perms.ToList();
        if (permList.Count == 0) return new();

        using var conn = _connectionFactory.CreateConnection();

        // 批量查询角色名称
        var roleIds = permList
            .Where(p => p.PrincipalType == "role")
            .Select(p => p.PrincipalId)
            .Distinct()
            .ToList();
        var roleDict = new Dictionary<long, string>();
        if (roleIds.Count > 0)
        {
            var roles = await conn.QueryAsync<(long Id, string Name)>(
                "SELECT id, name FROM sys_roles WHERE id IN @Ids AND is_deleted = FALSE",
                new { Ids = roleIds });
            foreach (var (id, name) in roles)
                roleDict[id] = name;
        }

        // 批量查询用户名称
        var userIds = permList
            .Where(p => p.PrincipalType == "user")
            .Select(p => p.PrincipalId)
            .Distinct()
            .ToList();
        var userDict = new Dictionary<long, string>();
        if (userIds.Count > 0)
        {
            var users = await conn.QueryAsync<(long Id, string Name)>(
                "SELECT u.id, COALESCE(e.name, u.username) AS name FROM sys_users u " +
                "LEFT JOIN sys_employees e ON u.employee_id = e.id " +
                "WHERE u.id IN @Ids AND u.is_deleted = FALSE",
                new { Ids = userIds });
            foreach (var (id, name) in users)
                userDict[id] = name;
        }

        // 组装结果
        return permList.Select(p => new ReportPermissionDto
        {
            Id = p.Id,
            ReportId = p.ReportId,
            PrincipalType = p.PrincipalType,
            PrincipalId = p.PrincipalId,
            PrincipalName = p.PrincipalType == "role"
                ? roleDict.GetValueOrDefault(p.PrincipalId) ?? $"角色({p.PrincipalId})"
                : userDict.GetValueOrDefault(p.PrincipalId) ?? $"用户({p.PrincipalId})",
            AccessType = p.AccessType,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    public async Task<bool> CheckPermissionAsync(long reportId, long userId, string accessType)
    {
        return await _reportRepository.HasPermissionAsync(reportId, userId, accessType);
    }

    public async Task<(bool Success, string Message)> SetReportPermissionAsync(long reportId, SetReportPermissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrincipalType) || request.PrincipalId <= 0)
            return (false, "请选择用户或角色");

        var ok = await _reportRepository.SetPermissionAsync(reportId, request.PrincipalType, request.PrincipalId, request.AccessType);
        return ok ? (true, "授权成功") : (false, "该权限已存在");
    }

    public async Task<(bool Success, string Message)> DeleteReportPermissionAsync(long permissionId)
    {
        var ok = await _reportRepository.DeletePermissionByIdAsync(permissionId);
        return ok ? (true, "已取消授权") : (false, "权限记录不存在");
    }

    public async Task<List<PrincipalOptionDto>> GetPrincipalOptionsAsync()
    {
        var list = new List<PrincipalOptionDto>();
        using var conn = _connectionFactory.CreateConnection();

        // 所有启用的角色（排除已删除）
        var roles = await conn.QueryAsync<(long Id, string Name)>(
            "SELECT id, name FROM sys_roles WHERE status = TRUE AND is_deleted = FALSE ORDER BY name");
        foreach (var (id, name) in roles)
            list.Add(new PrincipalOptionDto { Id = id, Name = name, Type = "role" });

        // 所有启用的用户（排除已删除，按显示名去重）
        var users = await conn.QueryAsync<(long Id, string Name)>(
            "SELECT DISTINCT u.id, COALESCE(e.name, u.username) AS name FROM sys_users u LEFT JOIN sys_employees e ON u.employee_id = e.id WHERE u.status = TRUE AND u.is_deleted = FALSE ORDER BY name");
        foreach (var (id, name) in users)
            list.Add(new PrincipalOptionDto { Id = id, Name = name, Type = "user" });

        return list;
    }

    // ========== 透视表配置 ==========

    public async Task<List<ReportPivotDto>> GetPivotViewsAsync(long reportId, long userId)
    {
        return await _reportRepository.GetPivotViewsAsync(reportId, userId);
    }

    public async Task<(bool Success, string Message, long Id)> SavePivotViewAsync(SavePivotViewRequest request, long userId, string? creatorName = null)
    {
        try
        {
            var name = creatorName ?? request.CreatorName;
            var id = await _reportRepository.SavePivotViewAsync(request, userId, name);
            return (true, "保存成功", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存透视表配置失败");
            return (false, $"保存失败: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Message)> DeletePivotViewAsync(long id, long? userId = null)
    {
        var ok = await _reportRepository.DeletePivotViewAsync(id, userId ?? 0);
        return ok ? (true, "删除成功") : (false, "配置不存在或无权限删除");
    }

    // ========== 透视表共享管理 ==========

    public async Task<List<PivotViewShareDto>> GetSharesAsync(long pivotViewId, long? userId = null)
    {
        if (userId.HasValue && !await _reportRepository.IsPivotViewOwnerAsync(pivotViewId, userId.Value))
            return new List<PivotViewShareDto>();
        return await _reportRepository.GetSharesAsync(pivotViewId);
    }

    public async Task<(bool Success, string Message)> AddShareAsync(PivotViewShareRequest request, long? userId = null)
    {
        if (userId.HasValue && !await _reportRepository.IsPivotViewOwnerAsync(request.PivotViewId, userId.Value))
            return (false, "无权限：只能分享自己的格式");

        try
        {
            var ok = await _reportRepository.AddShareAsync(request);
            return ok ? (true, "共享成功") : (false, "共享失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加共享失败 pivotViewId={Id}", request.PivotViewId);
            return (false, $"共享失败: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RemoveShareAsync(long shareId, long? userId = null)
    {
        try
        {
            // 先获取共享记录的 pivot_view_id 并校验所有权
            var pivotViewId = await _reportRepository.GetSharePivotViewIdAsync(shareId);
            if (pivotViewId == null)
                return (false, "共享记录不存在");
            if (userId.HasValue && !await _reportRepository.IsPivotViewOwnerAsync(pivotViewId.Value, userId.Value))
                return (false, "无权限：只能取消自己格式的共享");

            var ok = await _reportRepository.RemoveShareAsync(shareId);
            return ok ? (true, "已取消共享") : (false, "共享记录不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消共享失败 shareId={Id}", shareId);
            return (false, $"取消共享失败: {ex.Message}");
        }
    }

    // ========== 数据表视图格式配置 ==========

    public async Task<List<ReportTableViewDto>> GetTableViewsAsync(long reportId, long userId)
    {
        try
        {
            return await _reportRepository.GetTableViewsAsync(reportId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据表视图失败 reportId={ReportId} userId={UserId}", reportId, userId);
            return new List<ReportTableViewDto>();
        }
    }

    public async Task<(bool Success, string Message, long Id)> SaveTableViewAsync(SaveTableViewRequest request, long userId, string? creatorName = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ViewName))
                return (false, "请输入视图名称", 0);
            if (string.IsNullOrWhiteSpace(request.ViewParams) || request.ViewParams == "{}")
                return (false, "请先配置列格式", 0);

            if (request.IsLast)
            {
                await _reportRepository.UpdateTableViewLastFlagAsync(request.Id ?? 0, false);
            }

            var id = await _reportRepository.SaveTableViewAsync(request, userId, creatorName);
            return (true, "保存成功", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存数据表视图失败");
            return (false, $"保存失败: {ex.Message}", 0);
        }
    }

    public async Task<(bool Success, string Message)> DeleteTableViewAsync(long id, long? userId = null)
    {
        try
        {
            bool ok;
            if (userId.HasValue)
                ok = await _reportRepository.DeleteTableViewAsync(id, userId.Value);
            else
                ok = await _reportRepository.DeleteTableViewAsync(id);

            return ok ? (true, "已删除") : (false, "视图不存在或无权删除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除数据表视图失败 id={Id}", id);
            return (false, $"删除失败: {ex.Message}");
        }
    }

    public async Task<List<TableViewShareDto>> GetTableViewSharesAsync(long viewId, long? userId = null)
    {
        try
        {
            if (userId.HasValue)
            {
                var isOwner = await _reportRepository.IsTableViewOwnerAsync(viewId, userId.Value);
                if (!isOwner)
                    return new List<TableViewShareDto>();
            }
            return await _reportRepository.GetTableViewSharesAsync(viewId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据表视图共享列表失败 viewId={ViewId}", viewId);
            return new List<TableViewShareDto>();
        }
    }

    public async Task<(bool Success, string Message)> AddTableViewShareAsync(TableViewShareRequest request, long? userId = null)
    {
        try
        {
            if (userId.HasValue)
            {
                var isOwner = await _reportRepository.IsTableViewOwnerAsync(request.ViewId, userId.Value);
                if (!isOwner)
                    return (false, "无权限：只能共享自己的格式");
            }

            var ok = await _reportRepository.AddTableViewShareAsync(request);
            return ok ? (true, "已共享") : (false, "共享失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据表视图共享失败");
            return (false, $"共享失败: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RemoveTableViewShareAsync(long shareId, long? userId = null)
    {
        try
        {
            // 先获取共享记录的 view_id 并校验所有权
            var viewId = await _reportRepository.GetTableViewShareViewIdAsync(shareId);
            if (viewId == null)
                return (false, "共享记录不存在");
            if (userId.HasValue && !await _reportRepository.IsTableViewOwnerAsync(viewId.Value, userId.Value))
                return (false, "无权限：只能取消自己格式的共享");

            var ok = await _reportRepository.RemoveTableViewShareAsync(shareId);
            return ok ? (true, "已取消共享") : (false, "共享记录不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消数据表共享失败 shareId={Id}", shareId);
            return (false, $"取消共享失败: {ex.Message}");
        }
    }

    // ========== 报表配置导入导出 ==========

    public async Task<byte[]> ExportReportsConfigAsync(long userId)
    {
        _logger.LogInformation("[ExportReportsConfig] 用户 {UserId} 开始导出全部报表配置", userId);

        // 仅管理员可导出
        await EnsureAdminAsync(userId);

        var reports = await _reportRepository.GetReportsAsync(null, userId);
        var reportDetails = new List<ReportDetailDto>();
        foreach (var r in reports)
        {
            var detail = await _reportRepository.GetReportDetailAsync(r.Id);
            if (detail != null)
                reportDetails.Add(detail);
        }

        _logger.LogInformation("[ExportReportsConfig] 共导出 {Count} 个报表配置", reportDetails.Count);
        return JsonSerializer.SerializeToUtf8Bytes(reportDetails, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public async Task<(int SuccessCount, int FailCount, string Message)> ImportReportsConfigAsync(string reportsJson, long userId)
    {
        _logger.LogInformation("[ImportReportsConfig] 用户 {UserId} 开始导入报表配置", userId);

        // 仅管理员可导入
        await EnsureAdminAsync(userId);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var reports = JsonSerializer.Deserialize<List<ReportDetailDto>>(reportsJson, options);

        if (reports == null || reports.Count == 0)
            return (0, 0, "导入文件中没有有效的报表配置");

        int success = 0, fail = 0;
        foreach (var report in reports)
        {
            try
            {
                // 重置 ID 以创建新报表
                report.Id = 0;
                if (report.Fields != null)
                    foreach (var f in report.Fields) f.Id = 0;
                if (report.Filters != null)
                    foreach (var f in report.Filters) f.Id = 0;
                if (report.Sorts != null)
                    foreach (var s in report.Sorts) s.Id = 0;
                if (report.Charts != null)
                    foreach (var c in report.Charts) c.Id = 0;

                await _reportRepository.AddReportAsync(report, userId);
                success++;
            }
            catch (Exception ex)
            {
                fail++;
                _logger.LogError(ex, "[ImportReportsConfig] 导入报表 [{Title}] 失败", report.Title);
            }
        }

        _logger.LogInformation("[ImportReportsConfig] 导入完成: 成功 {Success}, 失败 {Fail}", success, fail);
        return (success, fail, $"导入完成：成功 {success} 个，失败 {fail} 个");
    }

    private async Task EnsureAdminAsync(long userId, string? message = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var isAdmin = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM sys_user_roles WHERE user_id = @UserId AND role_id = 1)",
            new { UserId = userId });
        if (!isAdmin)
            throw new UnauthorizedAccessException(message ?? "仅管理员可执行此操作");
    }

    /// <summary>
    /// 安全字段名正则：仅允许字母、数字、下划线、点号（用于防止 SQL 注入）
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex SafeFieldNameRegex = new(
        @"^[a-zA-Z_][a-zA-Z0-9_.]*$", System.Text.RegularExpressions.RegexOptions.Compiled);
}