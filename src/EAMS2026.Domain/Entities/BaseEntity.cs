namespace EAMS2026.Domain.Entities;

/// <summary>
/// 实体基类。
/// 所有领域实体均继承此类，提供统一的审计字段：
/// - Id: 自增主键（BIGINT）
/// - IsDeleted: 软删除标志（false=正常, true=已删除）
/// - CreatedAt/CreatedBy: 创建时间和创建人ID
/// - UpdatedAt/UpdatedBy: 最后更新时间和更新人ID
/// </summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public long UpdatedBy { get; set; }
}