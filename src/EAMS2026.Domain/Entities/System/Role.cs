namespace EAMS2026.Domain.Entities.System;

/// <summary>
/// 角色实体（RBAC模型核心）。
/// 角色是权限的集合体，用户通过角色获得相应的权限。
///
/// 特殊编码：
/// - "super_admin" 编码为系统保留的超级管理员角色。
///   拥有此角色的用户绕过所有权限检查，拥有系统全部功能访问权。
///   前端 isAdmin 计算属性和后端 PermissionHandler 均对此编码做特殊处理。
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Status { get; set; } = true;

    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}