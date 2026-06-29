using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class DictTypeRepository : IDictTypeRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DictTypeRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(DictType entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_dict_types (name, code, description, status, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@Name, @Code, @Description, @Status, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<IEnumerable<DictType>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_dict_types WHERE is_deleted = FALSE ORDER BY name";
        return await conn.QueryAsync<DictType>(sql);
    }

    public async Task<DictType?> GetByCodeAsync(string code)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_dict_types WHERE code = @Code AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<DictType>(sql, new { Code = code });
    }

    public async Task<DictType?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_dict_types WHERE id = @Id AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<DictType>(sql, new { Id = id });
    }

    public async Task<bool> IsCodeExistsAsync(string code, long? excludeId = null)
    {
        using var conn = _connectionFactory.CreateConnection();
        if (excludeId.HasValue)
        {
            var sql = "SELECT COUNT(1) FROM sys_dict_types WHERE code = @Code AND is_deleted = FALSE AND id != @ExcludeId";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code, ExcludeId = excludeId.Value });
            return count > 0;
        }
        else
        {
            var sql = "SELECT COUNT(1) FROM sys_dict_types WHERE code = @Code AND is_deleted = FALSE";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { Code = code });
            return count > 0;
        }
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_dict_types SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateAsync(DictType entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_dict_types SET name = @Name, code = @Code, description = @Description,
                    status = @Status, updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }
}