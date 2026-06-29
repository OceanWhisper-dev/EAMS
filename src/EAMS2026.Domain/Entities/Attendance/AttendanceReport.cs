namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceReport
{
    public long RecordId { get; set; }
    public long EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateOnly SDate { get; set; }
    public string? Description { get; set; }
    public TimeOnly? BAttTime { get; set; }
    public int? BOffset { get; set; }
    public TimeOnly? EAttTime { get; set; }
    public int? EOffset { get; set; }
    public decimal? BOffsetFee { get; set; }
    public decimal? EOffsetFee { get; set; }
    public string? Event { get; set; }
    public decimal? Fee { get; set; }
}