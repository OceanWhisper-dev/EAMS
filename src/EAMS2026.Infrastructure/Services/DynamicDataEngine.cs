using System.Text.RegularExpressions;
using System.Text.Json;
using Dapper;
using EAMS2026.Domain.Interfaces;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Services;

public class DynamicDataEngine : IDynamicDataEngine
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// 已知安全的数据库表名白名单（防止 SQL 注入通过表名注入）
    /// </summary>
    private static readonly HashSet<string> KnownSafeTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "sys_users",
        "sys_roles",
        "sys_employees",
        "sys_departments",
        "sys_operation_logs",
        "sys_messages",
        "sys_user_roles",
        "sys_permissions",
        "sys_role_permissions",
        "sys_notifications"
    };

    public DynamicDataEngine(DbConnectionFactory connectionFactory, HttpClient httpClient)
    {
        _connectionFactory = connectionFactory;
        _httpClient = httpClient;
    }

    /// <summary>
    /// 验证表名是否在白名单中，防止 SQL 注入
    /// </summary>
    private static void ValidateTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("表名不能为空");
        if (!KnownSafeTables.Contains(tableName))
            throw new ArgumentException($"不安全的表名 '{tableName}'，仅允许预定义的安全表名");
    }

    public async Task<object> ExecuteAsync(string dataSourceType, string? dataSourceConfig)
    {
        if (string.IsNullOrEmpty(dataSourceConfig)) return new Dictionary<string, object>();

        Dictionary<string, object> config;
        try
        {
            config = JsonSerializer.Deserialize<Dictionary<string, object>>(dataSourceConfig)
                ?? new Dictionary<string, object>();
        }
        catch (JsonException)
        {
            throw new ArgumentException("数据源配置不是有效的JSON格式，请输入正确的JSON字符串");
        }

        // 如果是 builtin 类型，从配置中读取真正的类型
        if (dataSourceType == "builtin" && config.TryGetValue("type", out var typeObj))
        {
            dataSourceType = typeObj.ToString() ?? "";
        }

        return dataSourceType switch
        {
            "sql" => await ExecuteSqlAsync(config),
            "api" => await ExecuteApiAsync(config),
            "count" => await ExecuteCountAsync(config),
            "recent_logs" => await ExecuteRecentLogsAsync(config),
            "unread_messages" => await ExecuteUnreadMessagesAsync(),
            _ => new Dictionary<string, object>()
        };
    }

    private async Task<object> ExecuteSqlAsync(Dictionary<string, object> config)
    {
        if (!config.TryGetValue("sql", out var sqlObj) || sqlObj == null)
            return new Dictionary<string, object>();

        var sql = sqlObj.ToString() ?? "";
        var parameters = new Dictionary<string, object>();

        if (config.TryGetValue("parameters", out var paramObj) && paramObj != null)
        {
            var paramJson = paramObj.ToString() ?? "{}";
            parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(paramJson) ?? new Dictionary<string, object>();
        }

        // 尝试从 SQL 中提取表名进行白名单验证
        ExtractAndValidateTableNames(sql);

        using var conn = _connectionFactory.CreateConnection();
        var dynamicParams = new DynamicParameters();
        foreach (var (key, value) in parameters)
        {
            dynamicParams.Add(key, value);
        }

        var rows = await conn.QueryAsync(sql, dynamicParams);
        return rows.ToList();
    }

    /// <summary>
    /// 从 SQL 中提取表名并进行白名单验证
    /// </summary>
    private static void ExtractAndValidateTableNames(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return;

        // 使用正则提取 FROM 或 JOIN 后的表名（简单提取，防止 sqlmap 类型注入）
        var tableMatches = Regex.Matches(sql,
            @"\b(?:FROM|JOIN)\s+([a-zA-Z_][a-zA-Z0-9_]*)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (System.Text.RegularExpressions.Match match in tableMatches)
        {
            if (match.Success)
            {
                var tableName = match.Groups[1].Value;
                if (!KnownSafeTables.Contains(tableName))
                    throw new ArgumentException($"SQL 中引用了不安全的表名 '{tableName}'，仅允许预定义的安全表名");
            }
        }
    }

    private async Task<object> ExecuteApiAsync(Dictionary<string, object> config)
    {
        if (!config.TryGetValue("url", out var urlObj) || urlObj == null)
            return new Dictionary<string, object>();

        var url = urlObj.ToString() ?? "";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<object>(content) ?? new Dictionary<string, object>();
    }

    private async Task<object> ExecuteCountAsync(Dictionary<string, object> config)
    {
        string table = "";
        string condition = "TRUE";

        if (config.TryGetValue("table", out var tableObj) && tableObj != null)
        {
            table = tableObj is JsonElement je ? je.GetString() ?? "" : tableObj.ToString() ?? "";
        }

        if (config.TryGetValue("condition", out var conditionObj) && conditionObj != null)
        {
            condition = conditionObj is JsonElement je ? je.GetString() ?? "TRUE" : conditionObj.ToString() ?? "TRUE";
        }

        if (string.IsNullOrEmpty(table)) return 0;
        ValidateTableName(table);

        using var conn = _connectionFactory.CreateConnection();
        var sql = $"SELECT COUNT(1) FROM {table} WHERE {condition}";
        return await conn.ExecuteScalarAsync<int>(sql);
    }

    // ⚠️ condition 来自 JSON 配置，作为裸 SQL 片段拼接 —
    // 若配置来源不可信，可能引入 SQL 注入风险。
    // 当前仅在仪表盘组件内使用，由管理员配置。
    private async Task<object> ExecuteRecentLogsAsync(Dictionary<string, object> config)
    {
        var limit = 10;
        if (config.ContainsKey("limit"))
        {
            limit = config["limit"] is JsonElement je ? je.GetInt32() : Convert.ToInt32(config["limit"]);
        }

        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT id, username, module, operation_type as operationType, description, 
                    created_at as createdAt FROM sys_operation_logs 
                    ORDER BY created_at DESC LIMIT @Limit";
        var rows = await conn.QueryAsync(sql, new { Limit = limit });
        return rows.Select(r => new Dictionary<string, object>
        {
            { "id", r.id },
            { "username", r.username },
            { "module", r.module },
            { "operationType", r.operationtype },
            { "description", r.description },
            { "createdAt", r.createdat?.ToString() ?? "" }
        }).ToList();
    }

    private async Task<object> ExecuteUnreadMessagesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM sys_messages WHERE is_read = FALSE AND is_deleted = FALSE");
    }
}
