using EAMS2026.Domain.Entities;

namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表过滤条件配置
/// </summary>
public class ReportFilter : BaseEntity
{
    public long ReportId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>比较操作符：eq / ne / gt / ge / lt / le / like / in / between</summary>
    public string Operator { get; set; } = "eq";
    public string? DefaultValue { get; set; }
    /// <summary>控件类型：text / select / date / daterange / number / checkbox</summary>
    public string ControlType { get; set; } = "text";
    /// <summary>下拉框选项SQL（仅select类型）</summary>
    public string? OptionsQuery { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}