namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceEmployee : BaseEntity
{
    public string EmployeeName { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
    public string Source { get; set; } = "hwatt";
    public long? HwattEmployeeId { get; set; }
    public long? SystemEmployeeId { get; set; }
    public string? SystemEmployeeName { get; set; }
}