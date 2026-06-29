using System.Text.Json;
using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class DashboardConfigService : IDashboardConfigService
{
    private readonly IDashboardWidgetRepository _widgetRepository;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDynamicDataEngine _dataEngine;
    private readonly IDashboardSettings _dashboardSettings;
    private readonly ILogger<DashboardConfigService> _logger;

    public DashboardConfigService(
        IDashboardWidgetRepository widgetRepository,
        IDashboardRepository dashboardRepository,
        IMessageRepository messageRepository,
        IOperationLogRepository logRepository,
        IRoleRepository roleRepository,
        IDynamicDataEngine dataEngine,
        IDashboardSettings dashboardSettings,
        ILogger<DashboardConfigService> logger)
    {
        _widgetRepository = widgetRepository;
        _dashboardRepository = dashboardRepository;
        _messageRepository = messageRepository;
        _logRepository = logRepository;
        _roleRepository = roleRepository;
        _dataEngine = dataEngine;
        _dashboardSettings = dashboardSettings;
        _logger = logger;
    }

    public async Task<IEnumerable<Dictionary<string, object>>> GetAllWidgetsAsync()
    {
        var widgets = await _widgetRepository.GetAllAsync();
        return widgets.Select(w => new Dictionary<string, object>
        {
            { "id", w.Id },
            { "widgetKey", w.WidgetKey },
            { "widgetName", w.WidgetName },
            { "widgetType", w.WidgetType },
            { "description", w.Description ?? "" },
            { "icon", w.Icon ?? "" },
            { "defaultConfig", ParseJson(w.DefaultConfig) },
            { "dataSourceType", w.DataSourceType },
            { "dataSourceConfig", ParseJson(w.DataSourceConfig) },
            { "layoutConfig", ParseJson(w.LayoutConfig) },
            { "refreshInterval", w.RefreshInterval },
            { "sortOrder", w.SortOrder }
        });
    }

    public async Task<IEnumerable<Dictionary<string, object>>> GetRoleDashboardConfigAsync(long roleId)
    {
        var widgets = await _widgetRepository.GetAllAsync();
        var configs = await _widgetRepository.GetRoleConfigsAsync(roleId);
        var configDict = configs.ToDictionary(c => c.WidgetKey, c => c);

        var result = new List<Dictionary<string, object>>();
        foreach (var widget in widgets.OrderBy(w => w.SortOrder))
        {
            configDict.TryGetValue(widget.WidgetKey, out var config);
            result.Add(new Dictionary<string, object>
            {
                { "widgetKey", widget.WidgetKey },
                { "widgetName", widget.WidgetName },
                { "widgetType", widget.WidgetType },
                { "description", widget.Description ?? "" },
                { "icon", widget.Icon ?? "" },
                { "dataSourceType", widget.DataSourceType },
                { "dataSourceConfig", ParseJson(widget.DataSourceConfig) },
                { "layoutConfig", ParseJson(widget.LayoutConfig) },
                { "refreshInterval", widget.RefreshInterval },
                { "isEnabled", config?.IsEnabled ?? false },
                { "config", ParseJson(config?.Config ?? widget.DefaultConfig) },
                { "sortOrder", config?.SortOrder ?? widget.SortOrder }
            });
        }
        return result;
    }

    public async Task SaveRoleDashboardConfigAsync(long roleId, List<Dictionary<string, object>> configs)
    {
        var roleConfigs = configs.Select((c, index) => new RoleDashboardConfig
        {
            RoleId = roleId,
            WidgetKey = c["widgetKey"].ToString() ?? "",
            IsEnabled = c["isEnabled"] is JsonElement je ? je.GetBoolean() : Convert.ToBoolean(c["isEnabled"]),
            Config = c.ContainsKey("config") ? JsonSerializer.Serialize(c["config"]) : null,
            SortOrder = index
        }).ToList();

        await _widgetRepository.SaveRoleConfigAsync(roleId, roleConfigs);
    }

    public async Task<IEnumerable<Dictionary<string, object>>> GetUserDashboardAsync(long userId, IEnumerable<string> roleCodes)
    {
        var roles = await GetRolesByCodesAsync(roleCodes);
        var allConfigs = new List<RoleDashboardConfig>();

        foreach (var role in roles)
        {
            var configs = await _widgetRepository.GetRoleConfigsAsync(role.Id);
            allConfigs.AddRange(configs);
        }

        if (!allConfigs.Any())
        {
            var defaultWidgets = await _widgetRepository.GetAllAsync();
            return defaultWidgets.Select(w => new Dictionary<string, object>
            {
                { "widgetKey", w.WidgetKey },
                { "widgetName", w.WidgetName },
                { "widgetType", w.WidgetType },
                { "config", ParseJson(w.DefaultConfig) },
                { "layoutConfig", ParseJson(w.LayoutConfig) },
                { "refreshInterval", w.RefreshInterval },
                { "data", new Dictionary<string, object>() }
            });
        }

        var enabledKeys = allConfigs.Where(c => c.IsEnabled).Select(c => c.WidgetKey).ToHashSet();
        var widgets = await _widgetRepository.GetAllAsync();
        var widgetDict = widgets.ToDictionary(w => w.WidgetKey, w => w);
        var configDict = allConfigs.GroupBy(c => c.WidgetKey)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<Dictionary<string, object>>();
        foreach (var key in enabledKeys)
        {
            if (!widgetDict.TryGetValue(key, out var widget)) continue;

            var config = configDict[key];
            var data = await GetWidgetDataAsync(widget, config.Config, userId, roleCodes);

            result.Add(new Dictionary<string, object>
            {
                { "widgetKey", widget.WidgetKey },
                { "widgetName", widget.WidgetName },
                { "widgetType", widget.WidgetType },
                { "config", ParseJson(config.Config ?? widget.DefaultConfig) },
                { "layoutConfig", ParseJson(widget.LayoutConfig) },
                { "refreshInterval", widget.RefreshInterval },
                { "data", data }
            });
        }

        return result.OrderBy(r =>
        {
            configDict.TryGetValue(r["widgetKey"].ToString() ?? "", out var c);
            return c?.SortOrder ?? 0;
        });
    }

    public async Task<long> AddWidgetAsync(DashboardWidget widget)
    {
        return await _widgetRepository.AddWidgetAsync(widget);
    }

    public async Task<bool> UpdateWidgetAsync(DashboardWidget widget)
    {
        return await _widgetRepository.UpdateWidgetAsync(widget);
    }

    public async Task<bool> DeleteWidgetAsync(long id)
    {
        return await _widgetRepository.DeleteWidgetAsync(id);
    }

    public async Task<object> PreviewWidgetDataAsync(string dataSourceType, string? dataSourceConfig)
    {
        return await _dataEngine.ExecuteAsync(dataSourceType, dataSourceConfig);
    }

    private async Task<Dictionary<string, object>> GetWidgetDataAsync(DashboardWidget widget, string? configJson, long userId, IEnumerable<string> roleCodes)
    {
        if (widget.WidgetKey == "recent_logs")
        {
            var logConfig = ParseJson(configJson);
            return new Dictionary<string, object> { { "value", await GetRecentLogsAsync(logConfig, userId, roleCodes) } };
        }

        if (!string.IsNullOrEmpty(widget.DataSourceConfig))
        {
            var data = await _dataEngine.ExecuteAsync(widget.DataSourceType, widget.DataSourceConfig);
            return new Dictionary<string, object> { { "value", data } };
        }

        var config = ParseJson(configJson);
        return widget.WidgetKey switch
        {
            "stat_departments" => new Dictionary<string, object> { { "value", await _dashboardRepository.GetDepartmentCountAsync() } },
            "stat_employees" => new Dictionary<string, object> { { "value", await _dashboardRepository.GetEmployeeCountAsync() } },
            "stat_users" => new Dictionary<string, object> { { "value", await _dashboardRepository.GetUserCountAsync() } },
            "stat_roles" => new Dictionary<string, object> { { "value", await _dashboardRepository.GetRoleCountAsync() } },
            "stat_messages" => new Dictionary<string, object> { { "value", await _messageRepository.GetUnreadCountAsync(userId) } },
            _ => new Dictionary<string, object>()
        };
    }

    private async Task<IEnumerable<Dictionary<string, object>>> GetRecentLogsAsync(Dictionary<string, object> config, long userId, IEnumerable<string> roleCodes)
    {
        var limit = _dashboardSettings.RecentLogLimit;
        if (config.TryGetValue("limit", out var val) && val is JsonElement je)
            limit = je.GetInt32();
        var isAdmin = roleCodes.Any(r => r == "super_admin");
        var logs = await _logRepository.GetPagedAsync(1, limit, userId: isAdmin ? null : userId);
        return logs.Select(l => new Dictionary<string, object>
        {
            { "id", l.Id },
            { "username", l.Username },
            { "module", l.Module },
            { "operationType", l.OperationType },
            { "description", l.Description ?? "" },
            { "createdAt", l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") }
        });
    }

    private async Task<IEnumerable<Role>> GetRolesByCodesAsync(IEnumerable<string> roleCodes)
    {
        return await _roleRepository.GetByCodesAsync(roleCodes);
    }

    private static Dictionary<string, object> ParseJson(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new Dictionary<string, object>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }
}