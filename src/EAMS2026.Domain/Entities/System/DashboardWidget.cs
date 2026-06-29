namespace EAMS2026.Domain.Entities.System;

public class DashboardWidget : BaseEntity
{
    public string WidgetKey { get; set; } = string.Empty;
    public string WidgetName { get; set; } = string.Empty;
    public string WidgetType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? DefaultConfig { get; set; }
    public string DataSourceType { get; set; } = "sql";
    public string? DataSourceConfig { get; set; }
    public string? LayoutConfig { get; set; }
    public int RefreshInterval { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RoleDashboardConfig : BaseEntity
{
    public long RoleId { get; set; }
    public string WidgetKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Config { get; set; }
    public int SortOrder { get; set; }
}