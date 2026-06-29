using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class PermissionRepository : IPermissionRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public PermissionRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(Permission entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_permissions (name, code, type, parent_id, path, icon, sort_order, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@Name, @Code, @Type, @ParentId, @Path, @Icon, @SortOrder, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<IEnumerable<Permission>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_permissions WHERE is_deleted = FALSE ORDER BY sort_order, name";
        return await conn.QueryAsync<Permission>(sql);
    }

    public async Task<Permission?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_permissions WHERE id = @Id AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Permission>(sql, new { Id = id });
    }

    public async Task<Permission?> GetByCodeAsync(string code)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_permissions WHERE code = @Code AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Permission>(sql, new { Code = code });
    }

    public async Task<IEnumerable<Permission>> GetByRoleIdAsync(long roleId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT p.* FROM sys_permissions p
                    INNER JOIN sys_role_permissions rp ON p.id = rp.permission_id
                    WHERE rp.role_id = @RoleId AND p.is_deleted = FALSE";
        return await conn.QueryAsync<Permission>(sql, new { RoleId = roleId });
    }

    public async Task<IEnumerable<Permission>> GetByUserIdAsync(long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT DISTINCT p.* FROM sys_permissions p
                    INNER JOIN sys_role_permissions rp ON p.id = rp.permission_id
                    INNER JOIN sys_user_roles ur ON rp.role_id = ur.role_id
                    WHERE ur.user_id = @UserId AND p.is_deleted = FALSE";
        return await conn.QueryAsync<Permission>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Permission>> GetTreeAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_permissions WHERE is_deleted = FALSE ORDER BY sort_order, name";
        var all = await conn.QueryAsync<Permission>(sql);
        var lookup = all.ToDictionary(x => x.Id);
        var roots = new List<Permission>();
        foreach (var item in all)
        {
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
            {
                parent.Children ??= new List<Permission>();
                parent.Children.Add(item);
            }
            else
            {
                roots.Add(item);
            }
        }
        return roots;
    }

    public async Task<bool> IsCodeExistsAsync(string code, long? excludeId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (excludeId.HasValue)
        {
            var sql = "SELECT COUNT(1) FROM sys_permissions WHERE code = @Code AND is_deleted = FALSE AND id != @ExcludeId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId.Value });
            return count > 0;
        }
        else
        {
            var sql = "SELECT COUNT(1) FROM sys_permissions WHERE code = @Code AND is_deleted = FALSE";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code });
            return count > 0;
        }
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_permissions SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateAsync(Permission entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_permissions SET name = @Name, code = @Code, type = @Type,
                    parent_id = @ParentId, path = @Path, icon = @Icon, sort_order = @SortOrder,
                    updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }
}