using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Text.RegularExpressions;

namespace EAMS2026.Infrastructure.Data;

/// <summary>
/// ODBC 连接包装器（继承 DbConnection 以支持 Dapper 异步查询）：
/// - 自动将 SQL 中的 @param 命名参数转换为 ? 位置参数（ODBC/FreeTDS 要求）
/// - 仅在 Linux 上连接 SQL Server 2005 时使用
///
/// 为什么要继承 DbConnection 而非仅实现 IDbConnection：
/// Dapper 的异步方法（QueryAsync、ExecuteAsync 等）要求连接是 DbConnection
/// 或 CreateCommand() 返回 DbCommand，否则会抛出 InvalidOperationException。
/// </summary>
public partial class OdbcCompatibleConnection : DbConnection
{
    private readonly OdbcConnection _inner;

    public OdbcCompatibleConnection(OdbcConnection inner)
    {
        _inner = inner;
    }

    // ── DbConnection 抽象成员 ──

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => _inner.BeginTransaction(isolationLevel);

    public override void Close() => _inner.Close();

    public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

    public override string? ConnectionString
    {
#pragma warning disable CS8764, CS8765 // 内层 OdbcConnection 的 ConnectionString 不是可为 null 的，但包装层暴露的 nullable 合同符合 DbConnection 基类要求
        get => _inner.ConnectionString;
        set => _inner.ConnectionString = value!;
#pragma warning restore CS8764, CS8765
    }

    public override string Database => _inner.Database;
    public override string DataSource => _inner.DataSource;
    public override string ServerVersion => _inner.ServerVersion;
    public override ConnectionState State => _inner.State;

    public override void Open() => _inner.Open();

    /// <summary>
    /// 创建 ODBC 兼容命令。
    /// 注意：内层 OdbcCommand 的 Connection 已由 OdbcConnection.CreateCommand()
    /// 自动设为 inner OdbcConnection，此处无需额外设置。
    /// </summary>
    protected override DbCommand CreateDbCommand()
    {
        return new OdbcCompatibleCommand(_inner.CreateCommand());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>
/// ODBC 命令包装器（继承 DbCommand）：
/// 自动将 SQL 中的 @paramName 替换为 ?，适配 ODBC 位置参数。
///
/// Dapper 异步路径要求命令是 DbCommand，因此必须继承 DbCommand。
/// 内层 OdbcCommand 的 Connection 由 OdbcConnection.CreateCommand() 自动维护，
/// 外层 DbConnection 属性仅用于跟踪包装连接供 Dapper 管理连接状态。
/// </summary>
public partial class OdbcCompatibleCommand : DbCommand
{
    private readonly OdbcCommand _inner;
    private DbConnection? _connection;

    public OdbcCompatibleCommand(OdbcCommand inner)
    {
        _inner = inner;
    }

    // ── DbCommand 核心属性 ──

    public override string? CommandText
    {
#pragma warning disable CS8764 // 内层 OdbcCommand 的 CommandText 不是可为 null 的，但包装层暴露的 nullable 合同符合 DbCommand 基类要求
        get => _inner.CommandText;
#pragma warning restore CS8764
        set => _inner.CommandText = ConvertNamedToPositional(value ?? string.Empty);
    }

    public override int CommandTimeout
    {
        get => _inner.CommandTimeout;
        set => _inner.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => _inner.CommandType;
        set => _inner.CommandType = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => _inner.UpdatedRowSource;
        set => _inner.UpdatedRowSource = value;
    }

    public override bool DesignTimeVisible
    {
        get => false;
        set { }
    }

    /// <summary>
    /// DbConnection：Dapper 通过此属性管理连接状态（Open/Close）。
    /// 不会设置到内层 OdbcCommand.Connection（内层已由 CreateCommand() 自动维护）。
    /// </summary>
    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = value;
    }

    protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

    /// <summary>
    /// DbTransaction：Dapper 通过此属性管理事务。
    /// 内层 OdbcCommand 的 Transaction 由实际 OdbcTransaction 管理。
    /// </summary>
    protected override DbTransaction? DbTransaction
    {
        get => _inner.Transaction;
        set => _inner.Transaction = value as OdbcTransaction;
    }

    // ── DbCommand 核心方法 ──

    public override void Cancel() => _inner.Cancel();

    public override int ExecuteNonQuery()
    {
        FixParameterTypes();
        return _inner.ExecuteNonQuery();
    }

    public override object? ExecuteScalar()
    {
        FixParameterTypes();
        return _inner.ExecuteScalar();
    }

    public override void Prepare() => _inner.Prepare();

    protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        FixParameterTypes();
        return _inner.ExecuteReader(behavior);
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        FixParameterTypes();
        return await _inner.ExecuteReaderAsync(behavior, cancellationToken);
    }

    /// <summary>
    /// FreeTDS + SQL Server 2005 下，OdbcParameter 若未显式设置 OdbcType，
    /// ODBC 驱动会将 DateTime 值作为字符传传递，导致 SQL Server 报
    /// "Conversion failed when converting datetime from character string"。
    /// 在执行前扫描参数，对 DateTime 值强制设置 OdbcType = DateTime。
    /// </summary>
    private void FixParameterTypes()
    {
        foreach (OdbcParameter p in _inner.Parameters)
        {
            if (p.Value is DateTime && p.OdbcType != OdbcType.DateTime)
                p.OdbcType = OdbcType.DateTime;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inner.Dispose();
        base.Dispose(disposing);
    }

    // ── 命名参数 → 位置参数转换 ──

    [GeneratedRegex(@"@\w+")]
    private static partial Regex NamedParamRegex();

    private static string ConvertNamedToPositional(string sql)
    {
        return NamedParamRegex().Replace(sql, "?");
    }
}