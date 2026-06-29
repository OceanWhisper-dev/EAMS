namespace EAMS2026.Domain.Entities.System;

public class DictItem : BaseEntity
{
    public long DictTypeId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool Status { get; set; } = true;

    public DictType? DictType { get; set; }
}