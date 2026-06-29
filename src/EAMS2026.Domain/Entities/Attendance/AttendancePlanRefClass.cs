namespace EAMS2026.Domain.Entities.Attendance;

public class AttendancePlanRefClass : BaseEntity
{
    public long ClassId { get; set; }
    public long PlanId { get; set; }
    public int? PeriodNo { get; set; }

    public AttendanceSchemeClass? Class { get; set; }
    public AttendancePlanTime? Plan { get; set; }
}