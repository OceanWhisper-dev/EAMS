using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IDashboardConfigService
{
    Task<IEnumerable<Dictionary<string, object>>> GetAllWidgetsAsync();
    Task<IEnumerable<Dictionary<string, object>>> GetRoleDashboardConfigAsync(long roleId);
    Task SaveRoleDashboardConfigAsync(long roleId, List<Dictionary<string, object>> configs);
    Task<IEnumerable<Dictionary<string, object>>> GetUserDashboardAsync(long userId, IEnumerable<string> roleCodes);
    Task<long> AddWidgetAsync(DashboardWidget widget);
    Task<bool> UpdateWidgetAsync(DashboardWidget widget);
    Task<bool> DeleteWidgetAsync(long id);
    Task<object> PreviewWidgetDataAsync(string dataSourceType, string? dataSourceConfig);
}