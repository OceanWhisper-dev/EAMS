using EAMS2026.Domain.Interfaces.Repositories.Attendance;

namespace EAMS2026.Application.Common.Interfaces.Attendance;

public interface IDingTalkService
{
    Task<string> GetTokenAsync();
    Task<List<DingTalkDepartmentDto>> GetDepartmentsAsync();
    Task<List<DingTalkUserDto>> GetDepartmentUsersAsync(long departmentId);
    Task<List<DingTalkAttendanceRecordDto>> GetAttendanceRecordsAsync(DateTime startDate, DateTime endDate, List<string> userIdList);
    Task<List<DingTalkUserDto>> GetAttendanceGroupUsersAsync();
    DateTime ConvertDingTalkTime(string dingTalkTimestamp);
}

public class DingTalkDepartmentDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DingTalkUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
}

public class DingTalkAttendanceRecordDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime UserCheckTime { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public string TimeResult { get; set; } = string.Empty;
    public DateTime? BaseCheckTime { get; set; }
}