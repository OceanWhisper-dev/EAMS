namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceHoliday : BaseEntity
{
    public int IYear { get; set; }
    public DateTime SDate { get; set; }
    public string? SName { get; set; }
    public TimeSpan? BTime { get; set; }
    public TimeSpan? ETime { get; set; }
    public string? SDescription { get; set; }
}