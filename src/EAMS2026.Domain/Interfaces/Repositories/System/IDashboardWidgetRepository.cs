using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IDashboardWidgetRepository
{
    Task<IEnumerable<DashboardWidget>> GetAllAsync();
    Task<DashboardWidget?> GetByKeyAsync(string widgetKey);
    Task<IEnumerable<RoleDashboardConfig>> GetRoleConfigsAsync(long roleId);
    Task SaveRoleConfigAsync(long roleId, IEnumerable<RoleDashboardConfig> configs);
    Task<int> DeleteRoleConfigsAsync(long roleId);
    Task<long> AddWidgetAsync(DashboardWidget widget);
    Task<bool> UpdateWidgetAsync(DashboardWidget widget);
    Task<bool> DeleteWidgetAsync(long id);
}