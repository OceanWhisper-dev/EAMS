using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IMessageService
{
    Task<(IEnumerable<Message> Messages, int Total)> GetReceivedAsync(long userId, int page = 1, int pageSize = 20);
    Task<(IEnumerable<Message> Messages, int Total)> GetSentAsync(long userId, int page = 1, int pageSize = 20);
    Task<Message?> GetByIdAsync(long id);
    Task<int> GetUnreadCountAsync(long userId);
    Task<IEnumerable<Message>> GetConversationAsync(long messageId);
    Task<(bool Success, string Message)> SendAsync(Message message, long userId);
    Task<(bool Success, string Message)> ReplyAsync(long parentId, string content, long userId, string userName);
    Task<(bool Success, string Message)> MarkAsReadAsync(long messageId, long userId);
    Task<(bool Success, string Message)> MarkAllAsReadAsync(long userId);
    Task<(bool Success, string Message)> DeleteAsync(long id, long userId);
}