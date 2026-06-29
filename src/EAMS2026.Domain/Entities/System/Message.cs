namespace EAMS2026.Domain.Entities.System;

public class Message : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public long SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public long ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public long? ParentId { get; set; }
    public string Type { get; set; } = "personal";
    public string? Priority { get; set; } = "normal";

    public Message? Parent { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
}