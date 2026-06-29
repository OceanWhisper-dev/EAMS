namespace EAMS2026.Domain.Entities.System;

public class OperationLog : BaseEntity
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    public string? Description { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}