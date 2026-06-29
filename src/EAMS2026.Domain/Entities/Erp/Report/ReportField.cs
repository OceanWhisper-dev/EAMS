using EAMS2026.Domain.Entities;

namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表字段定义
/// </summary>
public class ReportField : BaseEntity
{
    public long ReportId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldTitle { get; set; } = string.Empty;
    /// <summary>字段类型：string / number / date / boolean / money</summary>
    public string FieldType { get; set; } = "string";
    public int SortOrder { get; set; }
    /// <summary>列宽(px)，0=自动</summary>
    public int Width { get; set; }
    /// <summary>对齐方式：left / center / right</summary>
    public string Align { get; set; } = "left";
    public bool IsDisplay { get; set; } = true;
    public bool IsSortable { get; set; } = true;
    public bool IsFilterable { get; set; }
    public bool IsGroupable { get; set; }
    public bool IsSummary { get; set; }
    /// <summary>汇总类型：sum / avg / count / max / min</summary>
    public string? SummaryType { get; set; }
    /// <summary>格式化模式，如 #,##0.00</summary>
    public string? FormatPattern { get; set; }
}