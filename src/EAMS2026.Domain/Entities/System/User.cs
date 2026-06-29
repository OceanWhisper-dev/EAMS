namespace EAMS2026.Domain.Entities.System;

/// <summary>
/// 系统用户实体。
/// 用户是登录系统的账户，通过角色关联权限（RBAC模型）。
///
/// 关键设计说明：
/// - PasswordHash 字段名具有误导性：在创建/更新用户时，Service 层传入的是明文密码，
///   在插入数据库前通过 BCrypt.HashPassword() 进行哈希处理后才覆盖此字段值。
///   这是为了减少 DTO 层而做的妥协，阅读代码时需注意此上下文。
/// - EmployeeId 关联员工信息，一个用户对应最多一个员工。
/// - Status 控制账户是否启用，禁用后无法登录。
/// - ForceChangePassword 为 true 时，用户登录后会强制跳转到修改密码页面。
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public long? EmployeeId { get; set; }
    public bool Status { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public bool ForceChangePassword { get; set; }

    public string? EmployeeName { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}