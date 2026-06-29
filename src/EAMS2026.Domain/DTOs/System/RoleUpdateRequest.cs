using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.System;

public class RoleUpdateRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "角色ID无效")]
    public long Id { get; set; }
    [Required(ErrorMessage = "角色名称不能为空")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "角色编码不能为空")]
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Status { get; set; } = true;
}