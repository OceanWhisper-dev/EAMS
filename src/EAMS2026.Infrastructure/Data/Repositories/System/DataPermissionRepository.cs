using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Enums;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class DataPermissionRepository : IDataPermissionRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DataPermissionRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DataPermissionRule?> GetByRoleAndModuleAsync(long roleId, string module)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT id, role_id AS RoleId, module, data_scope AS DataScope,
                           is_deleted AS IsDeleted, created_at AS CreatedAt, created_by AS CreatedBy,
                           updated_at AS UpdatedAt, updated_by AS UpdatedBy
                    FROM sys_data_permission_rules
                    WHERE role_id = @RoleId AND module = @Module AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<DataPermissionRule>(sql, new { RoleId = roleId, Module = module });
    }

    public async Task<IEnumerable<DataPermissionRule>> GetByModuleAsync(string module)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT dpr.id, dpr.role_id AS RoleId, dpr.module, dpr.data_scope AS DataScope,
                           dpr.is_deleted AS IsDeleted, dpr.created_at AS CreatedAt, dpr.created_by AS CreatedBy,
                           dpr.updated_at AS UpdatedAt, dpr.updated_by AS UpdatedBy,
                           r.name AS RoleName
                    FROM sys_data_permission_rules dpr
                    LEFT JOIN sys_roles r ON dpr.role_id = r.id AND r.is_deleted = FALSE
                    WHERE dpr.module = @Module AND dpr.is_deleted = FALSE
                    ORDER BY r.name";
        return await conn.QueryAsync<DataPermissionRule>(sql, new { Module = module });
    }

    public async Task<IEnumerable<DataPermissionRule>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT dpr.id, dpr.role_id AS RoleId, dpr.module, dpr.data_scope AS DataScope,
                           dpr.is_deleted AS IsDeleted, dpr.created_at AS CreatedAt, dpr.created_by AS CreatedBy,
                           dpr.updated_at AS UpdatedAt, dpr.updated_by AS UpdatedBy,
                           r.name AS RoleName
                    FROM sys_data_permission_rules dpr
                    LEFT JOIN sys_roles r ON dpr.role_id = r.id AND r.is_deleted = FALSE
                    WHERE dpr.is_deleted = FALSE
                    ORDER BY dpr.module, r.name";
        return await conn.QueryAsync<DataPermissionRule>(sql);
    }

    public async Task<bool> SaveAsync(DataPermissionRule rule)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_data_permission_rules (role_id, module, data_scope, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@RoleId, @Module, @DataScope, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    ON CONFLICT (role_id, module) WHERE is_deleted = FALSE
                    DO UPDATE SET data_scope = @DataScope, updated_at = NOW(), updated_by = @UpdatedBy";
        return await conn.ExecuteAsync(sql, rule) > 0;
    }

    public async Task<bool> DeleteAsync(long roleId, string module)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_data_permission_rules
                    SET is_deleted = TRUE, updated_at = NOW()
                    WHERE role_id = @RoleId AND module = @Module AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, new { RoleId = roleId, Module = module }) > 0;
    }
}