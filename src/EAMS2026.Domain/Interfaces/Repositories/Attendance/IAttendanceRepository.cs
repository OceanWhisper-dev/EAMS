using EAMS2026.Domain.Entities.Attendance;

namespace EAMS2026.Domain.Interfaces.Repositories.Attendance;

public interface IAttendanceRepository
{
    Task<IEnumerable<AttendanceReport>> GetReportAsync(long? employeeId, DateTime startDate, DateTime endDate, long? departmentId = null);
    Task<IEnumerable<AttendanceEmployee>> GetEmployeesAsync(long? departmentId = null);
    Task<AttendanceEmployee?> GetEmployeeByIdAsync(long id);
    Task<IEnumerable<AttendanceEmployee>> GetEmployeeBySystemEmployeeIdAsync(long? systemEmployeeId);
    Task<long> AddEmployeeAsync(AttendanceEmployee entity);
    Task<bool> UpdateEmployeeAsync(AttendanceEmployee entity);
    Task<bool> DeleteEmployeeAsync(long id);
    Task<int> SaveHwattCardRecordsAsync(IEnumerable<HwattCardRecordDto> records);

    // HWATT缓存表操作方法
    Task<int> SaveHwattEmployeesAsync(IEnumerable<HwattEmployeeDto> employees);
    Task<IEnumerable<HwattEmployeeDto>> GetHwattEmployeesFromCacheAsync();
    Task<IEnumerable<HwattCardRecordDto>> GetCardRecordsFromCacheAsync(long employeeId, DateTime date);
    Task<IEnumerable<HwattCardRecordDto>> GetCardRecordsByDateRangeAsync(long employeeId, DateTime startDate, DateTime endDate);
    Task<HwattEmployeeDto?> GetHwattEmployeeByIdAsync(long employeeId);

    Task<IEnumerable<AttendanceRecord>> GetRecordsAsync(long? employeeId, DateTime? startDate, DateTime? endDate, int page, int pageSize);
    Task<int> GetRecordsCountAsync(long? employeeId, DateTime? startDate, DateTime? endDate);
    Task<AttendanceRecord?> GetRecordByIdAsync(long id);
    Task<AttendanceRecord?> GetRecordByEmployeeAndDateAsync(long employeeId, DateTime sDate);
    Task<long> AddRecordAsync(AttendanceRecord record);
    Task<long> UpsertRecordAsync(AttendanceRecord record);
    Task<bool> UpdateRecordAsync(AttendanceRecord record);
    Task<bool> DeleteRecordAsync(long id);

    Task<IEnumerable<AttendanceDayType>> GetDayTypesAsync();
    Task<AttendanceDayType?> GetDayTypeByIdAsync(long id);
    Task<long> AddDayTypeAsync(AttendanceDayType entity);
    Task<bool> UpdateDayTypeAsync(AttendanceDayType entity);
    Task<bool> DeleteDayTypeAsync(long id);

    Task<IEnumerable<AttendanceSchemeClass>> GetSchemeClassesAsync();
    Task<AttendanceSchemeClass?> GetSchemeClassByIdAsync(long id);
    Task<long> AddSchemeClassAsync(AttendanceSchemeClass entity);
    Task<bool> UpdateSchemeClassAsync(AttendanceSchemeClass entity);
    Task<bool> DeleteSchemeClassAsync(long id);

    Task<IEnumerable<AttendancePlanTime>> GetPlanTimesAsync();
    Task<AttendancePlanTime?> GetPlanTimeByIdAsync(long id);
    Task<long> AddPlanTimeAsync(AttendancePlanTime entity);
    Task<bool> UpdatePlanTimeAsync(AttendancePlanTime entity);
    Task<bool> DeletePlanTimeAsync(long id);

    Task<IEnumerable<AttendancePlanRefClass>> GetPlanRefClassesAsync(long? planId);
    Task<long> AddPlanRefClassAsync(AttendancePlanRefClass entity);
    Task<bool> DeletePlanRefClassAsync(long id);

    Task<IEnumerable<AttendanceEventDeclared>> GetEventsByRecordIdAsync(long recordId);
    Task<AttendanceEventDeclared?> GetEventByIdAsync(long id);
    Task<long> AddEventAsync(AttendanceEventDeclared entity);
    Task<bool> UpdateEventAsync(AttendanceEventDeclared entity);
    Task<bool> DeleteEventAsync(long id);
    Task<bool> UpdateEventFeeByRecordIdAsync(long recordId, bool isBeginTime, decimal fee);
    Task<bool> RecalculateRecordFeeAsync(long recordId);

    Task<IEnumerable<AttendanceHoliday>> GetHolidaysAsync(int? year);
    Task<AttendanceHoliday?> GetHolidayByIdAsync(long id);
    Task<AttendanceHoliday?> GetHolidayByDateAsync(DateTime date);
    Task<long> AddHolidayAsync(AttendanceHoliday entity);
    Task<bool> UpdateHolidayAsync(AttendanceHoliday entity);
    Task<bool> DeleteHolidayAsync(long id);

    Task<IEnumerable<AttendanceEmployeeRefSchemeClass>> GetEmployeeRefSchemeClassesAsync(long? employeeId);
    Task<AttendanceEmployeeRefSchemeClass?> GetEmployeeActiveSchemeAsync(long employeeId, DateTime date);
    Task<long> AddEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity);
    Task<bool> UpdateEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity);
    Task<bool> DeleteEmployeeRefSchemeClassAsync(long id);

    Task<IEnumerable<AttendanceFeeCalculator>> GetFeeCalculatorsAsync(long? dayTypeId);
    Task<long> AddFeeCalculatorAsync(AttendanceFeeCalculator entity);
    Task<bool> UpdateFeeCalculatorAsync(AttendanceFeeCalculator entity);
    Task<bool> DeleteFeeCalculatorAsync(long id);

    // 钉钉缓存表操作方法
    Task<int> SaveDingTalkEmployeesAsync(IEnumerable<DingTalkEmployeeDto> employees);
    Task<IEnumerable<DingTalkEmployeeDto>> GetDingTalkEmployeesFromCacheAsync();
    Task<int> SaveDingTalkCardRecordsAsync(IEnumerable<DingTalkCardRecordDto> records);
    Task<IEnumerable<DingTalkCardRecordDto>> GetDingTalkCardRecordsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<DingTalkCardRecordDto>> GetDingTalkCardRecordsByEmployeeAsync(string userId, DateTime startDate, DateTime endDate);
    Task<(IEnumerable<CardRecordDto> Items, int TotalCount)> GetCardRecordsAsync(string? employeeName, DateTime? startDate, DateTime? endDate, int page, int pageSize);

    // 合并查询 HWATT + 钉钉打卡记录（按员工+日期），用于导入考勤计算
    Task<IEnumerable<MergedCardRecordDto>> GetMergedCardRecordsAsync(long employeeId, DateTime date);
}

public class MergedCardRecordDto
{
    public DateTime CardTime { get; set; }
}

public class CardRecordDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime CardTime { get; set; }
    public string Source { get; set; } = string.Empty;
    public string CardType { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class HwattCardRecordDto
{
    public long EmployeeId { get; set; }
    public DateTime CardTime { get; set; }
    public int? CardTypeId { get; set; }
    public long? DevId { get; set; }
}

public class HwattEmployeeDto
{
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

public class DingTalkEmployeeDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int Status { get; set; } = 1;
}

public class DingTalkCardRecordDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public DateTime CheckTime { get; set; }
    public string CheckType { get; set; } = string.Empty;
    public string TimeResult { get; set; } = string.Empty;
    public DateTime? BaseCheckTime { get; set; }
}