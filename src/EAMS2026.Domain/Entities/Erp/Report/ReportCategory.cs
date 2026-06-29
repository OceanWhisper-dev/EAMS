using EAMS2026.Domain.Entities;

namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表分类
/// </summary>
public class ReportCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }

    // 导航属性
    public ReportCategory? Parent { get; set; }
    public List<ReportCategory> Children { get; set; } = new();
    public List<ReportEntity> Reports { get; set; } = new();
}