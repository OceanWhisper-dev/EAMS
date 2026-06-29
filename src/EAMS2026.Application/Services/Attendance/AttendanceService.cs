using EAMS2026.Application.Common;
using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Attendance;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Common;
using EAMS2026.Domain.Entities;
using EAMS2026.Domain.Entities.Attendance;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Enums;
using EAMS2026.Domain.Interfaces.Repositories.Attendance;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.Attendance;

public class AttendanceService : IAttendanceService
{
    private const string SourceHwatt = "hwatt";
    private const string SourceDingTalk = "dingtalk";
    private const int DingTalkFallbackEmployeeIdBase = 9000;

    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly IHwattImportService _hwattImportService;
    private readonly IDingTalkService _dingTalkService;
    private readonly IAttendanceCalculationService _calcService;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDataPermissionService _dataPermissionService;
    private readonly IHwattImportSettings _hwattSettings;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IOperationLogRepository logRepository,
        IHwattImportService hwattImportService,
        IDingTalkService dingTalkService,
        IAttendanceCalculationService calcService,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IDataPermissionService dataPermissionService,
        IHwattImportSettings hwattSettings,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _logRepository = logRepository;
        _hwattImportService = hwattImportService;
        _dingTalkService = dingTalkService;
        _calcService = calcService;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _dataPermissionService = dataPermissionService;
        _hwattSettings = hwattSettings;
        _logger = logger;
    }

    public async Task<IEnumerable<AttendanceReport>> GetReportAsync(AttendanceReportSearchRequest request, long? userId = null)
    {
        if (!userId.HasValue)
            return await _attendanceRepository.GetReportAsync(request.EmployeeId, request.StartDate, request.EndDate, null);

        var (scope, userEmployeeId) = await GetEffectiveAttendanceScopeAsync(userId.Value);
        if (scope == DataScope.ALL)
            return await _attendanceRepository.GetReportAsync(request.EmployeeId, request.StartDate, request.EndDate, null);

        if (scope == DataScope.DEPARTMENT)
        {
            var employee = await _employeeRepository.GetByIdAsync(userEmployeeId ?? 0);
            return await _attendanceRepository.GetReportAsync(request.EmployeeId, request.StartDate, request.EndDate, employee?.DepartmentId);
        }

        // 普通员工 — 只查询本人
        var allAttendanceEmps = await _attendanceRepository.GetEmployeesAsync(null);
        var myAttendanceEmp = allAttendanceEmps.FirstOrDefault(e => e.SystemEmployeeId == userEmployeeId);
        return await _attendanceRepository.GetReportAsync(myAttendanceEmp?.EmployeeId, request.StartDate, request.EndDate, null);
    }

    public async Task<IEnumerable<AttendanceEmployee>> GetEmployeesAsync(long? userId = null)
    {
        if (!userId.HasValue)
            return await _attendanceRepository.GetEmployeesAsync(null);

        var (scope, userEmployeeId) = await GetEffectiveAttendanceScopeAsync(userId.Value);
        if (scope == DataScope.ALL)
            return await _attendanceRepository.GetEmployeesAsync(null);

        if (scope == DataScope.DEPARTMENT)
        {
            var employee = await _employeeRepository.GetByIdAsync(userEmployeeId ?? 0);
            return await _attendanceRepository.GetEmployeesAsync(employee?.DepartmentId);
        }

        // 普通员工 — 只返回本人
        var allEmps = await _attendanceRepository.GetEmployeesAsync(null);
        return allEmps.Where(e => e.SystemEmployeeId == userEmployeeId);
    }

    /// <summary>获取当前用户对考勤模块的有效数据权限范围</summary>
    private async Task<(DataScope? Scope, long? UserEmployeeId)> GetEffectiveAttendanceScopeAsync(long userId)
    {
        var user = await _userRepository.GetWithRolesAsync(userId);
        if (user == null) return (null, null);

        DataScope? effectiveScope = null;
        foreach (var role in user.Roles)
        {
            var scope = await _dataPermissionService.GetDataScopeAsync(role.Id, "attendance");
            if (scope == DataScope.ALL) { effectiveScope = DataScope.ALL; break; }
            if (scope == DataScope.DEPARTMENT && effectiveScope == null) effectiveScope = DataScope.DEPARTMENT;
        }

        return (effectiveScope, user.EmployeeId);
    }

    public async Task<bool> UpdateEmployeeAsync(AttendanceEmployee entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        return await _attendanceRepository.UpdateEmployeeAsync(entity);
    }

    public async Task<bool> UpdateEmployeeMappingAsync(long id, long? systemEmployeeId, long operatorId)
    {
        var entity = await _attendanceRepository.GetEmployeeByIdAsync(id);
        if (entity == null) return false;
        entity.SystemEmployeeId = systemEmployeeId;
        entity.UpdatedBy = operatorId;
        return await _attendanceRepository.UpdateEmployeeAsync(entity);
    }

    public async Task<bool> DeleteEmployeeAsync(long id)
    {
        return await _attendanceRepository.DeleteEmployeeAsync(id);
    }

    public async Task<PagedResult<AttendanceRecord>> GetRecordsAsync(AttendanceRecordSearchRequest request)
    {
        var items = await _attendanceRepository.GetRecordsAsync(request.EmployeeId, request.StartDate, request.EndDate, request.Page, request.PageSize);
        var total = await _attendanceRepository.GetRecordsCountAsync(request.EmployeeId, request.StartDate, request.EndDate);
        return new PagedResult<AttendanceRecord>
        {
            Items = items,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<AttendanceRecord?> GetRecordByIdAsync(long id)
    {
        return await _attendanceRepository.GetRecordByIdAsync(id);
    }

    public async Task<(bool Success, string Message)> CreateRecordAsync(AttendanceRecord record, long operatorId)
    {
        record.CreatedBy = operatorId;
        record.UpdatedBy = operatorId;
        await _attendanceRepository.AddRecordAsync(record);
        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId, OperationType = "Create", Module = "Attendance",
            EntityType = "AttendanceRecord", EntityId = record.Id,
            Description = $"创建考勤记录: 员工{record.EmployeeId} 日期{record.SDate:yyyy-MM-dd}"
        });
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateRecordAsync(AttendanceRecord record, long operatorId)
    {
        record.UpdatedBy = operatorId;
        await _attendanceRepository.UpdateRecordAsync(record);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteRecordAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeleteRecordAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendanceDayType>> GetDayTypesAsync()
    {
        return await _attendanceRepository.GetDayTypesAsync();
    }

    public async Task<(bool Success, string Message)> CreateDayTypeAsync(AttendanceDayType entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddDayTypeAsync(entity);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateDayTypeAsync(AttendanceDayType entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        await _attendanceRepository.UpdateDayTypeAsync(entity);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteDayTypeAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeleteDayTypeAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendanceSchemeClass>> GetSchemeClassesAsync()
    {
        return await _attendanceRepository.GetSchemeClassesAsync();
    }

    public async Task<(bool Success, string Message)> CreateSchemeClassAsync(AttendanceSchemeClass entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddSchemeClassAsync(entity);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateSchemeClassAsync(AttendanceSchemeClass entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        await _attendanceRepository.UpdateSchemeClassAsync(entity);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteSchemeClassAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeleteSchemeClassAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendancePlanTime>> GetPlanTimesAsync()
    {
        return await _attendanceRepository.GetPlanTimesAsync();
    }

    public async Task<(bool Success, string Message)> CreatePlanTimeAsync(AttendancePlanTime entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddPlanTimeAsync(entity);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdatePlanTimeAsync(AttendancePlanTime entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        await _attendanceRepository.UpdatePlanTimeAsync(entity);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeletePlanTimeAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeletePlanTimeAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendancePlanRefClass>> GetPlanRefClassesAsync(long? planId)
    {
        return await _attendanceRepository.GetPlanRefClassesAsync(planId);
    }

    public async Task<(bool Success, string Message)> AddPlanRefClassAsync(AttendancePlanRefClass entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddPlanRefClassAsync(entity);
        return (true, "添加成功");
    }

    public async Task<(bool Success, string Message)> DeletePlanRefClassAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeletePlanRefClassAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendanceEventDeclared>> GetEventsByRecordIdAsync(long recordId)
    {
        return await _attendanceRepository.GetEventsByRecordIdAsync(recordId);
    }

    public async Task<(bool Success, string Message)> CreateEventAsync(AttendanceEventDeclared entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddEventAsync(entity);
        await _attendanceRepository.RecalculateRecordFeeAsync(entity.RecordId);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateEventAsync(AttendanceEventDeclared entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        await _attendanceRepository.UpdateEventAsync(entity);
        await _attendanceRepository.RecalculateRecordFeeAsync(entity.RecordId);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteEventAsync(long id, long operatorId)
    {
        var eventEntity = await _attendanceRepository.GetEventByIdAsync(id);
        if (eventEntity == null)
            return (false, "事件不存在");
        await _attendanceRepository.DeleteEventAsync(id);
        await _attendanceRepository.RecalculateRecordFeeAsync(eventEntity.RecordId);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendanceHoliday>> GetHolidaysAsync(int? year)
    {
        return await _attendanceRepository.GetHolidaysAsync(year);
    }

    public async Task<(bool Success, string Message)> CreateHolidayAsync(AttendanceHoliday entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddHolidayAsync(entity);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateHolidayAsync(AttendanceHoliday entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        await _attendanceRepository.UpdateHolidayAsync(entity);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteHolidayAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeleteHolidayAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendanceEmployeeRefSchemeClass>> GetEmployeeRefSchemeClassesAsync(long? employeeId)
    {
        return await _attendanceRepository.GetEmployeeRefSchemeClassesAsync(employeeId);
    }

    public async Task<(bool Success, string Message)> AddEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddEmployeeRefSchemeClassAsync(entity);
        return (true, "添加成功");
    }

    public async Task<(bool Success, string Message)> UpdateEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        var updated = await _attendanceRepository.UpdateEmployeeRefSchemeClassAsync(entity);
        return updated ? (true, "更新成功") : (false, "更新失败，记录不存在");
    }

    public async Task<(bool Success, string Message)> DeleteEmployeeRefSchemeClassAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeleteEmployeeRefSchemeClassAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<AttendanceFeeCalculator>> GetFeeCalculatorsAsync(long? dayTypeId)
    {
        return await _attendanceRepository.GetFeeCalculatorsAsync(dayTypeId);
    }

    public async Task<(bool Success, string Message)> CreateFeeCalculatorAsync(AttendanceFeeCalculator entity, long operatorId)
    {
        entity.CreatedBy = operatorId; entity.UpdatedBy = operatorId;
        await _attendanceRepository.AddFeeCalculatorAsync(entity);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateFeeCalculatorAsync(AttendanceFeeCalculator entity, long operatorId)
    {
        entity.UpdatedBy = operatorId;
        await _attendanceRepository.UpdateFeeCalculatorAsync(entity);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteFeeCalculatorAsync(long id, long operatorId)
    {
        await _attendanceRepository.DeleteFeeCalculatorAsync(id);
        return (true, "删除成功");
    }

    public async Task<AttendanceSyncEmployeesResponse> SyncEmployeesFromHwattAsync(long operatorId)
    {
        // 1. 从HWATT SQL Server获取员工列表（仅BrchID=3）
        var hwattEmployees = await _hwattImportService.GetHwattEmployeesAsync();

        // 2. 保存到 hwatt_employees 缓存表
        var cacheDtos = hwattEmployees.Select(e => new HwattEmployeeDto
        {
            EmployeeId = e.EmployeeId,
            EmployeeName = e.EmployeeName,
            Status = 1
        });
        await _attendanceRepository.SaveHwattEmployeesAsync(cacheDtos);

        // 3. 从缓存表合并到 attendance_employees（以员工管理为基准，只同步匹配的员工）
        var cachedEmployees = await _attendanceRepository.GetHwattEmployeesFromCacheAsync();
        var existingEmployees = await _attendanceRepository.GetEmployeesAsync();
        var systemEmployees = await _employeeRepository.GetAllAsync();
        var systemEmployeesByName = systemEmployees
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .GroupBy(e => e.Name)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().Id);
        int synced = 0;
        int updated = 0;
        int skipped = 0;

        // 去重：清理 source='hwatt' 中 system_employee_id 重复的记录
        var hwattDupGroups = existingEmployees
            .Where(e => e.Source == SourceHwatt && e.SystemEmployeeId != null)
            .GroupBy(e => e.SystemEmployeeId)
            .Where(g => g.Count() > 1);
        foreach (var group in hwattDupGroups)
        {
            var ordered = group.OrderByDescending(e => e.HwattEmployeeId != null).ThenByDescending(e => e.Id).ToList();
            foreach (var dup in ordered.Skip(1))
            {
                await _attendanceRepository.DeleteEmployeeAsync(dup.Id);
            }
        }

        // 重新获取最新列表（去重后）
        existingEmployees = await _attendanceRepository.GetEmployeesAsync();

        foreach (var cached in cachedEmployees)
        {
            // 以员工管理里的员工为准，跳过未匹配到系统员工表的记录
            if (!systemEmployeesByName.TryGetValue(cached.EmployeeName, out var sysEmpId))
            {
                skipped++;
                continue;
            }

            // 按 hwatt_employee_id 优先查找，其次按 system_employee_id 查找
            var existing = existingEmployees.FirstOrDefault(e =>
                e.Source == SourceHwatt && e.HwattEmployeeId == cached.EmployeeId)
                ?? existingEmployees.FirstOrDefault(e =>
                    e.Source == SourceHwatt && e.SystemEmployeeId == sysEmpId);

            if (existing == null)
            {
                var newEmp = new AttendanceEmployee
                {
                    EmployeeId = cached.EmployeeId,
                    EmployeeName = cached.EmployeeName,
                    Source = SourceHwatt,
                    HwattEmployeeId = cached.EmployeeId,
                    SystemEmployeeId = sysEmpId,
                    CreatedBy = operatorId,
                    UpdatedBy = operatorId
                };
                await _attendanceRepository.AddEmployeeAsync(newEmp);
                synced++;
            }
            else
            {
                existing.EmployeeName = cached.EmployeeName;
                existing.HwattEmployeeId = cached.EmployeeId;
                existing.SystemEmployeeId = sysEmpId;
                existing.UpdatedBy = operatorId;
                await _attendanceRepository.UpdateEmployeeAsync(existing);
                updated++;
            }
        }

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId, OperationType = "Sync", Module = "Attendance",
            EntityType = "AttendanceEmployee",
            Description = $"从HWATT同步员工(BrchID={_hwattSettings.BrchId}): 同步{cacheDtos.Count()}人到缓存表, 新增{synced}名, 更新{updated}名, 跳过{skipped}名(未匹配系统员工)"
        });

        return new AttendanceSyncEmployeesResponse { SyncedCount = synced, UpdatedCount = updated, TotalCount = cachedEmployees.Count() };
    }

    public async Task<int> ImportDeviceAttendanceAsync(AttendanceImportRequest request, long operatorId)
    {
        if (!request.EmployeeId.HasValue)
            throw new ArgumentException("必须指定员工ID");

        // 根据数据权限范围决定导入权限
        var (scope, userEmployeeId) = await GetEffectiveAttendanceScopeAsync(operatorId);
        if (scope != DataScope.ALL && scope != DataScope.DEPARTMENT)
        {
            // 普通员工只能导入自己的考勤数据 — 需将系统 EmployeeId 转为考勤 EmployeeId
            if (!userEmployeeId.HasValue)
                throw new UnauthorizedAccessException("当前用户未关联员工，无法导入考勤");
            var myAttendanceEmps = await _attendanceRepository.GetEmployeeBySystemEmployeeIdAsync(userEmployeeId.Value);
            var myAttendanceEmp = myAttendanceEmps.FirstOrDefault();
            if (myAttendanceEmp == null)
                throw new UnauthorizedAccessException("未找到当前用户的考勤员工信息");
            request.EmployeeId = myAttendanceEmp.EmployeeId;
        }

        // 查找目标员工的考勤信息，获取系统员工ID
        var attendanceEmployees = await _attendanceRepository.GetEmployeesAsync();
        var attEmp = attendanceEmployees.FirstOrDefault(e => e.EmployeeId == request.EmployeeId.Value && !e.IsDeleted);

        // 自动从各源同步该员工的打卡记录（员工可能在 HWATT 和钉钉两个源都有打卡）
        if (attEmp != null)
        {
            if (attEmp.SystemEmployeeId != null)
            {
                // 查找该系统员工关联的所有考勤员工记录
                var matchingEmps = attendanceEmployees
                    .Where(e => e.SystemEmployeeId == attEmp.SystemEmployeeId && !e.IsDeleted)
                    .ToList();

                // 1. 同步 HWATT 打卡记录
                var hwattEmp = matchingEmps.FirstOrDefault(e => e.Source == SourceHwatt && e.HwattEmployeeId.HasValue);
                if (hwattEmp != null)
                {
                    try { await SyncCardRecordsFromHwattAsync(request.BeginDate, request.EndDate, operatorId, hwattEmp.HwattEmployeeId!.Value); }
                    catch (Exception ex) { _logger.LogWarning(ex, "自动同步HWATT打卡记录失败，继续执行导入"); }
                }

                // 2. 同步钉钉打卡记录 — 从缓存表按员工名匹配钉钉 UserId
                var dingtalkCached = await _attendanceRepository.GetDingTalkEmployeesFromCacheAsync();
                var matchedDingtalk = dingtalkCached.FirstOrDefault(d => d.UserName == attEmp.EmployeeName);
                if (matchedDingtalk != null)
                {
                    try { await SyncCardRecordsFromDingTalkAsync(request.BeginDate, request.EndDate, operatorId, matchedDingtalk.UserId); }
                    catch (Exception ex) { _logger.LogWarning(ex, "自动同步钉钉打卡记录失败，继续执行导入"); }
                }
            }
            else
            {
                // 无系统员工关联时，按员工源类型分别同步
                if (attEmp.Source == SourceHwatt && attEmp.HwattEmployeeId.HasValue)
                {
                    try { await SyncCardRecordsFromHwattAsync(request.BeginDate, request.EndDate, operatorId, attEmp.HwattEmployeeId!.Value); }
                    catch (Exception ex) { _logger.LogWarning(ex, "自动同步HWATT打卡记录失败，继续执行导入"); }
                }
                else
                {
                    try { await SyncCardRecordsFromDingTalkAsync(request.BeginDate, request.EndDate, operatorId, attEmp.HwattEmployeeId?.ToString()); }
                    catch (Exception ex) { _logger.LogWarning(ex, "自动同步钉钉打卡记录失败，继续执行导入"); }
                }
            }
        }

        // 检查员工是否已离职
        DateOnly? resignationDate = null;
        if (attEmp?.SystemEmployeeId != null)
        {
            var sysEmp = await _employeeRepository.GetByIdAsync(attEmp.SystemEmployeeId.Value);
            if (sysEmp?.ResignationDate != null)
                resignationDate = sysEmp.ResignationDate.Value;
        }

        int count = 0;
        for (var date = request.BeginDate; date <= request.EndDate; date = date.AddDays(1))
        {
            // 跳过离职日期之后的记录
            if (resignationDate.HasValue && DateOnly.FromDateTime(date) >= resignationDate.Value)
                continue;

            var records = await _attendanceRepository.GetMergedCardRecordsAsync(request.EmployeeId.Value, date);
            TimeSpan? cardBegin = null;
            TimeSpan? cardEnd = null;
            if (records.Any())
            {
                cardBegin = records.Min(r => r.CardTime.TimeOfDay);
                cardEnd = records.Max(r => r.CardTime.TimeOfDay);
            }

            var recordId = await _calcService.SaveAttendanceRecordAsync(request.EmployeeId.Value, date, cardBegin, cardEnd, operatorId);
            await _calcService.ScheduleOneDayAsync(request.EmployeeId.Value, date, operatorId, recordId);
            await _calcService.ReCalculateOneDayAsync(request.EmployeeId.Value, date, recordId);
            count++;
        }

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId, OperationType = "Import", Module = "Attendance",
            EntityType = "AttendanceRecord",
            Description = $"导入考勤机数据: 员工{request.EmployeeId.Value}, {request.BeginDate:yyyy-MM-dd}~{request.EndDate:yyyy-MM-dd}, 共{count}天"
        });

        return count;
    }

    public async Task<int> ImportAllEmployeeAttendanceAsync(AttendanceAllEmployeeRequest request, long operatorId, Action<int, string>? onProgress = null)
    {
        // 自动从源数据同步打卡记录
        var startDate = request.DoDate;
        var endDate = request.EndDate ?? new DateTime(request.DoDate.Year, request.DoDate.Month, DateTime.DaysInMonth(request.DoDate.Year, request.DoDate.Month));
        try { await SyncCardRecordsFromHwattAsync(startDate, endDate, operatorId); }
        catch (Exception ex) { _logger.LogWarning(ex, "自动同步HWATT打卡记录失败，继续执行导入"); }
        try { await SyncCardRecordsFromDingTalkAsync(startDate, endDate, operatorId); }
        catch (Exception ex) { _logger.LogWarning(ex, "自动同步钉钉打卡记录失败，继续执行导入"); }

        var employees = await _attendanceRepository.GetEmployeeRefSchemeClassesAsync(null);
        var employeeIds = employees.Select(e => e.EmployeeId).Distinct().ToList();

        // 构建员工离职日期映射
        var allAttendanceEmps = await _attendanceRepository.GetEmployeesAsync();
        var systemEmployees = await _employeeRepository.GetAllAsync();
        var resignedEmpIds = new HashSet<long>();
        var systemResignDates = systemEmployees
            .Where(e => e.ResignationDate != null)
            .ToDictionary(e => e.Id, e => e.ResignationDate!.Value);

        foreach (var attEmp in allAttendanceEmps)
        {
            if (attEmp.SystemEmployeeId != null && systemResignDates.TryGetValue(attEmp.SystemEmployeeId.Value, out var resignDate))
            {
                // 记录已离职考勤员工的 employee_id
                resignedEmpIds.Add(attEmp.EmployeeId);
            }
        }

        int totalCount = 0;
        int skippedCount = 0;
        int totalEmployees = employeeIds.Count;
        int processedEmployees = 0;
        int lastReportedProgress = 0;

        foreach (var empId in employeeIds)
        {
            // 检查该员工是否有离职日期
            var attEmp = allAttendanceEmps.FirstOrDefault(e => e.EmployeeId == empId && !e.IsDeleted);
            DateOnly? empResignDate = null;
            if (attEmp?.SystemEmployeeId != null && systemResignDates.TryGetValue(attEmp.SystemEmployeeId.Value, out var rd))
                empResignDate = rd;

            for (var date = request.DoDate; date <= endDate; date = date.AddDays(1))
            {
                // 跳过离职日期之后的记录
                if (empResignDate.HasValue && DateOnly.FromDateTime(date) >= empResignDate.Value)
                {
                    skippedCount++;
                    continue;
                }

                var records = await _attendanceRepository.GetMergedCardRecordsAsync(empId, date);
                TimeSpan? cardBegin = null;
                TimeSpan? cardEnd = null;
                if (records.Any())
                {
                    cardBegin = records.Min(r => r.CardTime.TimeOfDay);
                    cardEnd = records.Max(r => r.CardTime.TimeOfDay);
                }

                var recordId = await _calcService.SaveAttendanceRecordAsync(empId, date, cardBegin, cardEnd, operatorId);
                await _calcService.ScheduleOneDayAsync(empId, date, operatorId, recordId);
                await _calcService.ReCalculateOneDayAsync(empId, date, recordId);
                totalCount++;
            }

            processedEmployees++;
            if (onProgress != null)
            {
                var empName = attEmp?.EmployeeName ?? $"员工{empId}";
                int progress = (processedEmployees * 100) / totalEmployees;
                int currentThreshold = (progress / 10) * 10;
                if (currentThreshold > lastReportedProgress)
                {
                    lastReportedProgress = currentThreshold;
                    onProgress(currentThreshold, $"正在处理{processedEmployees}/{totalEmployees} - {empName}");
                }
            }
        }

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId, OperationType = "ImportAll", Module = "Attendance",
            EntityType = "AttendanceRecord",
            Description = $"导入所有员工考勤: {request.DoDate:yyyy-MM-dd}~{endDate:yyyy-MM-dd}, {employeeIds.Count}人, 共{totalCount}天次, 跳过{skippedCount}天(已离职)"
        });

        return totalCount;
    }

    public async Task<AttendanceSyncCardRecordsResponse> SyncCardRecordsFromHwattAsync(DateTime? startDate = null, DateTime? endDate = null, long operatorId = 0, long? hwattEmployeeId = null)
    {
        int totalSynced = 0;
        DateTime currentDate = startDate ?? DateTime.Parse(_hwattSettings.DefaultSyncStartDate);
        DateTime endSyncDate = endDate ?? DateTime.Now;

        while (currentDate <= endSyncDate)
        {
            DateTime batchEndDate = currentDate.AddDays(_hwattSettings.BatchDays);
            if (batchEndDate > endSyncDate)
                batchEndDate = endSyncDate;

            var records = await _hwattImportService.GetHwattCardRecordsAsync(currentDate, batchEndDate, hwattEmployeeId);
            
            if (records.Any())
            {
                int saved = await _attendanceRepository.SaveHwattCardRecordsAsync(records);
                totalSynced += saved;
            }

            currentDate = batchEndDate.AddDays(1);
        }

        if (operatorId > 0)
        {
            var desc = hwattEmployeeId.HasValue
                ? $"从HWATT同步打卡记录(员工{hwattEmployeeId}): {startDate?.ToString("yyyy-MM-dd") ?? "开始"}~{endDate?.ToString("yyyy-MM-dd") ?? "现在"}, 共{totalSynced}条"
                : $"从HWATT同步打卡记录: {startDate?.ToString("yyyy-MM-dd") ?? "开始"}~{endDate?.ToString("yyyy-MM-dd") ?? "现在"}, 共{totalSynced}条";
            await _logRepository.AddAsync(new OperationLog
            {
                UserId = operatorId, OperationType = "Sync", Module = "Attendance",
                EntityType = "HwattCardRecord",
                Description = desc
            });
        }

        return new AttendanceSyncCardRecordsResponse { SyncedCount = totalSynced };
    }

    // ========== 钉钉数据同步 ==========

    public async Task<DingTalkSyncEmployeesResponse> SyncEmployeesFromDingTalkAsync(long operatorId)
    {
        var dingTalkUsers = await _dingTalkService.GetAttendanceGroupUsersAsync();

        var cacheDtos = dingTalkUsers.Select(u => new DingTalkEmployeeDto
        {
            UserId = u.UserId,
            UserName = u.Name,
            DepartmentId = u.DepartmentId,
            DepartmentName = "",
            Status = 1
        });
        await _attendanceRepository.SaveDingTalkEmployeesAsync(cacheDtos);

        var cachedEmployees = await _attendanceRepository.GetDingTalkEmployeesFromCacheAsync();
        var existingEmployees = await _attendanceRepository.GetEmployeesAsync();
        var systemEmployees = await _employeeRepository.GetAllAsync();
        var systemEmployeesByName = systemEmployees
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .GroupBy(e => e.Name)
            .Where(g => g.Count() == 1)
            .ToDictionary(g => g.Key, g => g.First().Id);
        int synced = 0;

        foreach (var cached in cachedEmployees)
        {
            long.TryParse(cached.UserId, out long empId);
            var existing = existingEmployees.FirstOrDefault(e =>
                e.Source == SourceDingTalk && e.HwattEmployeeId.HasValue && e.HwattEmployeeId.Value == empId);

            long? sysEmpId = null;
            if (systemEmployeesByName.TryGetValue(cached.UserName, out var matchedId))
                sysEmpId = matchedId;

            if (existing == null)
            {
                var newEmp = new AttendanceEmployee
                {
                    EmployeeId = empId > 0 ? empId : DingTalkFallbackEmployeeIdBase + synced,
                    EmployeeName = cached.UserName,
                    Source = SourceDingTalk,
                    HwattEmployeeId = empId > 0 ? empId : null,
                    SystemEmployeeId = sysEmpId,
                    CreatedBy = operatorId,
                    UpdatedBy = operatorId
                };
                await _attendanceRepository.AddEmployeeAsync(newEmp);
                synced++;
            }
            else
            {
                existing.SystemEmployeeId ??= sysEmpId;
                existing.UpdatedBy = operatorId;
                await _attendanceRepository.UpdateEmployeeAsync(existing);
            }
        }

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId, OperationType = "Sync", Module = "Attendance",
            EntityType = "AttendanceEmployee",
            Description = $"从钉钉同步员工: 同步{cacheDtos.Count()}人到缓存表, 新增{synced}名考勤员工"
        });

        return new DingTalkSyncEmployeesResponse { SyncedCount = synced, TotalCount = cachedEmployees.Count() };
    }

    public async Task<DingTalkSyncCardRecordsResponse> SyncCardRecordsFromDingTalkAsync(DateTime? startDate = null, DateTime? endDate = null, long operatorId = 0, string? dingtalkUserId = null)
    {
        var cachedEmployees = await _attendanceRepository.GetDingTalkEmployeesFromCacheAsync();
        List<string> userIdList;
        if (!string.IsNullOrEmpty(dingtalkUserId))
        {
            userIdList = new List<string> { dingtalkUserId };
        }
        else
        {
            userIdList = cachedEmployees.Select(e => e.UserId).ToList();
        }

        if (!userIdList.Any())
            throw new InvalidOperationException("请先同步钉钉员工信息");

        var start = startDate ?? DateTime.Now.AddMonths(-1);
        var end = endDate ?? DateTime.Now;

        var records = await _dingTalkService.GetAttendanceRecordsAsync(start, end, userIdList);

        // 从缓存中查找钉钉用户姓名，填充到打卡记录中
        var userNameLookup = cachedEmployees
            .DistinctBy(e => e.UserId)
            .ToDictionary(e => e.UserId, e => e.UserName);

        var recordDtos = records.Select(r => new DingTalkCardRecordDto
        {
            UserId = r.UserId,
            UserName = userNameLookup.TryGetValue(r.UserId, out var name) ? name : r.UserName,
            WorkDate = r.WorkDate,
            CheckTime = r.UserCheckTime,
            CheckType = r.CheckType,
            TimeResult = r.TimeResult,
            BaseCheckTime = r.BaseCheckTime
        }).ToList();

        int saved = 0;
        if (recordDtos.Any())
        {
            saved = await _attendanceRepository.SaveDingTalkCardRecordsAsync(recordDtos);
        }

        if (operatorId > 0)
        {
            var desc = !string.IsNullOrEmpty(dingtalkUserId)
                ? $"从钉钉同步打卡记录(用户{dingtalkUserId}): {start:yyyy-MM-dd}~{end:yyyy-MM-dd}, 共{saved}条"
                : $"从钉钉同步打卡记录: {start:yyyy-MM-dd}~{end:yyyy-MM-dd}, 共{saved}条";
            await _logRepository.AddAsync(new OperationLog
            {
                UserId = operatorId, OperationType = "Sync", Module = "Attendance",
                EntityType = "DingTalkCardRecord",
                Description = desc
            });
        }

        return new DingTalkSyncCardRecordsResponse { SyncedCount = saved };
    }

    public async Task<PagedResult<CardRecordDto>> GetCardRecordsAsync(CardRecordSearchRequest request)
    {
        var (items, total) = await _attendanceRepository.GetCardRecordsAsync(
            request.EmployeeName, request.StartDate, request.EndDate, request.Page, request.PageSize);
        return new PagedResult<CardRecordDto>
        {
            Items = items,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}