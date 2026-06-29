using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class DictItemRepository : IDictItemRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DictItemRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(DictItem entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_dict_items (dict_type_id, label, value, sort_order, status, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@DictTypeId, @Label, @Value, @SortOrder, @Status, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<IEnumerable<DictItem>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_dict_items WHERE is_deleted = FALSE ORDER BY sort_order";
        return await conn.QueryAsync<DictItem>(sql);
    }

    public async Task<DictItem?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_dict_items WHERE id = @Id AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<DictItem>(sql, new { Id = id });
    }

    public async Task<IEnumerable<DictItem>> GetByTypeCodeAsync(string code)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT di.* FROM sys_dict_items di
                    INNER JOIN sys_dict_types dt ON di.dict_type_id = dt.id
                    WHERE dt.code = @Code AND di.is_deleted = FALSE AND dt.is_deleted = FALSE AND di.status = TRUE
                    ORDER BY di.sort_order";
        return await conn.QueryAsync<DictItem>(sql, new { Code = code });
    }

    public async Task<DictItem?> GetByTypeCodeAndValueAsync(string code, string value)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT di.* FROM sys_dict_items di
                    INNER JOIN sys_dict_types dt ON di.dict_type_id = dt.id
                    WHERE dt.code = @Code AND di.value = @Value AND di.is_deleted = FALSE AND dt.is_deleted = FALSE
                    LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<DictItem>(sql, new { Code = code, Value = value });
    }

    public async Task<IEnumerable<DictItem>> GetByTypeIdAsync(long dictTypeId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_dict_items WHERE dict_type_id = @DictTypeId AND is_deleted = FALSE ORDER BY sort_order";
        return await conn.QueryAsync<DictItem>(sql, new { DictTypeId = dictTypeId });
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_dict_items SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> UpdateAsync(DictItem entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_dict_items SET label = @Label, value = @Value, sort_order = @SortOrder,
                    status = @Status, updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }
}