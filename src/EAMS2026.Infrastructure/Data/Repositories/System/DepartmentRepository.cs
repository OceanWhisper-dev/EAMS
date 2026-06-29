using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DepartmentRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(Department entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_departments (name, code, parent_id, sort_order, status, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@Name, @Code, @ParentId, @SortOrder, @Status, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<IEnumerable<Department>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_departments WHERE is_deleted = FALSE ORDER BY sort_order, name";
        return await conn.QueryAsync<Department>(sql);
    }

    public async Task<Department?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_departments WHERE id = @Id AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Department>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Department>> GetChildrenAsync(long parentId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_departments WHERE parent_id = @ParentId AND is_deleted = FALSE ORDER BY sort_order, name";
        return await conn.QueryAsync<Department>(sql, new { ParentId = parentId });
    }

    public async Task<IEnumerable<Department>> GetTreeAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_departments WHERE is_deleted = FALSE ORDER BY sort_order, name";
        var all = await conn.QueryAsync<Department>(sql);
        return BuildTree(all.ToList());
    }

    public async Task<bool> HasChildrenAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT COUNT(1) FROM sys_departments WHERE parent_id = @Id AND is_deleted = FALSE";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    public async Task<bool> IsCodeExistsAsync(string code, long? excludeId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (excludeId.HasValue)
        {
            var sql = "SELECT COUNT(1) FROM sys_departments WHERE code = @Code AND is_deleted = FALSE AND id != @ExcludeId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId.Value });
            return count > 0;
        }
        else
        {
            var sql = "SELECT COUNT(1) FROM sys_departments WHERE code = @Code AND is_deleted = FALSE";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code });
            return count > 0;
        }
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_departments SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateAsync(Department entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_departments SET name = @Name, code = @Code, parent_id = @ParentId,
                    sort_order = @SortOrder, status = @Status, updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<Department?> GetByCodeAsync(string code)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_departments WHERE code = @Code AND is_deleted = FALSE LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Department>(sql, new { Code = code });
    }

    private static List<Department> BuildTree(List<Department> all)
    {
        var lookup = all.ToDictionary(x => x.Id);
        var roots = new List<Department>();

        foreach (var item in all)
        {
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
            {
                parent.Children ??= new List<Department>();
                parent.Children.Add(item);
            }
            else
            {
                roots.Add(item);
            }
        }

        return roots;
    }
}