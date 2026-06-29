using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IMessageRepository : IBaseRepository<Message>
{
    Task<IEnumerable<Message>> GetReceivedAsync(long userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<Message>> GetSentAsync(long userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(long userId);
    Task<IEnumerable<Message>> GetConversationAsync(long messageId);
    Task<bool> MarkAsReadAsync(long messageId, long userId);
    Task<bool> MarkAllAsReadAsync(long userId);
}