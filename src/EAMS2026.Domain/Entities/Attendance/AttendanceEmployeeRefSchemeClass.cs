namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceEmployeeRefSchemeClass : BaseEntity
{
    public long EmployeeId { get; set; }
    public long ClassId { get; set; }
    public int? PeriodNo { get; set; }
    public DateTime EffDate { get; set; }
    public DateTime? ExpDate { get; set; }

    public AttendanceSchemeClass? Class { get; set; }

    public string? EmployeeName { get; set; }
    public string? ClassName { get; set; }
}