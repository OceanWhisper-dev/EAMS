namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表字段级权限
/// </summary>
public class ReportFieldPermission
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    /// <summary>主体类型：role / user</summary>
    public string PrincipalType { get; set; } = string.Empty;
    public long PrincipalId { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}