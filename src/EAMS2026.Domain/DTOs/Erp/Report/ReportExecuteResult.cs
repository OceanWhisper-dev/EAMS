namespace EAMS2026.Domain.DTOs.Erp.Report;

/// <summary>
/// 报表执行结果
/// </summary>
public class ReportExecuteResult
{
    public List<ReportColumnDto> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public Dictionary<string, object?>? Summary { get; set; }
    public ReportPaginationResult? Pagination { get; set; }
    public ReportExecutionInfo? ExecutionInfo { get; set; }
}

public class ReportColumnDto
{
    public string Field { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public int Width { get; set; }
    public string Align { get; set; } = "left";
    public bool IsSortable { get; set; } = true;
    public string? SummaryType { get; set; }
    public string? FormatPattern { get; set; }
}

public class ReportPaginationResult
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}

public class ReportExecutionInfo
{
    public int DurationMs { get; set; }
    public int RowCount { get; set; }
}