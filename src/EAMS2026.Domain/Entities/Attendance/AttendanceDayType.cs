namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceDayType : BaseEntity
{
    public string DayTypeName { get; set; } = string.Empty;
    public string? DayTypeCaption { get; set; }
}