namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表默认排序配置
/// </summary>
public class ReportSort
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    /// <summary>方向：asc / desc</summary>
    public string Direction { get; set; } = "asc";
    public int SortOrder { get; set; }
}