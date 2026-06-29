using Dapper;
using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Attendance;
using EAMS2026.Domain.Interfaces.Repositories;
using EAMS2026.Domain.Interfaces.Repositories.Attendance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EAMS2026.Infrastructure.Data;

public class HwattImportService : IHwattImportService
{
    private readonly HwattConnectionFactory _hwattFactory;
    private readonly ILogger<HwattImportService> _logger;
    private readonly HwattImportOptions _options;

    public HwattImportService(HwattConnectionFactory hwattFactory, ILogger<HwattImportService> logger,
        IOptions<HwattImportOptions> options)
    {
        _hwattFactory = hwattFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<List<(long EmployeeId, string EmployeeName)>> GetHwattEmployeesAsync()
    {
        using var conn = _hwattFactory.CreateConnection();
        var sql = "SELECT EmployeeID, EmployeeName FROM KQZ_Employee WHERE StaffStatus = 0 AND BrchID = @BrchId";
        var rows = await conn.QueryAsync(sql, new { BrchId = _options.BrchId });
        return rows.Select(r => ((long)r.EmployeeID, (string)r.EmployeeName)).ToList();
    }

    public async Task<(TimeSpan? cardBegin, TimeSpan? cardEnd)> GetCardTimesAsync(long employeeId, DateTime date)
    {
        using var conn = _hwattFactory.CreateConnection();
        var sql = @"SELECT MIN(CardTime) AS CardBegin, MAX(CardTime) AS CardEnd
                    FROM KQZ_Card
                    WHERE EmployeeID = @EmployeeId
                      AND CAST(CardTime AS DATE) = @Date";

        var result = await conn.QueryFirstOrDefaultAsync(sql, new { EmployeeId = employeeId, Date = date.Date });

        if (result == null)
            return (null, null);

        DateTime? cardBegin = result.CardBegin;
        DateTime? cardEnd = result.CardEnd;

        return (
            cardBegin?.TimeOfDay,
            cardEnd?.TimeOfDay
        );
    }

    public async Task<List<HwattCardRecordDto>> GetHwattCardRecordsAsync(DateTime? startDate = null, DateTime? endDate = null, long? employeeId = null)
    {
        using var conn = _hwattFactory.CreateConnection();
        var sql = @"SELECT EmployeeID, CardTime, CardTypeID, DevID
                    FROM KQZ_Card
                    WHERE 1=1";

        if (startDate.HasValue)
            sql += " AND CardTime >= @StartDate";
        if (endDate.HasValue)
            sql += " AND CardTime <= @EndDate";
        if (employeeId.HasValue)
            sql += " AND EmployeeID = @EmployeeId";

        sql += " ORDER BY EmployeeID, CardTime";

        var rows = await conn.QueryAsync(sql, new { StartDate = startDate, EndDate = endDate, EmployeeId = employeeId });
        return rows.Select(r => new HwattCardRecordDto
        {
            EmployeeId = (long)r.EmployeeID,
            CardTime = r.CardTime,
            CardTypeId = r.CardTypeID as int?,
            DevId = r.DevID as long?
        }).ToList();
    }
}
