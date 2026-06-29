using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class MessageRepository : IMessageRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public MessageRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> AddAsync(Message entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"INSERT INTO sys_messages (title, content, sender_id, sender_name, receiver_id, receiver_name,
                    is_read, read_at, parent_id, type, priority, is_deleted, created_at, created_by, updated_at, updated_by)
                    VALUES (@Title, @Content, @SenderId, @SenderName, @ReceiverId, @ReceiverName,
                    FALSE, NULL, @ParentId, @Type, @Priority, FALSE, NOW(), @CreatedBy, NOW(), @UpdatedBy)
                    RETURNING id";
        return await conn.ExecuteScalarAsync<long>(sql, entity);
    }

    public async Task<bool> UpdateAsync(Message entity)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"UPDATE sys_messages SET title = @Title, content = @Content, is_read = @IsRead,
                    read_at = @ReadAt, updated_at = NOW(), updated_by = @UpdatedBy
                    WHERE id = @Id AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, entity) > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await SoftDeleteAsync(id);
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_messages SET is_deleted = TRUE, updated_at = NOW() WHERE id = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<Message?> GetByIdAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_messages WHERE id = @Id AND is_deleted = FALSE";
        return await conn.QueryFirstOrDefaultAsync<Message>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Message>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM sys_messages WHERE is_deleted = FALSE ORDER BY created_at DESC";
        return await conn.QueryAsync<Message>(sql);
    }

    public async Task<IEnumerable<Message>> GetReceivedAsync(long userId, int page = 1, int pageSize = 20)
    {
        using var conn = _connectionFactory.CreateConnection();
        var offset = (page - 1) * pageSize;
        var sql = @"SELECT * FROM sys_messages WHERE receiver_id = @UserId AND is_deleted = FALSE
                    ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        return await conn.QueryAsync<Message>(sql, new { UserId = userId, PageSize = pageSize, Offset = offset });
    }

    public async Task<IEnumerable<Message>> GetSentAsync(long userId, int page = 1, int pageSize = 20)
    {
        using var conn = _connectionFactory.CreateConnection();
        var offset = (page - 1) * pageSize;
        var sql = @"SELECT * FROM sys_messages WHERE sender_id = @UserId AND is_deleted = FALSE
                    ORDER BY created_at DESC LIMIT @PageSize OFFSET @Offset";
        return await conn.QueryAsync<Message>(sql, new { UserId = userId, PageSize = pageSize, Offset = offset });
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "SELECT COUNT(1) FROM sys_messages WHERE receiver_id = @UserId AND is_read = FALSE AND is_deleted = FALSE";
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Message>> GetConversationAsync(long messageId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"WITH RECURSIVE conv AS (
                        SELECT * FROM sys_messages WHERE id = @MessageId AND is_deleted = FALSE
                        UNION ALL
                        SELECT m.* FROM sys_messages m JOIN conv ON m.id = conv.parent_id
                        WHERE m.is_deleted = FALSE
                    )
                    SELECT * FROM conv ORDER BY created_at ASC";
        return await conn.QueryAsync<Message>(sql, new { MessageId = messageId });
    }

    public async Task<bool> MarkAsReadAsync(long messageId, long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_messages SET is_read = TRUE, read_at = NOW() WHERE id = @Id AND receiver_id = @UserId AND is_deleted = FALSE";
        return await conn.ExecuteAsync(sql, new { Id = messageId, UserId = userId }) > 0;
    }

    public async Task<bool> MarkAllAsReadAsync(long userId)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = "UPDATE sys_messages SET is_read = TRUE, read_at = NOW() WHERE receiver_id = @UserId AND is_read = FALSE AND is_deleted = FALSE";
        await conn.ExecuteAsync(sql, new { UserId = userId });
        return true;
    }
}