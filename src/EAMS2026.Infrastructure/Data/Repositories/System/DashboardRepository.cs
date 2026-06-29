using Dapper;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class DashboardRepository : IDashboardRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DashboardRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetDepartmentCountAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM sys_departments WHERE is_deleted = FALSE");
    }

    public async Task<int> GetEmployeeCountAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM sys_employees WHERE is_deleted = FALSE");
    }

    public async Task<int> GetUserCountAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM sys_users WHERE is_deleted = FALSE");
    }

    public async Task<int> GetRoleCountAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM sys_roles WHERE is_deleted = FALSE");
    }
}