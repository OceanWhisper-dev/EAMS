using EAMS2026.Domain.Interfaces.Repositories.Attendance;

namespace EAMS2026.Application.Common.Interfaces.Attendance;

public interface IHwattImportService
{
    Task<List<(long EmployeeId, string EmployeeName)>> GetHwattEmployeesAsync();
    Task<(TimeSpan? cardBegin, TimeSpan? cardEnd)> GetCardTimesAsync(long employeeId, DateTime date);
    Task<List<HwattCardRecordDto>> GetHwattCardRecordsAsync(DateTime? startDate = null, DateTime? endDate = null, long? employeeId = null);
}