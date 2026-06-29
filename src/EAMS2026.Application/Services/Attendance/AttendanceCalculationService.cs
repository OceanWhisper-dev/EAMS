using EAMS2026.Application.Common.Interfaces.Attendance;
using EAMS2026.Domain.Entities.Attendance;
using EAMS2026.Domain.Interfaces.Repositories.Attendance;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.Attendance;

public class AttendanceCalculationService : IAttendanceCalculationService
{
    private readonly IAttendanceRepository _repo;
    private readonly ILogger<AttendanceCalculationService> _logger;

    public AttendanceCalculationService(
        IAttendanceRepository repo,
        ILogger<AttendanceCalculationService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<int> DoAllEmployeeAttendanceAsync(DateTime doDate, DateTime? endDate = null, long operatorId = 0)
    {
        var end = endDate ?? new DateTime(doDate.Year, doDate.Month, DateTime.DaysInMonth(doDate.Year, doDate.Month));

        var employees = await _repo.GetEmployeeRefSchemeClassesAsync(null);
        var employeeIds = employees.Select(e => e.EmployeeId).Distinct().ToList();

        int totalProcessed = 0;
        foreach (var eid in employeeIds)
        {
            _logger.LogInformation("Processing employee {EmployeeId}, {DoDate} - {EndDate}", eid, doDate, end);

            for (var date = doDate; date <= end; date = date.AddDays(1))
            {
                var recordId = await SaveAttendanceRecordAsync(eid, date, null, null, operatorId);
                await ScheduleOneDayAsync(eid, date, operatorId, recordId);
                await ReCalculateOneDayAsync(eid, date, recordId);
                totalProcessed++;
            }
        }

        return totalProcessed;
    }

    public async Task<int> ImportFromHwattDeviceAsync(
        long employeeId,
        DateTime beginDate,
        DateTime endDate,
        Func<long, DateTime, Task<(TimeSpan? cardBegin, TimeSpan? cardEnd)>> hwattCardQuery,
        long operatorId = 0)
    {
        int importCount = 0;
        for (var date = beginDate; date <= endDate; date = date.AddDays(1))
        {
            var (cardBegin, cardEnd) = await hwattCardQuery(employeeId, date);
            await SaveAttendanceRecordAsync(employeeId, date, cardBegin, cardEnd, operatorId);
            importCount++;
        }
        return importCount;
    }

    public async Task<long> SaveAttendanceRecordAsync(
        long employeeId,
        DateTime sDate,
        TimeSpan? cardBegin,
        TimeSpan? cardEnd,
        long operatorId = 0)
    {
        var existing = await _repo.GetRecordByEmployeeAndDateAsync(employeeId, sDate);

        long recordId;
        if (existing == null)
        {
            var record = new AttendanceRecord
            {
                EmployeeId = employeeId,
                SDate = sDate,
                BAttTime = cardBegin ?? TimeSpan.Zero,
                EAttTime = cardEnd ?? TimeSpan.Zero,
                CreatedBy = operatorId,
                UpdatedBy = operatorId
            };
            recordId = await _repo.AddRecordAsync(record);
            _logger.LogInformation("INSERT record: emp={EmpId}, date={Date}, bAtt={B}, eAtt={E}, id={Id}",
                employeeId, sDate.ToString("yyyy-MM-dd"), cardBegin, cardEnd, recordId);
        }
        else
        {
            var bAtt = existing.BAttTime;
            if (bAtt == TimeSpan.Zero || (cardBegin.HasValue && cardBegin.Value < bAtt))
                bAtt = cardBegin ?? bAtt;

            var eAtt = existing.EAttTime;
            if (eAtt == TimeSpan.Zero || (cardEnd.HasValue && cardEnd.Value > eAtt))
                eAtt = cardEnd ?? eAtt;

            existing.BAttTime = bAtt;
            existing.EAttTime = eAtt;
            existing.UpdatedBy = operatorId;
            await _repo.UpdateRecordAsync(existing);
            recordId = existing.Id;
            _logger.LogInformation("UPDATE record: emp={EmpId}, date={Date}, bAtt={B}, eAtt={E}, id={Id}",
                employeeId, sDate.ToString("yyyy-MM-dd"), bAtt, eAtt, recordId);
        }
        return recordId;
    }

    public async Task ScheduleOneDayAsync(long employeeId, DateTime scheduleDate, long operatorId = 0, long? existingRecordId = null)
    {
        _logger.LogDebug("ScheduleOneDay: emp={EmpId}, date={Date}", employeeId, scheduleDate.ToString("yyyy-MM-dd"));

        var activeScheme = await _repo.GetEmployeeActiveSchemeAsync(employeeId, scheduleDate);
        if (activeScheme == null)
        {
            _logger.LogWarning("No active scheme for emp={EmpId} on {Date}", employeeId, scheduleDate.ToString("yyyy-MM-dd"));
            return;
        }

        var schemeClass = await _repo.GetSchemeClassByIdAsync(activeScheme.ClassId);
        if (schemeClass == null) return;

        int periods = schemeClass.Periods ?? 1;

        var prevDayRecord = await _repo.GetRecordByEmployeeAndDateAsync(employeeId, scheduleDate.AddDays(-1));
        int period2;
        if (prevDayRecord != null && prevDayRecord.PeriodNo > 0)
        {
            period2 = (prevDayRecord.PeriodNo % periods) + 1;
        }
        else
        {
            int daysSinceEff = (int)((scheduleDate.Date - activeScheme.EffDate.Date).Days);
            period2 = ((activeScheme.PeriodNo ?? 1) - 1 + daysSinceEff) % periods + 1;
        }

        _logger.LogDebug("Period calc: {Period}/{Periods}", period2, periods);

        TimeSpan? bTime = null;
        TimeSpan? eTime = null;
        long? planId = null;
        long? classId = null;
        long? dayTypeId = null;

        var planRefClasses = await _repo.GetPlanRefClassesAsync(null);
        var planTimes = await _repo.GetPlanTimesAsync();

        var currentRef = planRefClasses
            .FirstOrDefault(r => r.PeriodNo == period2 && r.ClassId == activeScheme.ClassId);

        if (currentRef != null)
        {
            var planTime = planTimes.FirstOrDefault(p => p.Id == currentRef.PlanId);
            if (planTime != null)
            {
                bTime = planTime.BTime;
                eTime = planTime.ETime;
                planId = planTime.Id;
                dayTypeId = planTime.DayTypeId;
                classId = activeScheme.ClassId;
            }
        }

        _logger.LogDebug("Plan info: period={Period}, planId={PlanId}, classId={ClassId}, bTime={BTime}, eTime={ETime}, dayType={DayType}",
            period2, planId, classId, bTime, eTime, dayTypeId);

        var effScheme = await _repo.GetEmployeeActiveSchemeAsync(employeeId, scheduleDate);
        if (effScheme != null && effScheme.EffDate.Date == scheduleDate.Date)
        {
            var effRef = planRefClasses
                .FirstOrDefault(r => r.PeriodNo == effScheme.PeriodNo && r.ClassId == effScheme.ClassId);

            if (effRef != null)
            {
                var effPlan = planTimes.FirstOrDefault(p => p.Id == effRef.PlanId);
                if (effPlan != null)
                {
                    period2 = effScheme.PeriodNo ?? period2;
                    classId = effScheme.ClassId;
                    planId = effPlan.Id;
                    bTime = effPlan.BTime;
                    eTime = effPlan.ETime;
                    dayTypeId = effPlan.DayTypeId;
                    _logger.LogDebug("EffDate plan override: period={Period}, planId={PlanId}", period2, planId);
                }
            }
        }

        var holiday = await _repo.GetHolidayByDateAsync(scheduleDate);
        if (holiday != null)
        {
            if (holiday.BTime is null || holiday.BTime == TimeSpan.Zero)
                dayTypeId = 5;
            else
                dayTypeId = 1;

            planId = (holiday.BTime is null || holiday.BTime == TimeSpan.Zero) ? 12 : 11;
            bTime = holiday.BTime;
            eTime = holiday.ETime;
            _logger.LogDebug("Holiday override: {Name}, dayType={DayType}, planId={PlanId}", holiday.SName, dayTypeId, planId);
        }

        //if (holiday != null && (planId == 11 || planId == 12) && scheduleDate.DayOfWeek == DayOfWeek.Saturday)
        if ( (planId == 11 || planId == 12) && scheduleDate.DayOfWeek == DayOfWeek.Saturday)
        {
            period2 = await ResetSchedulePeriodAsync(employeeId, scheduleDate, period2);
        }

        AttendanceRecord? existingRecord;
        if (existingRecordId.HasValue)
        {
            existingRecord = await _repo.GetRecordByIdAsync(existingRecordId.Value);
        }
        else
        {
            existingRecord = await _repo.GetRecordByEmployeeAndDateAsync(employeeId, scheduleDate);
        }

        if (existingRecord == null)
        {
            var newRecord = new AttendanceRecord
            {
                EmployeeId = employeeId,
                SDate = scheduleDate,
                PeriodNo = period2,
                ClassId = classId,
                BTime = bTime,
                ETime = eTime,
                DayTypeId = dayTypeId,
                PlanId = planId,
                CreatedBy = operatorId,
                UpdatedBy = operatorId
            };
            await _repo.AddRecordAsync(newRecord);
            _logger.LogInformation("Schedule INSERT: emp={EmpId}, date={Date}, period={Period}",
                employeeId, scheduleDate.ToString("yyyy-MM-dd"), period2);
        }
        else
        {
            existingRecord.PeriodNo = period2;
            existingRecord.ClassId = classId;
            existingRecord.BTime = bTime;
            existingRecord.ETime = eTime;
            existingRecord.DayTypeId = dayTypeId;
            existingRecord.PlanId = planId;
            existingRecord.UpdatedBy = operatorId;
            await _repo.UpdateRecordAsync(existingRecord);
            _logger.LogInformation("Schedule UPDATE: emp={EmpId}, date={Date}, period={Period}",
                employeeId, scheduleDate.ToString("yyyy-MM-dd"), period2);
        }
    }

    public async Task<int> ResetSchedulePeriodAsync(long employeeId, DateTime sDate, int currentPeriod)
    {
        var activeScheme = await _repo.GetEmployeeActiveSchemeAsync(employeeId, sDate);
        if (activeScheme == null) return currentPeriod;

        var schemeClass = await _repo.GetSchemeClassByIdAsync(activeScheme.ClassId);
        if (schemeClass == null) return currentPeriod;

        int periods = schemeClass.Periods ?? 1;
        int classPeriods = schemeClass.ClassPeriods ?? 0;

        int period = (periods + (currentPeriod - classPeriods)) % periods;
        if (period == 0) period = periods;

        _logger.LogDebug("ResetSchedulePeriod: emp={EmpId}, date={Date}, from={From} to={To}, periods={Periods}, classPeriods={ClassPeriods}",
            employeeId, sDate.ToString("yyyy-MM-dd"), currentPeriod, period, periods, classPeriods);

        return period;
    }

    public async Task ReCalculateOneDayAsync(long employeeId, DateTime attDate, long? existingRecordId = null)
    {
        AttendanceRecord? record;
        if (existingRecordId.HasValue)
        {
            record = await _repo.GetRecordByIdAsync(existingRecordId.Value);
        }
        else
        {
            record = await _repo.GetRecordByEmployeeAndDateAsync(employeeId, attDate);
        }

        if (record == null)
        {
            _logger.LogWarning("ReCalculate: no record for emp={EmpId} on {Date}", employeeId, attDate.ToString("yyyy-MM-dd"));
            return;
        }

        var bAttTime = record.BAttTime ?? TimeSpan.Zero;
        var eAttTime = record.EAttTime ?? TimeSpan.Zero;
        bAttTime = new TimeSpan(bAttTime.Hours, bAttTime.Minutes, 0);
        eAttTime = new TimeSpan(eAttTime.Hours, eAttTime.Minutes, 0);
        var bTime = record.BTime ?? TimeSpan.Zero;
        var eTime = record.ETime ?? TimeSpan.Zero;
        var dayTypeId = record.DayTypeId ?? 0;
        var planId = record.PlanId ?? 0;

        int bOverTime;
        int eOverTime;

        if (planId == 7 || planId == 8 || planId == 12)
        {
            bOverTime = 0;
            eOverTime = bAttTime == TimeSpan.Zero || eAttTime == TimeSpan.Zero
                ? 0
                : (int)Math.Ceiling((eAttTime - bAttTime).TotalMinutes);
        }
        else
        {
            bOverTime = bAttTime == TimeSpan.Zero ? 0 : (int)Math.Ceiling((bTime - bAttTime).TotalMinutes);
            eOverTime = (int)Math.Ceiling((eAttTime - eTime).TotalMinutes);
        }

        _logger.LogDebug("ReCalculate: emp={EmpId}, date={Date}, bOver={B}, eOver={E}, dayType={DayType}, plan={Plan}",
            employeeId, attDate.ToString("yyyy-MM-dd"), bOverTime, eOverTime, dayTypeId, planId);

        var feeCalculators = await _repo.GetFeeCalculatorsAsync(dayTypeId);
        var calcList = feeCalculators.ToList();

        decimal bFee = calcList
            .Where(c => c.RangeA < bOverTime && bOverTime <= c.RangeB)
            .Select(c => c.RangePrice)
            .FirstOrDefault();

        decimal eFee = calcList
            .Where(c => c.RangeA < eOverTime && eOverTime <= c.RangeB)
            .Select(c => c.RangePrice)
            .FirstOrDefault();

        _logger.LogDebug("ReCalculate fees: bFee={BFee}, eFee={EFee}", bFee, eFee);

        record.BOffset = bOverTime;
        record.EOffset = eOverTime;
        record.BOffsetFee = bFee;
        record.EOffsetFee = eFee;
        await _repo.UpdateRecordAsync(record);

        await _repo.UpdateEventFeeByRecordIdAsync(record.Id, true, bFee);
        await _repo.UpdateEventFeeByRecordIdAsync(record.Id, false, eFee);
    }
}