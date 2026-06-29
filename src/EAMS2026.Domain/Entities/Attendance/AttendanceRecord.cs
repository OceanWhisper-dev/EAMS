namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceRecord : BaseEntity
{
    public long EmployeeId { get; set; }
    public int PeriodNo { get; set; }
    public DateTime SDate { get; set; }
    public TimeSpan? BAttTime { get; set; }
    public TimeSpan? EAttTime { get; set; }
    public int? BOffset { get; set; }
    public int? EOffset { get; set; }
    public decimal? BOffsetFee { get; set; }
    public decimal? EOffsetFee { get; set; }
    public TimeSpan? BTime { get; set; }
    public TimeSpan? ETime { get; set; }
    public long? DayTypeId { get; set; }
    public long? PlanId { get; set; }
    public long? ClassId { get; set; }
    public decimal? Fee { get; set; }

    public AttendanceDayType? DayType { get; set; }
    public AttendancePlanTime? Plan { get; set; }
    public AttendanceSchemeClass? Class { get; set; }
}