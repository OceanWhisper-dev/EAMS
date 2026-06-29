using EAMS2026.Application.Common;
using EAMS2026.Domain.Common;
using EAMS2026.Domain.Entities.Attendance;
using EAMS2026.Domain.Interfaces.Repositories.Attendance;

namespace EAMS2026.Application.Common.Interfaces.Attendance;

public interface IAttendanceService
{
    Task<IEnumerable<AttendanceReport>> GetReportAsync(AttendanceReportSearchRequest request, long? userId = null);
    Task<IEnumerable<AttendanceEmployee>> GetEmployeesAsync(long? userId = null);
    Task<bool> UpdateEmployeeAsync(AttendanceEmployee entity, long operatorId);
    Task<bool> UpdateEmployeeMappingAsync(long id, long? systemEmployeeId, long operatorId);
    Task<bool> DeleteEmployeeAsync(long id);
    Task<PagedResult<AttendanceRecord>> GetRecordsAsync(AttendanceRecordSearchRequest request);
    Task<AttendanceRecord?> GetRecordByIdAsync(long id);
    Task<(bool Success, string Message)> CreateRecordAsync(AttendanceRecord record, long operatorId);
    Task<(bool Success, string Message)> UpdateRecordAsync(AttendanceRecord record, long operatorId);
    Task<(bool Success, string Message)> DeleteRecordAsync(long id, long operatorId);
    Task<IEnumerable<AttendanceDayType>> GetDayTypesAsync();
    Task<(bool Success, string Message)> CreateDayTypeAsync(AttendanceDayType entity, long operatorId);
    Task<(bool Success, string Message)> UpdateDayTypeAsync(AttendanceDayType entity, long operatorId);
    Task<(bool Success, string Message)> DeleteDayTypeAsync(long id, long operatorId);
    Task<IEnumerable<AttendanceSchemeClass>> GetSchemeClassesAsync();
    Task<(bool Success, string Message)> CreateSchemeClassAsync(AttendanceSchemeClass entity, long operatorId);
    Task<(bool Success, string Message)> UpdateSchemeClassAsync(AttendanceSchemeClass entity, long operatorId);
    Task<(bool Success, string Message)> DeleteSchemeClassAsync(long id, long operatorId);
    Task<IEnumerable<AttendancePlanTime>> GetPlanTimesAsync();
    Task<(bool Success, string Message)> CreatePlanTimeAsync(AttendancePlanTime entity, long operatorId);
    Task<(bool Success, string Message)> UpdatePlanTimeAsync(AttendancePlanTime entity, long operatorId);
    Task<(bool Success, string Message)> DeletePlanTimeAsync(long id, long operatorId);
    Task<IEnumerable<AttendancePlanRefClass>> GetPlanRefClassesAsync(long? planId);
    Task<(bool Success, string Message)> AddPlanRefClassAsync(AttendancePlanRefClass entity, long operatorId);
    Task<(bool Success, string Message)> DeletePlanRefClassAsync(long id, long operatorId);
    Task<IEnumerable<AttendanceEventDeclared>> GetEventsByRecordIdAsync(long recordId);
    Task<(bool Success, string Message)> CreateEventAsync(AttendanceEventDeclared entity, long operatorId);
    Task<(bool Success, string Message)> UpdateEventAsync(AttendanceEventDeclared entity, long operatorId);
    Task<(bool Success, string Message)> DeleteEventAsync(long id, long operatorId);
    Task<IEnumerable<AttendanceHoliday>> GetHolidaysAsync(int? year);
    Task<(bool Success, string Message)> CreateHolidayAsync(AttendanceHoliday entity, long operatorId);
    Task<(bool Success, string Message)> UpdateHolidayAsync(AttendanceHoliday entity, long operatorId);
    Task<(bool Success, string Message)> DeleteHolidayAsync(long id, long operatorId);
    Task<IEnumerable<AttendanceEmployeeRefSchemeClass>> GetEmployeeRefSchemeClassesAsync(long? employeeId);
    Task<(bool Success, string Message)> AddEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity, long operatorId);
    Task<(bool Success, string Message)> UpdateEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity, long operatorId);
    Task<(bool Success, string Message)> DeleteEmployeeRefSchemeClassAsync(long id, long operatorId);
    Task<IEnumerable<AttendanceFeeCalculator>> GetFeeCalculatorsAsync(long? dayTypeId);
    Task<(bool Success, string Message)> CreateFeeCalculatorAsync(AttendanceFeeCalculator entity, long operatorId);
    Task<(bool Success, string Message)> UpdateFeeCalculatorAsync(AttendanceFeeCalculator entity, long operatorId);
    Task<(bool Success, string Message)> DeleteFeeCalculatorAsync(long id, long operatorId);
    Task<AttendanceSyncEmployeesResponse> SyncEmployeesFromHwattAsync(long operatorId);
    Task<int> ImportDeviceAttendanceAsync(AttendanceImportRequest request, long operatorId);
    Task<int> ImportAllEmployeeAttendanceAsync(AttendanceAllEmployeeRequest request, long operatorId, Action<int, string>? onProgress = null);
    Task<AttendanceSyncCardRecordsResponse> SyncCardRecordsFromHwattAsync(DateTime? startDate = null, DateTime? endDate = null, long operatorId = 0, long? hwattEmployeeId = null);
    Task<DingTalkSyncEmployeesResponse> SyncEmployeesFromDingTalkAsync(long operatorId);
    Task<DingTalkSyncCardRecordsResponse> SyncCardRecordsFromDingTalkAsync(DateTime? startDate = null, DateTime? endDate = null, long operatorId = 0, string? dingtalkUserId = null);
    Task<PagedResult<CardRecordDto>> GetCardRecordsAsync(CardRecordSearchRequest request);
}