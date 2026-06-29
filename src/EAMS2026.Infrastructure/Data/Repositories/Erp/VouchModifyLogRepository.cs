using Dapper;
using EAMS2026.Domain.Common;
using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Entities.Erp;
using EAMS2026.Domain.Interfaces.Repositories;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.Erp;

public class VouchModifyLogRepository : IVouchModifyLogRepository
{
    private readonly DbConnectionFactory _db;

    public VouchModifyLogRepository(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<long> AddAsync(VouchModifyLog log)
    {
        using var conn = _db.CreateConnection();
        var sql = @"INSERT INTO erp_vouch_modify_logs (vouch_type, vouch_id, vouch_code, field_name, old_value, new_value, operator_id, operator_name, status, error_msg)
                    VALUES (@VouchType, @VouchId, @VouchCode, @FieldName, @OldValue, @NewValue, @OperatorId, @OperatorName, @Status, @ErrorMsg)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, log);
    }

    public async Task<PagedResult<VouchModifyLog>> QueryAsync(string? vouchType = null, long? operatorId = null,
        DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20)
    {
        using var conn = _db.CreateConnection();
        var dp = new DynamicParameters();
        var where = new List<string>();

        if (!string.IsNullOrEmpty(vouchType)) { where.Add("vouch_type = @VouchType"); dp.Add("VouchType", vouchType); }
        if (operatorId.HasValue) { where.Add("operator_id = @OperatorId"); dp.Add("OperatorId", operatorId.Value); }
        if (from.HasValue) { where.Add("operate_at >= @From"); dp.Add("From", from.Value); }
        if (to.HasValue) { where.Add("operate_at <= @To"); dp.Add("To", to.Value); }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        var countSql = $"SELECT COUNT(1) FROM erp_vouch_modify_logs {whereClause}";
        var total = await conn.ExecuteScalarAsync<int>(countSql, dp);

        dp.Add("Offset", (page - 1) * pageSize);
        dp.Add("PageSize", pageSize);
        var dataSql = $@"SELECT * FROM erp_vouch_modify_logs {whereClause}
                         ORDER BY operate_at DESC
                         OFFSET @Offset LIMIT @PageSize";
        var items = await conn.QueryAsync<VouchModifyLog>(dataSql, dp);

        return new PagedResult<VouchModifyLog>(items, total, page, pageSize);
    }

    public async Task<List<SalespersonDto>> GetMappedSalespersonsAsync()
    {
        using var conn = _db.CreateConnection();
        var rows = await conn.QueryAsync<(string Code, string Name)>(
            @"SELECT DISTINCT sp.salesperson_code, sp.salesperson_name
              FROM erp_settings_salesperson_map sp
              WHERE sp.salesperson_code IS NOT NULL AND sp.salesperson_name IS NOT NULL
              ORDER BY sp.salesperson_code");
        return rows.Select(r => new SalespersonDto { Code = r.Code, Name = r.Name }).ToList();
    }
}