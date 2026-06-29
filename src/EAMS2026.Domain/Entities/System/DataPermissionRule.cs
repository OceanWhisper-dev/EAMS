using EAMS2026.Domain.Enums;

namespace EAMS2026.Domain.Entities.System;

public class DataPermissionRule : BaseEntity
{
    public long RoleId { get; set; }
    public string Module { get; set; } = string.Empty;
    public DataScope DataScope { get; set; } = DataScope.DEPARTMENT;

    public Role? Role { get; set; }
}