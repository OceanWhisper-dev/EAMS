using System.ComponentModel.DataAnnotations;
using EAMS2026.Domain.Enums;

namespace EAMS2026.Domain.DTOs.System;

public class PermissionCreateRequest
{
    [Required(ErrorMessage = "权限名称不能为空")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "权限编码不能为空")]
    public string Code { get; set; } = string.Empty;
    public PermissionType Type { get; set; } = PermissionType.Menu;
    public long? ParentId { get; set; }
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}