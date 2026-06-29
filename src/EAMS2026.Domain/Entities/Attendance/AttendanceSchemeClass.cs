namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceSchemeClass : BaseEntity
{
    public string ClassName { get; set; } = string.Empty;
    public int? Periods { get; set; }
    public string? ClassDescription { get; set; }
    public int? ClassPeriods { get; set; }
}