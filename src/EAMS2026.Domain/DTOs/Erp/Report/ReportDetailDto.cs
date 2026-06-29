namespace EAMS2026.Domain.DTOs.Erp.Report;

/// <summary>
/// 报表详情（含所有子配置）
/// </summary>
public class ReportDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string QueryType { get; set; } = "sql";
    public string QueryText { get; set; } = string.Empty;
    public string QueryDatasource { get; set; } = "main";
    public bool IsSystem { get; set; }
    public string Status { get; set; } = "draft";
    /// <summary>默认打开标签页：table / pivot</summary>
    public string DefaultTab { get; set; } = "table";
    public List<ReportFieldDto> Fields { get; set; } = new();
    public List<ReportFilterDto> Filters { get; set; } = new();
    public List<ReportSortDto> Sorts { get; set; } = new();
    public List<ReportChartDto> Charts { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReportFieldDto
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string FieldTitle { get; set; } = string.Empty;
    public string FieldType { get; set; } = "string";
    public int SortOrder { get; set; }
    public int Width { get; set; }
    public string Align { get; set; } = "left";
    public bool IsDisplay { get; set; } = true;
    public bool IsSortable { get; set; } = true;
    public bool IsFilterable { get; set; }
    public bool IsGroupable { get; set; }
    public bool IsSummary { get; set; }
    public string? SummaryType { get; set; }
    public string? FormatPattern { get; set; }
}

public class ReportFilterDto
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq";
    public string? DefaultValue { get; set; }
    public string ControlType { get; set; } = "text";
    public string? OptionsQuery { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
}

public class ReportSortDto
{
    public long Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string Direction { get; set; } = "asc";
    public int SortOrder { get; set; }
}

public class ReportChartDto
{
    public long Id { get; set; }
    public string ChartType { get; set; } = "bar";
    public string Title { get; set; } = string.Empty;
    public string XField { get; set; } = string.Empty;
    public string YFields { get; set; } = "[]";
    public string? GroupField { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
}