using Dapper;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;

namespace EAMS2026.Infrastructure.Data.Repositories.System;

public class DashboardWidgetRepository : IDashboardWidgetRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DashboardWidgetRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DashboardWidget>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<DashboardWidget>(
            "SELECT * FROM sys_dashboard_widgets WHERE is_active = TRUE ORDER BY sort_order");
    }

    public async Task<DashboardWidget?> GetByKeyAsync(string widgetKey)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DashboardWidget>(
            "SELECT * FROM sys_dashboard_widgets WHERE widget_key = @WidgetKey", new { WidgetKey = widgetKey });
    }

    public async Task<IEnumerable<RoleDashboardConfig>> GetRoleConfigsAsync(long roleId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<RoleDashboardConfig>(
            "SELECT * FROM sys_role_dashboard_config WHERE role_id = @RoleId ORDER BY sort_order",
            new { RoleId = roleId });
    }

    public async Task SaveRoleConfigAsync(long roleId, IEnumerable<RoleDashboardConfig> configs)
    {
        using var conn = _connectionFactory.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "DELETE FROM sys_role_dashboard_config WHERE role_id = @RoleId",
            new { RoleId = roleId }, transaction);

        foreach (var config in configs)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO sys_role_dashboard_config (role_id, widget_key, is_enabled, config, sort_order)
                  VALUES (@RoleId, @WidgetKey, @IsEnabled, @Config, @SortOrder)",
                new { RoleId = roleId, config.WidgetKey, config.IsEnabled, config.Config, config.SortOrder },
                transaction);
        }

        transaction.Commit();
    }

    public async Task<int> DeleteRoleConfigsAsync(long roleId)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM sys_role_dashboard_config WHERE role_id = @RoleId",
            new { RoleId = roleId });
    }

    public async Task<long> AddWidgetAsync(DashboardWidget widget)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO sys_dashboard_widgets (widget_key, widget_name, widget_type, description, icon, default_config, 
              data_source_type, data_source_config, layout_config, refresh_interval, sort_order, is_active)
              VALUES (@WidgetKey, @WidgetName, @WidgetType, @Description, @Icon, @DefaultConfig,
              @DataSourceType, @DataSourceConfig, @LayoutConfig, 
              @RefreshInterval, @SortOrder, @IsActive)
              RETURNING id", widget);
    }

    public async Task<bool> UpdateWidgetAsync(DashboardWidget widget)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            @"UPDATE sys_dashboard_widgets SET widget_name = @WidgetName, widget_type = @WidgetType,
              description = @Description, icon = @Icon, default_config = @DefaultConfig,
              data_source_type = @DataSourceType, data_source_config = @DataSourceConfig,
              layout_config = @LayoutConfig, refresh_interval = @RefreshInterval,
              sort_order = @SortOrder, is_active = @IsActive, updated_at = NOW()
              WHERE id = @Id", widget) > 0;
    }

    public async Task<bool> DeleteWidgetAsync(long id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteAsync(
            "DELETE FROM sys_dashboard_widgets WHERE id = @Id", new { Id = id }) > 0;
    }
}