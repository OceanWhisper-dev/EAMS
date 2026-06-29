namespace EAMS2026.Domain.Entities.System;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool Status { get; set; } = true;

    public Department? Parent { get; set; }
    public ICollection<Department> Children { get; set; } = new List<Department>();
}