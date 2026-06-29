namespace EAMS2026.Domain.Entities.Attendance;

public class AttendanceEventDeclared : BaseEntity
{
    public string? EventDescription { get; set; }
    public decimal? Fee { get; set; }
    public string? Memo { get; set; }
    public string? CheckMan { get; set; }
    public string? Manager { get; set; }
    public long RecordId { get; set; }
    public bool IsBeginTime { get; set; }

    public AttendanceRecord? Record { get; set; }
}