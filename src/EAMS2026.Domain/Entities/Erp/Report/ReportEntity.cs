using EAMS2026.Domain.Entities;

namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表主表
/// </summary>
public class ReportEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? CategoryId { get; set; }
    /// <summary>查询类型：sql / proc / table</summary>
    public string QueryType { get; set; } = "sql";
    /// <summary>SQL查询语句，参数占位符 @param</summary>
    public string QueryText { get; set; } = string.Empty;
    /// <summary>数据源标识：main / erp</summary>
    public string QueryDatasource { get; set; } = "main";
    /// <summary>系统内置（不可删除）</summary>
    public bool IsSystem { get; set; }
    /// <summary>状态：draft / published / disabled</summary>
    public string Status { get; set; } = "draft";
    /// <summary>默认打开标签页：table / pivot</summary>
    public string DefaultTab { get; set; } = "table";

    // 导航属性
    public ReportCategory? Category { get; set; }
    public List<ReportField> Fields { get; set; } = new();
    public List<ReportFilter> Filters { get; set; } = new();
    public List<ReportSort> Sorts { get; set; } = new();
    public List<ReportChart> Charts { get; set; } = new();
    public List<ReportPermission> Permissions { get; set; } = new();
    public List<ReportFieldPermission> FieldPermissions { get; set; } = new();
}