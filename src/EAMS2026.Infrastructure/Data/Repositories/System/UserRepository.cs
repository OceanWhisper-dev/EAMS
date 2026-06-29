using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(User entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_users (username, password_hash, employee_id, status, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@Username, @PasswordHash, @EmployeeId, @Status, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> AssignRolesAsync(long userId, IEnumerable<long> roleIds)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            await conn.ExecuteAsync("DELETE FROM sys_user_roles WHERE user_id = @UserId", new { UserId = userId }, tx);
            foreach (var roleId in roleIds)
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

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT u.*, e.name AS employee_name
                    FROM sys_users u
                    LEFT JOIN sys_employees e ON u.employee_id = e.id
                    WHERE u.is_deleted = FALSE
                    ORDER BY u.username";
        return await conn.QueryAsync<User>(sql);
    }

    public async Task<IEnumerable<User>> GetAllWithRolesAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT u.*, e.name AS employee_name
                    FROM sys_users u
                    LEFT JOIN sys_employees e ON u.employee_id = e.id
                    WHERE u.is_deleted = FALSE
                    ORDER BY u.username";
        var users = (await conn.QueryAsync<User>(sql)).ToList();
        if (!users.Any()) return users;

        var userIds = users.Select(u => u.Id).ToArray();
        var roleSql = @"SELECT ur.user_id, r.id, r.name, r.code, r.description, r.status
                        FROM sys_user_roles ur
                        INNER JOIN sys_roles r ON ur.role_id = r.id
                        WHERE ur.user_id = ANY(@UserIds) AND r.is_deleted = FALSE";
        var roleData = await conn.QueryAsync(roleSql, new { UserIds = userIds });

        var roleDict = new Dictionary<long, List<Role>>();
        foreach (var row in roleData)
        {
            long uid = (long)row.user_id;
            if (!roleDict.ContainsKey(uid))
                roleDict[uid] = new List<Role>();
            roleDict[uid].Add(new Role
            {
                Id = row.id,
                Name = row.name,
                Code = row.code,
                Description = row.description,
                Status = row.status
            });
        }

        foreach (var user in users)
        {
            if (roleDict.TryGetValue(user.Id, out var roles))
                user.Roles = roles;
        }

        return users;
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM sys_users WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
        if (user != null)
        {
            var roles = await conn.QueryAsync<Role>(
                @"SELECT r.* FROM sys_roles r INNER JOIN sys_user_roles ur ON r.id = ur.role_id WHERE ur.user_id = @UserId AND r.is_deleted = FALSE",
                new { UserId = id });
            user.Roles = roles.ToList();
        }
        return user;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_users WHERE username = @Username AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT r.* FROM sys_roles r
                    INNER JOIN sys_user_roles ur ON r.id = ur.role_id
                    WHERE ur.user_id = @UserId AND r.is_deleted = FALSE AND r.status = TRUE";
        return await conn.QueryAsync<Role>(sql, new { UserId = userId });
    }

    public async Task<User?> GetWithRolesAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var user = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM sys_users WHERE id = @Id AND is_deleted = FALSE", new { Id = id });
        if (user != null)
        {
            var roles = await conn.QueryAsync<Role>(
                @"SELECT r.* FROM sys_roles r INNER JOIN sys_user_roles ur ON r.id = ur.role_id WHERE ur.user_id = @UserId AND r.is_deleted = FALSE",
                new { UserId = id });
            user.Roles = roles.ToList();
        }
        return user;
    }

    public async Task<bool> IsUsernameExistsAsync(string username, long? excludeId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (excludeId.HasValue)
        {
            var sql = "SELECT COUNT(1) FROM sys_users WHERE username = @Username AND is_deleted = FALSE AND id != @ExcludeId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Username = username, ExcludeId = excludeId.Value });
            return count > 0;
        }
        else
        {
            var sql = "SELECT COUNT(1) FROM sys_users WHERE username = @Username AND is_deleted = FALSE";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Username = username });
            return count > 0;
        }
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_users SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateAsync(User entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_users SET username = @Username, employee_id = @EmployeeId,
                    password_hash = @PasswordHash, status = @Status,
                    force_change_password = @ForceChangePassword,
                    updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task UpdateLastLoginAsync(long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_users SET last_login_at = NOW() WHERE id = @Id";
        await conn.ExecuteAsync(sql, new { Id = userId });
    }

    public async Task<IEnumerable<User>> GetDeletedAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT u.*, e.name AS employee_name
                    FROM sys_users u
                    LEFT JOIN sys_employees e ON u.employee_id = e.id
                    WHERE u.is_deleted = TRUE
                    ORDER BY u.updated_at DESC";
        return await conn.QueryAsync<User>(sql);
    }

    public async Task<bool> HardDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM sys_user_roles WHERE user_id = @Id", new { Id = id });
        return await conn.ExecuteAsync("DELETE FROM sys_users WHERE id = @Id", new { Id = id }) > 0;
    }
}