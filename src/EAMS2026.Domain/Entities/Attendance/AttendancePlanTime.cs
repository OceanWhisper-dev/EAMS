namespace EAMS2026.Domain.Entities.Attendance;

public class AttendancePlanTime : BaseEntity
{
    public string PlanName { get; set; } = string.Empty;
    public long DayTypeId { get; set; }
    public string? Description { get; set; }
    public TimeSpan BTime { get; set; }
    public TimeSpan ETime { get; set; }

    public AttendanceDayType? DayType { get; set; }
}