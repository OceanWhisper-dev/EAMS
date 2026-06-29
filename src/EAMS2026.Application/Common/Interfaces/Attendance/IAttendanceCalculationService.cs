namespace EAMS2026.Application.Common.Interfaces.Attendance;

public interface IAttendanceCalculationService
{
    Task<int> DoAllEmployeeAttendanceAsync(DateTime doDate, DateTime? endDate = null, long operatorId = 0);
    Task<int> ImportFromHwattDeviceAsync(long employeeId, DateTime beginDate, DateTime endDate, Func<long, DateTime, Task<(TimeSpan? cardBegin, TimeSpan? cardEnd)>> hwattCardQuery, long operatorId = 0);
    Task<long> SaveAttendanceRecordAsync(long employeeId, DateTime sDate, TimeSpan? cardBegin, TimeSpan? cardEnd, long operatorId = 0);
    Task ScheduleOneDayAsync(long employeeId, DateTime scheduleDate, long operatorId = 0, long? existingRecordId = null);
    Task<int> ResetSchedulePeriodAsync(long employeeId, DateTime sDate, int currentPeriod);
    Task ReCalculateOneDayAsync(long employeeId, DateTime attDate, long? existingRecordId = null);
}