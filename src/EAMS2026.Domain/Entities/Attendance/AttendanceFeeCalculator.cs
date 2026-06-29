namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceFeeCalculator : BaseEntity
{
    public long DayTypeId { get; set; }
    public int RangeA { get; set; }
    public int RangeB { get; set; }
    public decimal RangePrice { get; set; }

    public AttendanceDayType? DayType { get; set; }
}