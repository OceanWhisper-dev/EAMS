using EAMS2026.Domain.Enums;

namespace EAMS2026.Domain.Entities.System;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public PermissionType Type { get; set; } = PermissionType.Menu;
    public long? ParentId { get; set; }
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }

    public Permission? Parent { get; set; }
    public ICollection<Permission> Children { get; set; } = new List<Permission>();
}