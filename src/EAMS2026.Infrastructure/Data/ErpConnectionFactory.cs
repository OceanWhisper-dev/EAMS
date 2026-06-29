using Dapper;
using System.Data;
using System.Data.Odbc;
using System.Text.RegularExpressions;

// CS0618: System.Data.SqlClient 已过时，但 Windows 上兼容 SQL Server 2005 仍需使用
#pragma warning disable CS0618

namespace EAMS2026.Infrastructure.Data;

/// <summary>
/// U8 ERP 数据库连接工厂（SQL Server）
/// 从 erp_settings_datasource 表实时读取 ERP 连接字符串（共享报表模块的数据源配置）
/// 与报表模块的 CreateDataSourceConnectionAsync 行为一致：每次都从数据库读取最新连接字符串
/// 支持按年份切换数据库：UFDATA_{账套号}_{年份}
///
/// 平台兼容：
/// - Windows：使用 System.Data.SqlClient（直接支持 SQL Server 2005）
/// - Linux：使用 System.Data.Odbc + FreeTDS（绕过 OpenSSL 3.0 的 TLS 1.0 兼容问题）
/// </summary>
public partial class ErpConnectionFactory
{
    private readonly DbConnectionFactory _dbFactory;
    private readonly string _accountSet;
    private readonly string _dataSourceName;
    private static readonly bool _isLinux = OperatingSystem.IsLinux();

    public ErpConnectionFactory(DbConnectionFactory dbFactory, string accountSet, string dataSourceName)
    {
        _dbFactory = dbFactory;
        _accountSet = accountSet;
        _dataSourceName = dataSourceName;
    }

    /// <summary>
    /// 从 erp_settings_datasource 表实时加载 ERP 连接字符串（按 name 查询），不缓存
    /// </summary>
    private string GetConnectionString()
    {
        using var conn = _dbFactory.CreateConnection();
        var connectionString = conn.QueryFirstOrDefault<string>(
            @"SELECT connection_string FROM erp_settings_datasource
              WHERE name = @Name AND is_deleted = FALSE AND is_enabled = TRUE",
            new { Name = _dataSourceName });

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                $"ERP 数据源未在 erp_settings_datasource 表中配置。请先在 系统设置 > 数据源配置 中添加名称为 '{_dataSourceName}' 的 SQL Server 数据源。");

        return connectionString;
    }

    /// <summary>使用 erp_settings_datasource 中配置的 ERP 连接字符串创建连接</summary>
    public IDbConnection CreateConnection()
    {
        var connStr = GetConnectionString();
        return _isLinux
            ? new OdbcCompatibleConnection(CreateOdbcConnection(connStr))
            : new global::System.Data.SqlClient.SqlConnection(connStr);
    }

    /// <summary>为指定年份创建连接，数据库名 = UFDATA_{账套号}_{year}</summary>
    public IDbConnection CreateConnection(int year)
    {
        var database = $"UFDATA_{_accountSet}_{year}";
        var connStr = GetConnectionString();

        if (_isLinux)
        {
            var odbcStr = ConvertToOdbcConnectionString(connStr);
            odbcStr = SetDatabaseInOdbcString(odbcStr, database);
            return new OdbcCompatibleConnection(new OdbcConnection(odbcStr));
        }
        else
        {
            var builder = new global::System.Data.SqlClient.SqlConnectionStringBuilder(connStr)
            {
                InitialCatalog = database
            };
            return new global::System.Data.SqlClient.SqlConnection(builder.ConnectionString);
        }
    }

    /// <summary>获取指定年份的数据库名</summary>
    public string GetDatabaseName(int year) => $"UFDATA_{_accountSet}_{year}";

    // ==================== Linux ODBC 连接创建 ====================

    /// <summary>
    /// 创建 ODBC 连接，并将 SqlClient 格式的连接字符串转为 FreeTDS ODBC 格式
    /// </summary>
    private static OdbcConnection CreateOdbcConnection(string sqlClientConnStr)
    {
        var odbcStr = ConvertToOdbcConnectionString(sqlClientConnStr);
        return new OdbcConnection(odbcStr);
    }

    /// <summary>
    /// 将 SqlClient 格式的连接字符串转为 FreeTDS ODBC 格式
    /// </summary>
    internal static string ConvertToOdbcConnectionString(string sqlClientConnStr)
    {
        // 解析 SqlClient 格式
        var server = ExtractValue(sqlClientConnStr, "Server", "Data Source", "Address", "Addr", "Network Address");
        var database = ExtractValue(sqlClientConnStr, "Database", "Initial Catalog");
        var user = ExtractValue(sqlClientConnStr, "User Id", "User ID", "Uid", "User");
        var password = ExtractValue(sqlClientConnStr, "Password", "Pwd");

        // 如果连接字符串中包含 Port=，直接使用
        var port = ExtractValue(sqlClientConnStr, "Port");
        if (string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(server))
        {
            // 从 Server 中提取端口 (host,port)
            var portMatch = ServerPortRegex().Match(server);
            if (portMatch.Success)
            {
                server = portMatch.Groups[1].Value;
                port = portMatch.Groups[2].Value;
            }
        }
        if (string.IsNullOrEmpty(port)) port = "1433";

        if (string.IsNullOrEmpty(server))
            throw new ArgumentException("无法从连接字符串中解析 Server 地址");

        var sb = new System.Text.StringBuilder();
        sb.Append("Driver=FreeTDS;");
        sb.Append($"Server={server};");
        sb.Append($"Port={port};");
        sb.Append($"Database={database ?? "master"};");
        if (!string.IsNullOrEmpty(user)) sb.Append($"Uid={user};");
        if (!string.IsNullOrEmpty(password)) sb.Append($"Pwd={password};");
        sb.Append("TDS_Version=7.0;");
        sb.Append("ClientCharset=UTF-8;");

        return sb.ToString();
    }

    /// <summary>替换 ODBC 连接字符串中的 Database= 值</summary>
    private static string SetDatabaseInOdbcString(string odbcStr, string database)
    {
        return DatabaseRegex().Replace(odbcStr, $"Database={database};");
    }

    /// <summary>从连接字符串中提取指定键的值（不区分大小写）</summary>
    private static string? ExtractValue(string connStr, params string[] keys)
    {
        var keySet = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        foreach (var part in connStr.Split(';'))
        {
            var trimmed = part.Trim();
            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0) continue;
            var k = trimmed[..eqIndex].Trim();
            var v = trimmed[(eqIndex + 1)..].Trim();
            if (keySet.Contains(k) && !string.IsNullOrEmpty(v))
                return v;
        }
        return null;
    }

    [GeneratedRegex(@"^(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}|[a-zA-Z][\w.-]*),(\d+)$")]
    private static partial Regex ServerPortRegex();

    [GeneratedRegex(@"Database=[^;]+")]
    private static partial Regex DatabaseRegex();

    /// <summary>
    /// 追加 SQL Server 2005 兼容参数：禁用加密协商
    /// </summary>
    internal static string AppendSqlServerCompat(string connectionString)
    {
        if (connectionString.IndexOf("Encrypt", StringComparison.OrdinalIgnoreCase) >= 0)
            return connectionString;
        var separator = connectionString.TrimEnd().EndsWith(";") ? "" : ";";
        return connectionString + separator + "Encrypt=False;TrustServerCertificate=True;";
    }
}

#pragma warning restore CS0618