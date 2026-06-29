namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表权限
/// </summary>
public class ReportPermission
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    /// <summary>主体类型：role / user</summary>
    public string PrincipalType { get; set; } = string.Empty;
    public long PrincipalId { get; set; }
    /// <summary>访问类型：view / export / manage</summary>
    public string AccessType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}