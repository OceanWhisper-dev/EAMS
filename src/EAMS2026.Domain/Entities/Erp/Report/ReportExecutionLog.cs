namespace EAMS2026.Domain.Entities.Erp.Report;

/// <summary>
/// 报表执行日志
/// </summary>
public class ReportExecutionLog
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public long UserId { get; set; }
    public string? Params { get; set; }
    public int RowCount { get; set; }
    public int DurationMs { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}