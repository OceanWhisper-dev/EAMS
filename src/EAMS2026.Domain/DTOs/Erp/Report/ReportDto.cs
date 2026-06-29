namespace EAMS2026.Domain.DTOs.Erp.Report;

/// <summary>
/// 报表摘要（列表用）
/// </summary>
public class ReportDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string QueryType { get; set; } = "sql";
    public string? QueryDatasource { get; set; }
    public string? QueryDatasourceName { get; set; }
    public string Status { get; set; } = "draft";
    public bool IsSystem { get; set; }
    public bool IsBookmarked { get; set; }
    public bool CanManage { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
}