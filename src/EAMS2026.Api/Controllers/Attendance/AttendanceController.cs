using EAMS2026.Application.Common;
using EAMS2026.Application.Common.Interfaces.Attendance;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services.Attendance;
using EAMS2026.Domain.Entities.Attendance;
using EAMS2026.Api.Hubs;
using EAMS2026.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Api.Controllers.Attendance;

[Authorize(Policy = "attendance")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/attendance")]
public class AttendanceController : BaseController
{
    private readonly IAttendanceService _attendanceService;
    private readonly IImportTaskService _importTaskService;
    private readonly IHubContext<ImportHub> _hubContext;
    private readonly ILogger<AttendanceController> _logger;
    private readonly BackgroundTaskService _backgroundTaskService;

    public AttendanceController(
        IAttendanceService attendanceService,
        IImportTaskService importTaskService,
        IHubContext<ImportHub> hubContext,
        ILogger<AttendanceController> logger,
        BackgroundTaskService backgroundTaskService)
    {
        _attendanceService = attendanceService;
        _importTaskService = importTaskService;
        _hubContext = hubContext;
        _logger = logger;
        _backgroundTaskService = backgroundTaskService;
    }

    [HttpPost("report/search")]
    public async Task<IActionResult> SearchReport([FromBody] AttendanceReportSearchRequest request)
    {
        var data = await _attendanceService.GetReportAsync(request, GetUserId());
        return Success(data);
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var data = await _attendanceService.GetEmployeesAsync(GetUserId());
        return Success(data);
    }

    [HttpPut("employees/{id}")]
    public async Task<IActionResult> UpdateEmployee(long id, [FromBody] AttendanceEmployee entity)
    {
        entity.Id = id;
        var ok = await _attendanceService.UpdateEmployeeAsync(entity, GetUserId());
        return ok ? Success("更新成功") : Fail("更新失败");
    }

    [HttpPut("employees/{id}/mapping")]
    public async Task<IActionResult> UpdateEmployeeMapping(long id, [FromBody] UpdateEmployeeMappingRequest request)
    {
        var ok = await _attendanceService.UpdateEmployeeMappingAsync(id, request.SystemEmployeeId, GetUserId());
        return ok ? Success("更新映射成功") : Fail("映射更新失败");
    }

    [HttpDelete("employees/{id}")]
    public async Task<IActionResult> DeleteEmployee(long id)
    {
        var ok = await _attendanceService.DeleteEmployeeAsync(id);
        return ok ? Success("删除成功") : Fail("删除失败");
    }

    [HttpGet("employees/export")]
    public async Task<IActionResult> ExportEmployees()
    {
        var data = await _attendanceService.GetEmployeesAsync();
        return Success(data);
    }

    [HttpPost("records/search")]
    public async Task<IActionResult> SearchRecords([FromBody] AttendanceRecordSearchRequest request)
    {
        var data = await _attendanceService.GetRecordsAsync(request);
        return Success(data);
    }

    [HttpGet("records/{id}")]
    public async Task<IActionResult> GetRecord(long id)
    {
        var data = await _attendanceService.GetRecordByIdAsync(id);
        if (data == null) return NotFound("考勤记录不存在");
        return Success(data);
    }

    [HttpPost("records")]
    public async Task<IActionResult> CreateRecord([FromBody] AttendanceRecord record)
    {
        return await ExecuteAsync(() => _attendanceService.CreateRecordAsync(record, GetUserId()));
    }

    [HttpPut("records/{id}")]
    public async Task<IActionResult> UpdateRecord(long id, [FromBody] AttendanceRecord record)
    {
        record.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdateRecordAsync(record, GetUserId()));
    }

    [HttpDelete("records/{id}")]
    public async Task<IActionResult> DeleteRecord(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeleteRecordAsync(id, GetUserId()));
    }

    [HttpGet("day-types")]
    public async Task<IActionResult> GetDayTypes()
    {
        var data = await _attendanceService.GetDayTypesAsync();
        return Success(data);
    }

    [HttpPost("day-types")]
    public async Task<IActionResult> CreateDayType([FromBody] AttendanceDayType entity)
    {
        return await ExecuteAsync(() => _attendanceService.CreateDayTypeAsync(entity, GetUserId()));
    }

    [HttpPut("day-types/{id}")]
    public async Task<IActionResult> UpdateDayType(long id, [FromBody] AttendanceDayType entity)
    {
        entity.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdateDayTypeAsync(entity, GetUserId()));
    }

    [HttpDelete("day-types/{id}")]
    public async Task<IActionResult> DeleteDayType(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeleteDayTypeAsync(id, GetUserId()));
    }

    [HttpGet("scheme-classes")]
    public async Task<IActionResult> GetSchemeClasses()
    {
        var data = await _attendanceService.GetSchemeClassesAsync();
        return Success(data);
    }

    [HttpPost("scheme-classes")]
    public async Task<IActionResult> CreateSchemeClass([FromBody] AttendanceSchemeClass entity)
    {
        return await ExecuteAsync(() => _attendanceService.CreateSchemeClassAsync(entity, GetUserId()));
    }

    [HttpPut("scheme-classes/{id}")]
    public async Task<IActionResult> UpdateSchemeClass(long id, [FromBody] AttendanceSchemeClass entity)
    {
        entity.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdateSchemeClassAsync(entity, GetUserId()));
    }

    [HttpDelete("scheme-classes/{id}")]
    public async Task<IActionResult> DeleteSchemeClass(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeleteSchemeClassAsync(id, GetUserId()));
    }

    [HttpGet("plan-times")]
    public async Task<IActionResult> GetPlanTimes()
    {
        var data = await _attendanceService.GetPlanTimesAsync();
        return Success(data);
    }

    [HttpPost("plan-times")]
    public async Task<IActionResult> CreatePlanTime([FromBody] AttendancePlanTime entity)
    {
        return await ExecuteAsync(() => _attendanceService.CreatePlanTimeAsync(entity, GetUserId()));
    }

    [HttpPut("plan-times/{id}")]
    public async Task<IActionResult> UpdatePlanTime(long id, [FromBody] AttendancePlanTime entity)
    {
        entity.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdatePlanTimeAsync(entity, GetUserId()));
    }

    [HttpDelete("plan-times/{id}")]
    public async Task<IActionResult> DeletePlanTime(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeletePlanTimeAsync(id, GetUserId()));
    }

    [HttpGet("plan-times/{planId}/ref-classes")]
    public async Task<IActionResult> GetPlanRefClasses(long planId)
    {
        var data = await _attendanceService.GetPlanRefClassesAsync(planId);
        return Success(data);
    }

    [HttpPost("plan-ref-classes")]
    public async Task<IActionResult> AddPlanRefClass([FromBody] AttendancePlanRefClass entity)
    {
        return await ExecuteAsync(() => _attendanceService.AddPlanRefClassAsync(entity, GetUserId()));
    }

    [HttpDelete("plan-ref-classes/{id}")]
    public async Task<IActionResult> DeletePlanRefClass(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeletePlanRefClassAsync(id, GetUserId()));
    }

    [HttpGet("records/{recordId}/events")]
    public async Task<IActionResult> GetEvents(long recordId)
    {
        var data = await _attendanceService.GetEventsByRecordIdAsync(recordId);
        return Success(data);
    }

    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] AttendanceEventDeclared entity)
    {
        return await ExecuteAsync(() => _attendanceService.CreateEventAsync(entity, GetUserId()));
    }

    [HttpPut("events/{id}")]
    public async Task<IActionResult> UpdateEvent(long id, [FromBody] AttendanceEventDeclared entity)
    {
        entity.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdateEventAsync(entity, GetUserId()));
    }

    [HttpDelete("events/{id}")]
    public async Task<IActionResult> DeleteEvent(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeleteEventAsync(id, GetUserId()));
    }

    [HttpGet("holidays")]
    public async Task<IActionResult> GetHolidays([FromQuery] int? year)
    {
        var data = await _attendanceService.GetHolidaysAsync(year);
        return Success(data);
    }

    [HttpPost("holidays")]
    public async Task<IActionResult> CreateHoliday([FromBody] AttendanceHoliday entity)
    {
        return await ExecuteAsync(() => _attendanceService.CreateHolidayAsync(entity, GetUserId()));
    }

    [HttpPut("holidays/{id}")]
    public async Task<IActionResult> UpdateHoliday(long id, [FromBody] AttendanceHoliday entity)
    {
        entity.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdateHolidayAsync(entity, GetUserId()));
    }

    [HttpDelete("holidays/{id}")]
    public async Task<IActionResult> DeleteHoliday(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeleteHolidayAsync(id, GetUserId()));
    }

    [HttpGet("employee-ref-classes")]
    public async Task<IActionResult> GetEmployeeRefSchemeClasses([FromQuery] long? employeeId)
    {
        var data = await _attendanceService.GetEmployeeRefSchemeClassesAsync(employeeId);
        return Success(data);
    }

    [HttpPost("employee-ref-classes")]
    public async Task<IActionResult> AddEmployeeRefSchemeClass([FromBody] AttendanceEmployeeRefSchemeClass entity)
    {
        return await ExecuteAsync(() => _attendanceService.AddEmployeeRefSchemeClassAsync(entity, GetUserId()));
    }

    [HttpPut("employee-ref-classes/{id}")]
    public async Task<IActionResult> UpdateEmployeeRefSchemeClass(long id, [FromBody] AttendanceEmployeeRefSchemeClass entity)
    {
        entity.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdateEmployeeRefSchemeClassAsync(entity, GetUserId()));
    }

    [HttpDelete("employee-ref-classes/{id}")]
    public async Task<IActionResult> DeleteEmployeeRefSchemeClass(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeleteEmployeeRefSchemeClassAsync(id, GetUserId()));
    }

    [HttpGet("fee-calculators")]
    public async Task<IActionResult> GetFeeCalculators([FromQuery] long? dayTypeId)
    {
        var data = await _attendanceService.GetFeeCalculatorsAsync(dayTypeId);
        return Success(data);
    }

    [HttpPost("fee-calculators")]
    public async Task<IActionResult> CreateFeeCalculator([FromBody] AttendanceFeeCalculator entity)
    {
        return await ExecuteAsync(() => _attendanceService.CreateFeeCalculatorAsync(entity, GetUserId()));
    }

    [HttpPut("fee-calculators/{id}")]
    public async Task<IActionResult> UpdateFeeCalculator(long id, [FromBody] AttendanceFeeCalculator entity)
    {
        entity.Id = id;
        return await ExecuteAsync(() => _attendanceService.UpdateFeeCalculatorAsync(entity, GetUserId()));
    }

    [HttpDelete("fee-calculators/{id}")]
    public async Task<IActionResult> DeleteFeeCalculator(long id)
    {
        return await ExecuteAsync(() => _attendanceService.DeleteFeeCalculatorAsync(id, GetUserId()));
    }

    [HttpPost("hwatt/sync-employees")]
    public async Task<IActionResult> SyncEmployees()
    {
        var result = await _attendanceService.SyncEmployeesFromHwattAsync(GetUserId());
        return Success(result);
    }

    [HttpPost("hwatt/import-records")]
    public async Task<IActionResult> ImportDeviceAttendance([FromBody] AttendanceImportRequest request)
    {
        var count = await _attendanceService.ImportDeviceAttendanceAsync(request, GetUserId());
        return Success(new { count });
    }

    [HttpPost("hwatt/import-all-records")]
    public async Task<IActionResult> ImportAllEmployeeAttendance([FromBody] AttendanceAllEmployeeRequest request)
    {
        var taskId = _importTaskService.CreateTask();
        var userId = GetUserId();

        await _backgroundTaskService.EnqueueAsync(async (scopeFactory, ct) =>
        {
            using var scope = scopeFactory.CreateScope();
            var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

            await attendanceService.ImportAllEmployeeAttendanceAsync(request, userId,
                (progress, message) =>
                {
                    _importTaskService.UpdateProgress(taskId, progress, message);
                    _hubContext.Clients.Group(taskId).SendAsync("ProgressUpdate",
                        new { taskId, progress, message });
                });

            _importTaskService.Complete(taskId, "导入完成");
            await _hubContext.Clients.Group(taskId).SendAsync("Completed",
                new { taskId, message = "导入完成" });
        });

        return Success(new { taskId });
    }

    [HttpGet("hwatt/import-tasks/{taskId}")]
    public IActionResult GetImportStatus(string taskId)
    {
        var task = _importTaskService.GetTask(taskId);
        if (task == null)
            return Fail("任务不存在");
        return Success(task);
    }

    [HttpPost("hwatt/sync-card-records")]
    public async Task<IActionResult> SyncCardRecords([FromBody] AttendanceCardRecordsSyncRequest request)
    {
        try
        {
            var result = await _attendanceService.SyncCardRecordsFromHwattAsync(request.StartDate, request.EndDate, GetUserId());
            return Success(result);
        }
        catch (Exception ex)
        {
            return Fail($"同步HWATT打卡记录失败: {ex.Message}");
        }
    }

    // ========== 钉钉数据导入 ==========

    [HttpPost("dingtalk/sync-employees")]
    public async Task<IActionResult> SyncDingTalkEmployees()
    {
        try
        {
            var result = await _attendanceService.SyncEmployeesFromDingTalkAsync(GetUserId());
            return Success(result);
        }
        catch (Exception ex)
        {
            return Fail($"同步钉钉员工失败: {ex.Message}");
        }
    }

    [HttpPost("dingtalk/sync-card-records")]
    public async Task<IActionResult> SyncDingTalkCardRecords([FromBody] DingTalkCardRecordsSyncRequest request)
    {
        try
        {
            var result = await _attendanceService.SyncCardRecordsFromDingTalkAsync(request.StartDate, request.EndDate, GetUserId());
            return Success(result);
        }
        catch (Exception ex)
        {
            return Fail($"同步钉钉打卡记录失败: {ex.Message}");
        }
    }

    [HttpPost("card-records")]
    public async Task<IActionResult> GetCardRecords([FromBody] CardRecordSearchRequest request)
    {
        var data = await _attendanceService.GetCardRecordsAsync(request);
        return Success(data);
    }
}