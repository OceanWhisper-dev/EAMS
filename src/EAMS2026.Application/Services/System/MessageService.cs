using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<MessageService> _logger;

    public MessageService(IMessageRepository messageRepository, ILogger<MessageService> logger)
    {
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<(IEnumerable<Message> Messages, int Total)> GetReceivedAsync(long userId, int page = 1, int pageSize = 20)
    {
        var messages = await _messageRepository.GetReceivedAsync(userId, page, pageSize);
        var unreadCount = await _messageRepository.GetUnreadCountAsync(userId);
        return (messages, unreadCount);
    }

    public async Task<(IEnumerable<Message> Messages, int Total)> GetSentAsync(long userId, int page = 1, int pageSize = 20)
    {
        var messages = await _messageRepository.GetSentAsync(userId, page, pageSize);
        return (messages, 0);
    }

    public async Task<Message?> GetByIdAsync(long id)
    {
        return await _messageRepository.GetByIdAsync(id);
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        return await _messageRepository.GetUnreadCountAsync(userId);
    }

    public async Task<IEnumerable<Message>> GetConversationAsync(long messageId)
    {
        return await _messageRepository.GetConversationAsync(messageId);
    }

    public async Task<(bool Success, string Message)> SendAsync(Message message, long userId)
    {
        if (string.IsNullOrWhiteSpace(message.Title))
            return (false, "消息标题不能为空");

        if (message.ReceiverId <= 0)
            return (false, "接收者不能为空");

        message.SenderId = userId;
        message.CreatedBy = userId;
        message.UpdatedBy = userId;
        message.Type = string.IsNullOrEmpty(message.Type) ? "personal" : message.Type;
        message.Priority = string.IsNullOrEmpty(message.Priority) ? "normal" : message.Priority;

        await _messageRepository.AddAsync(message);
        return (true, "发送成功");
    }

    public async Task<(bool Success, string Message)> ReplyAsync(long parentId, string content, long userId, string userName)
    {
        var parent = await _messageRepository.GetByIdAsync(parentId);
        if (parent == null)
            return (false, "原消息不存在");

        var reply = new Message
        {
            Title = $"回复: {parent.Title}",
            Content = content,
            SenderId = userId,
            SenderName = userName,
            ReceiverId = parent.SenderId,
            ReceiverName = parent.SenderName,
            ParentId = parentId,
            Type = parent.Type,
            Priority = parent.Priority,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        await _messageRepository.AddAsync(reply);
        return (true, "回复成功");
    }

    public async Task<(bool Success, string Message)> MarkAsReadAsync(long messageId, long userId)
    {
        var success = await _messageRepository.MarkAsReadAsync(messageId, userId);
        return success ? (true, "已标记为已读") : (false, "操作失败");
    }

    public async Task<(bool Success, string Message)> MarkAllAsReadAsync(long userId)
    {
        await _messageRepository.MarkAllAsReadAsync(userId);
        return (true, "全部标记为已读");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long id, long userId)
    {
        var msg = await _messageRepository.GetByIdAsync(id);
        if (msg == null)
            return (false, "消息不存在");

        if (msg.ReceiverId != userId && msg.SenderId != userId)
            return (false, "无权删除此消息");

        await _messageRepository.DeleteAsync(id);
        return (true, "删除成功");
    }
}