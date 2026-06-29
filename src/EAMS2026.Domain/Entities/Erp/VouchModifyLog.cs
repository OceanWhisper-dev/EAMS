namespace EAMS2026.Domain.Entities.Erp;

/// <summary>
/// 单据修改操作日志（PostgreSQL）
/// </summary>
public class VouchModifyLog
{
    public long Id { get; set; }
    /// <summary>单据类型: order / dispatch</summary>
    public string VouchType { get; set; } = string.Empty;
    /// <summary>单据ID (soid / dlid)</summary>
    public long VouchId { get; set; }
    /// <summary>单据编号 (cSOCode / cDLCode)</summary>
    public string VouchCode { get; set; } = string.Empty;
    /// <summary>修改字段</summary>
    public string FieldName { get; set; } = string.Empty;
    /// <summary>原值</summary>
    public string? OldValue { get; set; }
    /// <summary>新值</summary>
    public string? NewValue { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public DateTime OperateAt { get; set; } = DateTime.UtcNow;
    /// <summary>状态: SUCCESS / FAILED</summary>
    public string Status { get; set; } = "SUCCESS";
    public string? ErrorMsg { get; set; }
}