namespace EAMS2026.Infrastructure.Services.Erp;

/// <summary>
/// 报表 SQL 引擎：提供 SQL 安全验证功能
/// </summary>
public static class ReportSqlEngine
{
    /// <summary>
    /// 验证 SQL 是否为安全的 SELECT 查询语句
    /// </summary>
    /// <param name="sql">待验证的 SQL 语句</param>
    /// <exception cref="InvalidOperationException">SQL 不是 SELECT 查询或存在注入风险时抛出</exception>
    public static void ValidateSql(string sql)
    {
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("SELECT\n", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("SELECT\r", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("仅允许 SELECT 查询语句");
        }

        // 禁止块注释（可被用于隐藏恶意 SQL）
        if (trimmed.Contains("/*"))
            throw new InvalidOperationException("SQL 中不允许包含块注释 (/* */)");

        // 禁止多条语句（分号后紧跟非空白字符以外的语句关键字）
        var semiIndex = trimmed.IndexOf(';');
        if (semiIndex >= 0)
        {
            var afterSemi = trimmed[(semiIndex + 1)..].TrimStart();
            if (afterSemi.Length > 0 && !afterSemi.StartsWith("--", StringComparison.Ordinal))
                throw new InvalidOperationException("不允许执行多条 SQL 语句");
        }
    }
}