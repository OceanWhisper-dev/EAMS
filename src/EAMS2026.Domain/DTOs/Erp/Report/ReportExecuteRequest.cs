using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.Erp.Report;

/// <summary>
/// 报表执行请求
/// </summary>
public class ReportExecuteRequest
{
    /// <summary>查询参数，key=参数名(不含@), value=参数值</summary>
    public Dictionary<string, object?> Params { get; set; } = new();
    /// <summary>分页信息</summary>
    public ReportPagination? Pagination { get; set; }
    /// <summary>排序（覆盖默认排序）</summary>
    public List<ReportSortOption>? Sort { get; set; }
}

public class ReportPagination
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ReportSortOption
{
    [Required(ErrorMessage = "排序字段不能为空")]
    public string Field { get; set; } = string.Empty;
    public string Direction { get; set; } = "asc";
}