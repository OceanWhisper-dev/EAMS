namespace EAMS2026.Application.Common;

public class CardRecordSearchRequest
{
    public string? EmployeeName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AttendanceReportSearchRequest
{
    public long? EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class AttendanceRecordSearchRequest
{
    public long? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AttendanceImportRequest
{
    public long? EmployeeId { get; set; }
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class AttendanceAllEmployeeRequest
{
    public DateTime DoDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class AttendanceSyncEmployeesResponse
{
    public int SyncedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int TotalCount { get; set; }
}

public class AttendanceSyncCardRecordsResponse
{
    public int SyncedCount { get; set; }
}

public class AttendanceCardRecordsSyncRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

// ========== 钉钉数据导入 DTOs ==========

public class DingTalkSyncEmployeesResponse
{
    public int SyncedCount { get; set; }
    public int TotalCount { get; set; }
}

public class DingTalkSyncCardRecordsResponse
{
    public int SyncedCount { get; set; }
}

public class DingTalkCardRecordsSyncRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class DingTalkImportRecordRequest
{
    public string? UserId { get; set; }
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class DingTalkImportAllRequest
{
    public DateTime DoDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateEmployeeMappingRequest
{
    public long? SystemEmployeeId { get; set; }
}