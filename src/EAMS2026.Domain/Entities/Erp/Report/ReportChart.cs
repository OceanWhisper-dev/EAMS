using EAMS2026.Domain.Entities;

namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表图表配置
/// </summary>
public class ReportChart : BaseEntity
{
    public long ReportId { get; set; }
    /// <summary>图表类型：bar / line / pie / scatter / radar</summary>
    public string ChartType { get; set; } = "bar";
    public string Title { get; set; } = string.Empty;
    /// <summary>X轴字段</summary>
    public string XField { get; set; } = string.Empty;
    /// <summary>Y轴字段列表 JSON: ["field1","field2"]</summary>
    public string YFields { get; set; } = "[]";
    /// <summary>分组字段</summary>
    public string? GroupField { get; set; }
    /// <summary>扩展配置 JSON（颜色、堆叠等）</summary>
    public string? Options { get; set; }
    public int SortOrder { get; set; }
}