using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class RoleRepository : IRoleRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public RoleRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(Role entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_roles (name, code, description, status, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@Name, @Code, @Description, @Status, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> AssignPermissionsAsync(long roleId, IEnumerable<long> permissionIds)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync("DELETE FROM sys_role_permissions WHERE role_id = @RoleId", new { RoleId = roleId }, tx);
            foreach (var permissionId in permissionIds)
            {
                await conn.ExecuteAsync("INSERT INTO sys_role_permissions (role_id, permission_id) VALUES (@RoleId, @PermissionId)",
                    new { RoleId = roleId, PermissionId = permissionId }, tx);
            }
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_roles WHERE is_deleted = FALSE ORDER BY name";
        return await conn.QueryAsync<Role>(sql);
    }

    public async Task<IEnumerable<Role>> GetByCodesAsync(IEnumerable<string> codes)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_roles WHERE code = ANY(@Codes) AND is_deleted = FALSE";
        return await conn.QueryAsync<Role>(sql, new { Codes = codes.ToArray() });
    }

    public async Task<Role?> GetByCodeAsync(string code)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_roles WHERE code = @Code AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Role>(sql, new { Code = code });
    }

    public async Task<Role?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_roles WHERE id = @Id AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Role>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(long roleId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT p.* FROM sys_permissions p
                    INNER JOIN sys_role_permissions rp ON p.id = rp.permission_id
                    WHERE rp.role_id = @RoleId AND p.is_deleted = FALSE";
        return await conn.QueryAsync<Permission>(sql, new { RoleId = roleId });
    }

    public async Task<bool> IsCodeExistsAsync(string code, long? excludeId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (excludeId.HasValue)
        {
            var sql = "SELECT COUNT(1) FROM sys_roles WHERE code = @Code AND is_deleted = FALSE AND id != @ExcludeId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId.Value });
            return count > 0;
        }
        else
        {
            var sql = "SELECT COUNT(1) FROM sys_roles WHERE code = @Code AND is_deleted = FALSE";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code });
            return count > 0;
        }
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_roles SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateAsync(Role entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_roles SET name = @Name, code = @Code, description = @Description,
                    status = @Status, updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<IEnumerable<User>> GetRoleUsersAsync(long roleId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT u.*, e.name AS employee_name
                    FROM sys_users u
                    INNER JOIN sys_user_roles ur ON u.id = ur.user_id
                    LEFT JOIN sys_employees e ON u.employee_id = e.id
                    WHERE ur.role_id = @RoleId AND u.is_deleted = FALSE";
        return await conn.QueryAsync<User>(sql, new { RoleId = roleId });
    }

    public async Task<bool> AssignUsersToRoleAsync(long roleId, IEnumerable<long> userIds)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync("DELETE FROM sys_user_roles WHERE role_id = @RoleId", new { RoleId = roleId }, tx);
            foreach (var userId in userIds)
            {
                await conn.ExecuteAsync("INSERT INTO sys_user_roles (user_id, role_id) VALUES (@UserId, @RoleId)",
                    new { UserId = userId, RoleId = roleId }, tx);
            }
            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<Role>> GetDeletedAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_roles WHERE is_deleted = TRUE ORDER BY updated_at DESC";
        return await conn.QueryAsync<Role>(sql);
    }

    public async Task<bool> HardDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM sys_role_permissions WHERE role_id = @Id", new { Id = id });
        await conn.ExecuteAsync("DELETE FROM sys_user_roles WHERE role_id = @Id", new { Id = id });
        return await conn.ExecuteAsync("DELETE FROM sys_roles WHERE id = @Id", new { Id = id }) > 0;
    }
}