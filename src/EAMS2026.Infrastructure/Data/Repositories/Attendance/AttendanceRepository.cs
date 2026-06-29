using Dapper;
using EAMS2026.Domain.Entities.Attendance;
using EAMS2026.Domain.Interfaces.Repositories.Attendance;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.Attendance;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public AttendanceRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<AttendanceReport>> GetReportAsync(long? employeeId, DateTime startDate, DateTime endDate, long? departmentId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT 
            r.id AS RecordId,
            r.employee_id AS EmployeeId,
            COALESCE(se.name, ae.employee_name) AS EmployeeName,
            r.s_date AS SDate,
            COALESCE(h.s_description, pt.description) AS Description,
            r.b_att_time AS BAttTime,
            r.b_offset AS BOffset,
            r.e_att_time AS EAttTime,
            r.e_offset AS EOffset,
            r.b_offset_fee AS BOffsetFee,
            r.e_offset_fee AS EOffsetFee,
            COALESCE((SELECT string_agg(
                CASE WHEN ed.is_begin_time THEN '早:' || ed.event_description ELSE '晚:' || ed.event_description END,
                ';' ORDER BY ed.is_begin_time
            ) FROM attendance_event_declareds ed WHERE ed.record_id = r.id AND ed.is_deleted = FALSE), '') AS Event,
            COALESCE((SELECT MAX(ed.fee) FROM attendance_event_declareds ed WHERE ed.record_id = r.id AND ed.fee > 0 AND ed.is_deleted = FALSE), 0) AS Fee
        FROM attendance_records r
        LEFT JOIN attendance_plan_times pt ON r.plan_id = pt.id
        LEFT JOIN attendance_holidays h ON r.s_date = h.s_date AND h.is_deleted = FALSE
        LEFT JOIN (
            SELECT DISTINCT ON (employee_id) employee_id, employee_name, system_employee_id
            FROM attendance_employees
            WHERE is_deleted = FALSE
            ORDER BY employee_id
        ) ae ON r.employee_id = ae.employee_id
        LEFT JOIN sys_employees se ON ae.system_employee_id = se.id AND se.is_deleted = FALSE
        WHERE r.is_deleted = FALSE
          AND r.s_date >= @StartDate::date AND r.s_date <= @EndDate::date
          AND ae.system_employee_id IS NOT NULL
          AND (se.resignation_date IS NULL OR r.s_date <= se.resignation_date)";
        if (employeeId.HasValue)
            sql += " AND r.employee_id = @EmployeeId";
        if (departmentId.HasValue)
            sql += " AND ae.system_employee_id IN (SELECT id FROM sys_employees WHERE department_id = @DepartmentId AND is_deleted = FALSE)";
        sql += " ORDER BY r.s_date";

        return await conn.QueryAsync<AttendanceReport>(sql, new { EmployeeId = employeeId, StartDate = startDate.ToString("yyyy-MM-dd"), EndDate = endDate.ToString("yyyy-MM-dd"), DepartmentId = departmentId });
    }

    public async Task<IEnumerable<AttendanceEmployee>> GetEmployeesAsync(long? departmentId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT ae.id, ae.employee_id AS EmployeeId,
                           COALESCE(ae.employee_name,
                               CASE WHEN ae.source = 'hwatt' THEN he.employee_name
                                    WHEN ae.source = 'dingtalk' THEN de.user_name
                                    ELSE NULL END
                           ) AS EmployeeName,
                           ae.source AS Source, ae.hwatt_employee_id AS HwattEmployeeId,
                           ae.system_employee_id AS SystemEmployeeId,
                           e.name AS SystemEmployeeName
                    FROM attendance_employees ae
                    LEFT JOIN sys_employees e ON ae.system_employee_id = e.id AND e.is_deleted = FALSE
                    LEFT JOIN attendance_hwatt_employees he ON ae.source = 'hwatt' AND ae.hwatt_employee_id = he.employee_id AND he.is_deleted = FALSE
                    LEFT JOIN attendance_dingtalk_employees de ON ae.source = 'dingtalk' AND ae.hwatt_employee_id IS NOT NULL AND ae.hwatt_employee_id::varchar = de.user_id AND de.is_deleted = FALSE
                    WHERE ae.is_deleted = FALSE";
        if (departmentId.HasValue)
            sql += " AND ae.system_employee_id IN (SELECT id FROM sys_employees WHERE department_id = @DepartmentId AND is_deleted = FALSE)";
        sql += " ORDER BY ae.employee_id";
        return await conn.QueryAsync<AttendanceEmployee>(sql, new { DepartmentId = departmentId });
    }

    public async Task<AttendanceEmployee?> GetEmployeeByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT ae.id, ae.employee_id AS EmployeeId,
                           COALESCE(ae.employee_name,
                               CASE WHEN ae.source = 'hwatt' THEN he.employee_name
                                    WHEN ae.source = 'dingtalk' THEN de.user_name
                                    ELSE NULL END
                           ) AS EmployeeName,
                           ae.source AS Source, ae.hwatt_employee_id AS HwattEmployeeId,
                           ae.system_employee_id AS SystemEmployeeId,
                           e.name AS SystemEmployeeName
                    FROM attendance_employees ae
                    LEFT JOIN sys_employees e ON ae.system_employee_id = e.id AND e.is_deleted = FALSE
                    LEFT JOIN attendance_hwatt_employees he ON ae.source = 'hwatt' AND ae.hwatt_employee_id = he.employee_id AND he.is_deleted = FALSE
                    LEFT JOIN attendance_dingtalk_employees de ON ae.source = 'dingtalk' AND ae.hwatt_employee_id IS NOT NULL AND ae.hwatt_employee_id::varchar = de.user_id AND de.is_deleted = FALSE
                    WHERE ae.id = @Id AND ae.is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<AttendanceEmployee>(sql, new { Id = id });
    }

    public async Task<IEnumerable<AttendanceEmployee>> GetEmployeeBySystemEmployeeIdAsync(long? systemEmployeeId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT ae.id, ae.employee_id AS EmployeeId,
                           COALESCE(ae.employee_name,
                               CASE WHEN ae.source = 'hwatt' THEN he.employee_name
                                    WHEN ae.source = 'dingtalk' THEN de.user_name
                                    ELSE NULL END
                           ) AS EmployeeName,
                           ae.source AS Source, ae.hwatt_employee_id AS HwattEmployeeId,
                           ae.system_employee_id AS SystemEmployeeId,
                           e.name AS SystemEmployeeName
                    FROM attendance_employees ae
                    LEFT JOIN sys_employees e ON ae.system_employee_id = e.id AND e.is_deleted = FALSE
                    LEFT JOIN attendance_hwatt_employees he ON ae.source = 'hwatt' AND ae.hwatt_employee_id = he.employee_id AND he.is_deleted = FALSE
                    LEFT JOIN attendance_dingtalk_employees de ON ae.source = 'dingtalk' AND ae.hwatt_employee_id IS NOT NULL AND ae.hwatt_employee_id::varchar = de.user_id AND de.is_deleted = FALSE
                    WHERE ae.is_deleted = FALSE AND ae.system_employee_id = @SystemEmployeeId
                    ORDER BY ae.employee_id";
        return await conn.QueryAsync<AttendanceEmployee>(sql, new { SystemEmployeeId = systemEmployeeId });
    }

    public async Task<long> AddEmployeeAsync(AttendanceEmployee entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_employees (employee_id, employee_name, source, hwatt_employee_id, system_employee_id, created_at, created_by, updated_at, updated_by)
                    VALUES (@EmployeeId, @EmployeeName, @Source, @HwattEmployeeId, @SystemEmployeeId, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateEmployeeAsync(AttendanceEmployee entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE attendance_employees SET employee_name = @EmployeeName, hwatt_employee_id = @HwattEmployeeId,
                    system_employee_id = @SystemEmployeeId,
                    updated_at = NOW(), updated_by = @UpdatedBy WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> DeleteEmployeeAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_employees SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id AND is_deleted = FALSE", new { Id = id }) > 0;
    }

    public async Task<int> SaveHwattCardRecordsAsync(IEnumerable<HwattCardRecordDto> records)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_hwatt_card_records (employee_id, card_time, card_type_id, dev_id, created_at)
                    VALUES (@EmployeeId, @CardTime, @CardTypeId, @DevId, NOW())
                    ON CONFLICT (employee_id, card_time) DO NOTHING";
        return await conn.ExecuteAsync(sql, records);
    }

    public async Task<IEnumerable<AttendanceRecord>> GetRecordsAsync(long? employeeId, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT id, employee_id, period_no, s_date, b_att_time, e_att_time, b_offset, e_offset, b_offset_fee, e_offset_fee, b_time, e_time, day_type_id, plan_id, class_id, fee, is_deleted, created_at, created_by, updated_at, updated_by FROM attendance_records WHERE is_deleted = FALSE";
        if (employeeId.HasValue) sql += " AND employee_id = @EmployeeId";
        if (startDate.HasValue) sql += " AND s_date >= @StartDate";
        if (endDate.HasValue) sql += " AND s_date <= @EndDate";
        sql += " ORDER BY s_date DESC LIMIT @PageSize OFFSET @Offset";
        return await conn.QueryAsync<AttendanceRecord>(sql, new { EmployeeId = employeeId, StartDate = startDate, EndDate = endDate, PageSize = pageSize, Offset = (page - 1) * pageSize });
    }

    public async Task<int> GetRecordsCountAsync(long? employeeId, DateTime? startDate, DateTime? endDate)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT COUNT(*) FROM attendance_records WHERE is_deleted = FALSE";
        if (employeeId.HasValue) sql += " AND employee_id = @EmployeeId";
        if (startDate.HasValue) sql += " AND s_date >= @StartDate";
        if (endDate.HasValue) sql += " AND s_date <= @EndDate";
        return await conn.ExecuteScalarAsync<int>(sql, new { EmployeeId = employeeId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<AttendanceRecord?> GetRecordByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceRecord>(
            "SELECT id, employee_id, period_no, s_date, b_att_time, e_att_time, b_offset, e_offset, b_offset_fee, e_offset_fee, b_time, e_time, day_type_id, plan_id, class_id, fee, is_deleted, created_at, created_by, updated_at, updated_by FROM attendance_records WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
    }

    public async Task<long> AddRecordAsync(AttendanceRecord record)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_records (employee_id, period_no, s_date, b_att_time, e_att_time, b_offset, e_offset, b_offset_fee, e_offset_fee, b_time, e_time, day_type_id, plan_id, class_id, created_at, created_by, updated_at, updated_by)
                    VALUES (@EmployeeId, @PeriodNo, @SDate, @BAttTime, @EAttTime, @BOffset, @EOffset, @BOffsetFee, @EOffsetFee, @BTime, @ETime, @DayTypeId, @PlanId, @ClassId, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, record);
    }

    public async Task<bool> UpdateRecordAsync(AttendanceRecord record)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE attendance_records SET period_no=@PeriodNo, b_att_time=@BAttTime, e_att_time=@EAttTime, b_offset=@BOffset, e_offset=@EOffset, b_offset_fee=@BOffsetFee, e_offset_fee=@EOffsetFee, b_time=@BTime, e_time=@ETime, day_type_id=@DayTypeId, plan_id=@PlanId, class_id=@ClassId, updated_at=NOW(), updated_by=@UpdatedBy WHERE id=@Id";
        return await conn.ExecuteAsync(sql, record) > 0;
    }

    public async Task<bool> DeleteRecordAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_records SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<AttendanceRecord?> GetRecordByEmployeeAndDateAsync(long employeeId, DateTime sDate)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceRecord>(
            "SELECT id, employee_id, period_no, s_date, b_att_time, e_att_time, b_offset, e_offset, b_offset_fee, e_offset_fee, b_time, e_time, day_type_id, plan_id, class_id, fee, is_deleted, created_at, created_by, updated_at, updated_by FROM attendance_records WHERE employee_id = @EmployeeId AND s_date = @SDate AND is_deleted = FALSE",
            new { EmployeeId = employeeId, SDate = sDate });
    }

    public async Task<long> UpsertRecordAsync(AttendanceRecord record)
    {
        using var conn = _connectionFactory.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<AttendanceRecord>(
            "SELECT id, employee_id, period_no, s_date, b_att_time, e_att_time, b_offset, e_offset, b_offset_fee, e_offset_fee, b_time, e_time, day_type_id, plan_id, class_id, fee, is_deleted, created_at, created_by, updated_at, updated_by FROM attendance_records WHERE employee_id = @EmployeeId AND s_date = @SDate AND is_deleted = FALSE",
            new { record.EmployeeId, record.SDate });

        if (existing == null)
        {
            var insertSql = @"INSERT INTO attendance_records (employee_id, period_no, s_date, b_att_time, e_att_time, b_offset, e_offset, b_offset_fee, e_offset_fee, b_time, e_time, day_type_id, plan_id, class_id, created_at, created_by, updated_at, updated_by)
                              VALUES (@EmployeeId, @PeriodNo, @SDate, @BAttTime, @EAttTime, @BOffset, @EOffset, @BOffsetFee, @EOffsetFee, @BTime, @ETime, @DayTypeId, @PlanId, @ClassId, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                              RETURNING id";
            return await conn.ExecuteScalarAsync<long>(insertSql, record);
        }
        else
        {
            var bAttTime = existing.BAttTime;
            if (bAttTime == TimeSpan.Zero || (record.BAttTime != TimeSpan.Zero && record.BAttTime < bAttTime))
                bAttTime = record.BAttTime;

            var eAttTime = existing.EAttTime;
            if (eAttTime == TimeSpan.Zero || (record.EAttTime != TimeSpan.Zero && record.EAttTime > eAttTime))
                eAttTime = record.EAttTime;

            await conn.ExecuteAsync(
                "UPDATE attendance_records SET b_att_time = @BAttTime, e_att_time = @EAttTime, updated_at = NOW(), updated_by = @UpdatedBy WHERE id = @Id",
                new { BAttTime = bAttTime, EAttTime = eAttTime, UpdatedBy = record.UpdatedBy, Id = existing.Id });

            return existing.Id;
        }
    }

    public async Task<IEnumerable<AttendanceDayType>> GetDayTypesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<AttendanceDayType>("SELECT * FROM attendance_day_types WHERE is_deleted = FALSE ORDER BY id");
    }

    public async Task<AttendanceDayType?> GetDayTypeByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceDayType>("SELECT * FROM attendance_day_types WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
    }

    public async Task<long> AddDayTypeAsync(AttendanceDayType entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_day_types (day_type_name, day_type_caption, created_at, created_by, updated_at, updated_by)
                    VALUES (@DayTypeName, @DayTypeCaption, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateDayTypeAsync(AttendanceDayType entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_day_types SET day_type_name=@DayTypeName, day_type_caption=@DayTypeCaption, updated_at=NOW(), updated_by=@UpdatedBy WHERE id=@Id", entity) > 0;
    }

    public async Task<bool> DeleteDayTypeAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_day_types SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<IEnumerable<AttendanceSchemeClass>> GetSchemeClassesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<AttendanceSchemeClass>("SELECT * FROM attendance_scheme_classes WHERE is_deleted = FALSE ORDER BY id");
    }

    public async Task<AttendanceSchemeClass?> GetSchemeClassByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceSchemeClass>("SELECT * FROM attendance_scheme_classes WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
    }

    public async Task<long> AddSchemeClassAsync(AttendanceSchemeClass entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_scheme_classes (class_name, periods, class_description, class_periods, created_at, created_by, updated_at, updated_by)
                    VALUES (@ClassName, @Periods, @ClassDescription, @ClassPeriods, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateSchemeClassAsync(AttendanceSchemeClass entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_scheme_classes SET class_name=@ClassName, periods=@Periods, class_description=@ClassDescription, class_periods=@ClassPeriods, updated_at=NOW(), updated_by=@UpdatedBy WHERE id=@Id", entity) > 0;
    }

    public async Task<bool> DeleteSchemeClassAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_scheme_classes SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<IEnumerable<AttendancePlanTime>> GetPlanTimesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<AttendancePlanTime>("SELECT * FROM attendance_plan_times WHERE is_deleted = FALSE ORDER BY id");
    }

    public async Task<AttendancePlanTime?> GetPlanTimeByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendancePlanTime>("SELECT * FROM attendance_plan_times WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
    }

    public async Task<long> AddPlanTimeAsync(AttendancePlanTime entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_plan_times (plan_name, day_type_id, description, b_time, e_time, created_at, created_by, updated_at, updated_by)
                    VALUES (@PlanName, @DayTypeId, @Description, @BTime, @ETime, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdatePlanTimeAsync(AttendancePlanTime entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_plan_times SET plan_name=@PlanName, day_type_id=@DayTypeId, description=@Description, b_time=@BTime, e_time=@ETime, updated_at=NOW(), updated_by=@UpdatedBy WHERE id=@Id", entity) > 0;
    }

    public async Task<bool> DeletePlanTimeAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_plan_times SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<IEnumerable<AttendancePlanRefClass>> GetPlanRefClassesAsync(long? planId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM attendance_plan_ref_classes WHERE is_deleted = FALSE";
        if (planId.HasValue) sql += " AND plan_id = @PlanId";
        sql += " ORDER BY period_no";
        return await conn.QueryAsync<AttendancePlanRefClass>(sql, new { PlanId = planId });
    }

    public async Task<long> AddPlanRefClassAsync(AttendancePlanRefClass entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_plan_ref_classes (class_id, plan_id, period_no, created_at, created_by, updated_at, updated_by)
                    VALUES (@ClassId, @PlanId, @PeriodNo, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> DeletePlanRefClassAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("DELETE FROM attendance_plan_ref_classes WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<IEnumerable<AttendanceEventDeclared>> GetEventsByRecordIdAsync(long recordId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<AttendanceEventDeclared>(
            "SELECT * FROM attendance_event_declareds WHERE record_id = @RecordId AND is_deleted = FALSE ORDER BY is_begin_time",
            new { RecordId = recordId });
    }

    public async Task<long> AddEventAsync(AttendanceEventDeclared entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_event_declareds (event_description, fee, memo, check_man, manager, record_id, is_begin_time, created_at, created_by, updated_at, updated_by)
                    VALUES (@EventDescription, @Fee, @Memo, @CheckMan, @Manager, @RecordId, @IsBeginTime, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateEventAsync(AttendanceEventDeclared entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_event_declareds SET event_description=@EventDescription, fee=@Fee, memo=@Memo, check_man=@CheckMan, manager=@Manager, is_begin_time=@IsBeginTime, updated_at=NOW(), updated_by=@UpdatedBy WHERE id=@Id", entity) > 0;
    }

    public async Task<bool> DeleteEventAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_event_declareds SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<AttendanceEventDeclared?> GetEventByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceEventDeclared>(
            "SELECT * FROM attendance_event_declareds WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
    }

    public async Task<bool> RecalculateRecordFeeAsync(long recordId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE attendance_records
                    SET fee = (SELECT MAX(ed.fee) FROM attendance_event_declareds ed WHERE ed.record_id = @RecordId AND ed.fee > 0 AND ed.is_deleted = FALSE),
                        updated_at = NOW()
                    WHERE id = @RecordId";
        try
        {
            return await conn.ExecuteAsync(sql, new { RecordId = recordId }) > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateEventFeeByRecordIdAsync(long recordId, bool isBeginTime, decimal fee)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "UPDATE attendance_event_declareds SET fee = @Fee, updated_at = NOW() WHERE record_id = @RecordId AND is_begin_time = @IsBeginTime AND is_deleted = FALSE",
            new { RecordId = recordId, IsBeginTime = isBeginTime, Fee = fee }) > 0;
    }

    public async Task<IEnumerable<AttendanceHoliday>> GetHolidaysAsync(int? year)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM attendance_holidays WHERE is_deleted = FALSE";
        if (year.HasValue) sql += " AND i_year = @Year";
        sql += " ORDER BY s_date";
        return await conn.QueryAsync<AttendanceHoliday>(sql, new { Year = year });
    }

    public async Task<AttendanceHoliday?> GetHolidayByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceHoliday>("SELECT * FROM attendance_holidays WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
    }

    public async Task<AttendanceHoliday?> GetHolidayByDateAsync(DateTime date)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceHoliday>(
            "SELECT * FROM attendance_holidays WHERE s_date = @Date AND is_deleted = FALSE",
            new { Date = date });
    }

    public async Task<long> AddHolidayAsync(AttendanceHoliday entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_holidays (i_year, s_date, s_name, b_time, e_time, s_description, created_at, created_by, updated_at, updated_by)
                    VALUES (@IYear, @SDate, @SName, @BTime, @ETime, @SDescription, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateHolidayAsync(AttendanceHoliday entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_holidays SET i_year=@IYear, s_date=@SDate, s_name=@SName, b_time=@BTime, e_time=@ETime, s_description=@SDescription, updated_at=NOW(), updated_by=@UpdatedBy WHERE id=@Id", entity) > 0;
    }

    public async Task<bool> DeleteHolidayAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_holidays SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<IEnumerable<AttendanceEmployeeRefSchemeClass>> GetEmployeeRefSchemeClassesAsync(long? employeeId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT r.*, e.employee_name AS EmployeeName, s.class_name AS ClassName
                    FROM attendance_employee_ref_scheme_classes r
                    LEFT JOIN attendance_employees e ON e.employee_id = r.employee_id AND e.is_deleted = FALSE
                    LEFT JOIN attendance_scheme_classes s ON s.id = r.class_id AND s.is_deleted = FALSE
                    WHERE r.is_deleted = FALSE 
					and e.employee_id is not null";
        if (employeeId.HasValue) sql += " AND r.employee_id = @EmployeeId";
        sql += " ORDER BY r.eff_date DESC, r.id";
        return await conn.QueryAsync<AttendanceEmployeeRefSchemeClass>(sql, new { EmployeeId = employeeId });
    }

    public async Task<long> AddEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_employee_ref_scheme_classes (employee_id, class_id, period_no, eff_date, exp_date, created_at, created_by, updated_at, updated_by)
                    VALUES (@EmployeeId, @ClassId, @PeriodNo, @EffDate, @ExpDate, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateEmployeeRefSchemeClassAsync(AttendanceEmployeeRefSchemeClass entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE attendance_employee_ref_scheme_classes
                    SET employee_id = @EmployeeId, class_id = @ClassId, period_no = @PeriodNo,
                        eff_date = @EffDate, exp_date = @ExpDate,
                        updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<AttendanceEmployeeRefSchemeClass?> GetEmployeeActiveSchemeAsync(long employeeId, DateTime date)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<AttendanceEmployeeRefSchemeClass>(
            @"SELECT * FROM attendance_employee_ref_scheme_classes
              WHERE employee_id = @EmployeeId
                AND eff_date <= @Date
                AND (exp_date IS NULL OR exp_date >= @Date)
                AND is_deleted = FALSE
              ORDER BY eff_date DESC
              LIMIT 1",
            new { EmployeeId = employeeId, Date = date });
    }

    public async Task<bool> DeleteEmployeeRefSchemeClassAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_employee_ref_scheme_classes SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<IEnumerable<AttendanceFeeCalculator>> GetFeeCalculatorsAsync(long? dayTypeId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM attendance_fee_calculators WHERE is_deleted = FALSE";
        if (dayTypeId.HasValue) sql += " AND day_type_id = @DayTypeId";
        sql += " ORDER BY day_type_id, range_a";
        return await conn.QueryAsync<AttendanceFeeCalculator>(sql, new { DayTypeId = dayTypeId });
    }

    public async Task<long> AddFeeCalculatorAsync(AttendanceFeeCalculator entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_fee_calculators (day_type_id, range_a, range_b, range_price, created_at, created_by, updated_at, updated_by)
                    VALUES (@DayTypeId, @RangeA, @RangeB, @RangePrice, NOW(), @CreatedBy, NOW(), @UpdatedBy) RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateFeeCalculatorAsync(AttendanceFeeCalculator entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_fee_calculators SET day_type_id=@DayTypeId, range_a=@RangeA, range_b=@RangeB, range_price=@RangePrice, updated_at=NOW(), updated_by=@UpdatedBy WHERE id=@Id", entity) > 0;
    }

    public async Task<bool> DeleteFeeCalculatorAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync("UPDATE attendance_fee_calculators SET is_deleted=TRUE WHERE id=@Id", new { Id = id }) > 0;
    }

    public async Task<int> SaveHwattEmployeesAsync(IEnumerable<HwattEmployeeDto> employees)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_hwatt_employees (employee_id, employee_name, status, created_at, synced_at)
                    VALUES (@EmployeeId, @EmployeeName, @Status, NOW(), NOW())
                    ON CONFLICT (employee_id) DO UPDATE SET
                        employee_name = EXCLUDED.employee_name,
                        status = EXCLUDED.status,
                        synced_at = NOW()";
        return await conn.ExecuteAsync(sql, employees);
    }

    public async Task<IEnumerable<HwattEmployeeDto>> GetHwattEmployeesFromCacheAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT employee_id AS EmployeeId, employee_name AS EmployeeName, status AS Status, synced_at AS SyncedAt
                    FROM attendance_hwatt_employees WHERE is_deleted = FALSE AND status = 1 ORDER BY employee_id";
        return await conn.QueryAsync<HwattEmployeeDto>(sql);
    }

    public async Task<IEnumerable<HwattCardRecordDto>> GetCardRecordsFromCacheAsync(long employeeId, DateTime date)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT employee_id AS EmployeeId, card_time AS CardTime, card_type_id AS CardTypeId, dev_id AS DevId
                    FROM attendance_hwatt_card_records
                    WHERE employee_id = @EmployeeId AND card_date = @Date::date AND is_deleted = FALSE
                    ORDER BY card_time";
        return await conn.QueryAsync<HwattCardRecordDto>(sql, new { EmployeeId = employeeId, Date = date });
    }

    public async Task<IEnumerable<MergedCardRecordDto>> GetMergedCardRecordsAsync(long employeeId, DateTime date)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"
            WITH emp_ids AS (
                SELECT system_employee_id, employee_name FROM attendance_employees
                WHERE employee_id = @EmployeeId AND is_deleted = FALSE
                LIMIT 1
            ),
            matching_emps AS (
                SELECT employee_id, hwatt_employee_id, source, employee_name FROM attendance_employees
                WHERE is_deleted = FALSE
                  AND (
                      system_employee_id = (SELECT system_employee_id FROM emp_ids)
                      OR (
                          (SELECT system_employee_id FROM emp_ids) IS NULL
                          AND employee_name = (SELECT employee_name FROM emp_ids)
                      )
                  )
            )
            SELECT cr.card_time AS CardTime FROM attendance_hwatt_card_records cr
            INNER JOIN matching_emps e ON cr.employee_id = e.employee_id
            WHERE e.source = 'hwatt'
              AND cr.card_date = @Date::date AND cr.is_deleted = FALSE
            UNION ALL
            SELECT cr2.check_time AS CardTime FROM attendance_dingtalk_card_records cr2
            INNER JOIN matching_emps e ON
                (cr2.user_id ~ '^\d+$' AND e.hwatt_employee_id = cr2.user_id::bigint)
                OR (cr2.user_name = e.employee_name AND e.hwatt_employee_id IS NULL)
            WHERE e.source = 'dingtalk'
              AND cr2.work_date = @Date::date AND cr2.is_deleted = FALSE
            ORDER BY CardTime";
        return await conn.QueryAsync<MergedCardRecordDto>(sql, new { EmployeeId = employeeId, Date = date });
    }

    public async Task<IEnumerable<HwattCardRecordDto>> GetCardRecordsByDateRangeAsync(long employeeId, DateTime startDate, DateTime endDate)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT employee_id AS EmployeeId, card_time AS CardTime, card_type_id AS CardTypeId, dev_id AS DevId
                    FROM attendance_hwatt_card_records
                    WHERE employee_id = @EmployeeId AND card_date >= @StartDate AND card_date <= @EndDate AND is_deleted = FALSE
                    ORDER BY card_time";
        return await conn.QueryAsync<HwattCardRecordDto>(sql, new { EmployeeId = employeeId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<HwattEmployeeDto?> GetHwattEmployeeByIdAsync(long employeeId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT employee_id AS EmployeeId, employee_name AS EmployeeName, status AS Status, synced_at AS SyncedAt
                    FROM attendance_hwatt_employees
                    WHERE employee_id = @EmployeeId AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<HwattEmployeeDto>(sql, new { EmployeeId = employeeId });
    }

    // ========== 钉钉数据缓存表操作 ==========

    public async Task<int> SaveDingTalkEmployeesAsync(IEnumerable<DingTalkEmployeeDto> employees)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_dingtalk_employees (user_id, user_name, department_id, department_name, status, created_at, synced_at)
                    VALUES (@UserId, @UserName, @DepartmentId, @DepartmentName, @Status, NOW(), NOW())
                    ON CONFLICT (user_id) DO UPDATE SET
                        user_name = EXCLUDED.user_name,
                        department_id = EXCLUDED.department_id,
                        department_name = EXCLUDED.department_name,
                        status = EXCLUDED.status,
                        synced_at = NOW()";
        return await conn.ExecuteAsync(sql, employees);
    }

    public async Task<IEnumerable<DingTalkEmployeeDto>> GetDingTalkEmployeesFromCacheAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT user_id AS UserId, user_name AS UserName, department_id AS DepartmentId,
                           department_name AS DepartmentName, status AS Status
                    FROM attendance_dingtalk_employees WHERE is_deleted = FALSE AND status = 1 ORDER BY user_id";
        return await conn.QueryAsync<DingTalkEmployeeDto>(sql);
    }

    public async Task<int> SaveDingTalkCardRecordsAsync(IEnumerable<DingTalkCardRecordDto> records)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO attendance_dingtalk_card_records (user_id, user_name, work_date, check_time, check_type, time_result, base_check_time, source_type, created_at, synced_at)
                    VALUES (@UserId, @UserName, @WorkDate::date, @CheckTime, @CheckType, @TimeResult, @BaseCheckTime, 'dingtalk', NOW(), NOW())
                    ON CONFLICT (user_id, check_time) DO NOTHING";
        return await conn.ExecuteAsync(sql, records);
    }

    public async Task<IEnumerable<DingTalkCardRecordDto>> GetDingTalkCardRecordsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT user_id AS UserId, user_name AS UserName, work_date AS WorkDate,
                           check_time AS CheckTime, check_type AS CheckType, time_result AS TimeResult,
                           base_check_time AS BaseCheckTime
                    FROM attendance_dingtalk_card_records
                    WHERE work_date >= @StartDate::date AND work_date <= @EndDate::date AND is_deleted = FALSE
                    ORDER BY user_id, check_time";
        return await conn.QueryAsync<DingTalkCardRecordDto>(sql, new { StartDate = startDate, EndDate = endDate });
    }

    public async Task<IEnumerable<DingTalkCardRecordDto>> GetDingTalkCardRecordsByEmployeeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT user_id AS UserId, user_name AS UserName, work_date AS WorkDate,
                           check_time AS CheckTime, check_type AS CheckType, time_result AS TimeResult,
                           base_check_time AS BaseCheckTime
                    FROM attendance_dingtalk_card_records
                    WHERE user_id = @UserId AND work_date >= @StartDate::date AND work_date <= @EndDate::date AND is_deleted = FALSE
                    ORDER BY work_date, check_time";
        return await conn.QueryAsync<DingTalkCardRecordDto>(sql, new { UserId = userId, StartDate = startDate, EndDate = endDate });
    }

    public async Task<(IEnumerable<CardRecordDto> Items, int TotalCount)> GetCardRecordsAsync(
        string? employeeName, DateTime? startDate, DateTime? endDate, int page, int pageSize)
    {
        using var conn = _connectionFactory.CreateConnection();

        var where = "WHERE cr.is_deleted = FALSE";
        if (!string.IsNullOrEmpty(employeeName))
            where += " AND e.employee_name ILIKE @EmployeeName";
        if (startDate.HasValue)
            where += " AND cr.card_date >= @StartDate";
        if (endDate.HasValue)
            where += " AND cr.card_date <= @EndDate";

        var dtWhere = "WHERE cr2.is_deleted = FALSE";
        if (startDate.HasValue)
            dtWhere += " AND cr2.work_date >= @StartDate";
        if (endDate.HasValue)
            dtWhere += " AND cr2.work_date <= @EndDate";
        if (!string.IsNullOrEmpty(employeeName))
            dtWhere += " AND COALESCE(de.user_name, cr2.user_name) ILIKE @EmployeeName";

        var countSql = $@"SELECT COUNT(*) FROM (
            SELECT cr.employee_id FROM attendance_hwatt_card_records cr
            LEFT JOIN attendance_employees e ON cr.employee_id = e.employee_id AND e.is_deleted = FALSE
            {where}
            UNION ALL
            SELECT 1 FROM attendance_dingtalk_card_records cr2
            LEFT JOIN attendance_dingtalk_employees de ON cr2.user_id = de.user_id AND de.is_deleted = FALSE
            {dtWhere}
        ) t";

        var total = await conn.ExecuteScalarAsync<int>(countSql, new
        {
            EmployeeName = string.IsNullOrEmpty(employeeName) ? null : $"%{employeeName}%",
            StartDate = startDate,
            EndDate = endDate
        });

        var offset = (page - 1) * pageSize;

        var sql = $@"
            SELECT COALESCE(e.employee_name, he.employee_name, cr.employee_id::varchar) AS EmployeeName, cr.employee_id::varchar AS EmployeeId,
                   cr.card_time AS CardTime, 'hwatt' AS Source,
                   cr.card_type_id::varchar AS CardType, cr.dev_id::varchar AS DeviceInfo,
                   cr.created_at AS CreatedAt
            FROM attendance_hwatt_card_records cr
            LEFT JOIN attendance_employees e ON cr.employee_id = e.employee_id AND e.is_deleted = FALSE
            LEFT JOIN attendance_hwatt_employees he ON cr.employee_id = he.employee_id AND he.is_deleted = FALSE
            {where}
            UNION ALL
            SELECT COALESCE(de.user_name, cr2.user_name) AS EmployeeName, cr2.user_id AS EmployeeId,
                   cr2.check_time AS CardTime, 'dingtalk' AS Source,
                   cr2.check_type || '/' || cr2.time_result AS CardType,
                   cr2.base_check_time::varchar AS DeviceInfo, cr2.created_at AS CreatedAt
            FROM attendance_dingtalk_card_records cr2
            LEFT JOIN attendance_dingtalk_employees de ON cr2.user_id = de.user_id AND de.is_deleted = FALSE
            {dtWhere}
            ORDER BY CardTime DESC
            LIMIT @PageSize OFFSET @Offset";

        var items = await conn.QueryAsync<CardRecordDto>(sql, new
        {
            EmployeeName = string.IsNullOrEmpty(employeeName) ? null : $"%{employeeName}%",
            StartDate = startDate,
            EndDate = endDate,
            PageSize = pageSize,
            Offset = offset
        });

        return (items, total);
    }
}