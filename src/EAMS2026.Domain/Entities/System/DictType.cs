namespace EAMS2026.Domain.Entities.System;

public class DictType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Status { get; set; } = true;

    public ICollection<DictItem> Items { get; set; } = new List<DictItem>();
}