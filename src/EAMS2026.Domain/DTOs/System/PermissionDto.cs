using EAMS2026.Domain.Enums;

namespace EAMS2026.Domain.DTOs.System;

public class PermissionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public PermissionType Type { get; set; } = PermissionType.Menu;
    public long? ParentId { get; set; }
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public ICollection<PermissionDto> Children { get; set; } = new List<PermissionDto>();
}