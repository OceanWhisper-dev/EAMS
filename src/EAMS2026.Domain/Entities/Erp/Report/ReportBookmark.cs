using EAMS2026.Domain.Entities;

namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 用户报表收藏
/// </summary>
public class ReportBookmark : BaseEntity
{
    public long UserId { get; set; }
    public long ReportId { get; set; }
    public int SortOrder { get; set; }

    // 导航属性
    public ReportEntity? Report { get; set; }
}