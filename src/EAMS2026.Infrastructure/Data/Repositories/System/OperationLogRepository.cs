using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class OperationLogRepository : IOperationLogRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public OperationLogRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(OperationLog log)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_operation_logs (user_id, username, operation_type, module, entity_type, entity_id,
                    description, old_value, new_value, ip_address, created_at)
                    VALUES (@UserId, @Username, @OperationType, @Module, @EntityType, @EntityId,
                    @Description, CAST(@OldValue AS jsonb), CAST(@NewValue AS jsonb), @IpAddress, NOW())
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, log);
    }

    public async Task<OperationLog?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_operation_logs WHERE id = @Id";
        return await conn.QueryFirstOrDefaultAsync<OperationLog>(sql, new { Id = id });
    }

    public async Task<IEnumerable<OperationLog>> GetPagedAsync(int page, int pageSize, long? userId = null, string? module = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_operation_logs WHERE 1=1";
        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            sql += " AND user_id = @UserId";
            parameters.Add("UserId", userId.Value);
        }
        if (!string.IsNullOrEmpty(module))
        {
            sql += " AND module = @Module";
            parameters.Add("Module", module);
        }
        if (startDate.HasValue)
        {
            sql += " AND created_at >= @StartDate";
            parameters.Add("StartDate", startDate.Value);
        }
        if (endDate.HasValue)
        {
            sql += " AND created_at <= @EndDate";
            parameters.Add("EndDate", endDate.Value);
        }

        sql += " ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        return await conn.QueryAsync<OperationLog>(sql, parameters);
    }

    public async Task<int> GetCountAsync(long? userId = null, string? module = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT COUNT(1) FROM sys_operation_logs WHERE 1=1";
        var parameters = new DynamicParameters();

        if (userId.HasValue)
        {
            sql += " AND user_id = @UserId";
            parameters.Add("UserId", userId.Value);
        }
        if (!string.IsNullOrEmpty(module))
        {
            sql += " AND module = @Module";
            parameters.Add("Module", module);
        }
        if (startDate.HasValue)
        {
            sql += " AND created_at >= @StartDate";
            parameters.Add("StartDate", startDate.Value);
        }
        if (endDate.HasValue)
        {
            sql += " AND created_at <= @EndDate";
            parameters.Add("EndDate", endDate.Value);
        }

        return await conn.ExecuteScalarAsync<int>(sql, parameters);
    }

    public async Task<int> ClearAsync(DateTime? beforeDate = null, long? userId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "DELETE FROM sys_operation_logs WHERE 1=1";
        var parameters = new DynamicParameters();

        if (beforeDate.HasValue)
        {
            sql += " AND created_at < @BeforeDate";
            parameters.Add("BeforeDate", beforeDate.Value);
        }
        if (userId.HasValue)
        {
            sql += " AND user_id = @UserId";
            parameters.Add("UserId", userId.Value);
        }

        return await conn.ExecuteAsync(sql, parameters);
    }
}