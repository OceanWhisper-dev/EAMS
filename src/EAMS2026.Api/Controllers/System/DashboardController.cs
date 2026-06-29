using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;

namespace EAMS2026.Api.Controllers.System;

[Authorize]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/dashboard")]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;
    private readonly IDashboardConfigService _configService;
    private readonly IUserRepository _userRepository;

    public DashboardController(
        IDashboardService dashboardService,
        IDashboardConfigService configService,
        IUserRepository userRepository)
    {
        _dashboardService = dashboardService;
        _configService = configService;
        _userRepository = userRepository;
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync();
        return Success(stats, "获取统计数据成功");
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpGet("widgets")]
    public async Task<IActionResult> GetWidgets()
    {
        var widgets = await _configService.GetAllWidgetsAsync();
        return Success(widgets, "获取组件列表成功");
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpGet("config")]
    public async Task<IActionResult> GetRoleConfig([FromQuery] long roleId)
    {
        var config = await _configService.GetRoleDashboardConfigAsync(roleId);
        return Success(config, "获取角色仪表盘配置成功");
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpPost("config")]
    public async Task<IActionResult> SaveRoleConfig([FromBody] SaveConfigRequest request)
    {
        await _configService.SaveRoleDashboardConfigAsync(request.RoleId, request.Configs);
        return Success("保存成功");
    }

    [HttpGet("my-dashboard")]
    public async Task<IActionResult> GetMyDashboard()
    {
        var user = await _userRepository.GetWithRolesAsync(GetUserId());
        if (user == null) return NotFound("用户不存在");

        var roleCodes = user.Roles.Select(r => r.Code);
        var dashboard = await _configService.GetUserDashboardAsync(GetUserId(), roleCodes);
        return Success(dashboard, "获取仪表盘数据成功");
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpPost("widget")]
    public async Task<IActionResult> AddWidget([FromBody] WidgetRequest request)
    {
        var widget = new DashboardWidget
        {
            WidgetKey = request.WidgetKey,
            WidgetName = request.WidgetName,
            WidgetType = request.WidgetType,
            Description = request.Description,
            Icon = request.Icon,
            DefaultConfig = request.DefaultConfig,
            DataSourceType = request.DataSourceType,
            DataSourceConfig = request.DataSourceConfig,
            LayoutConfig = request.LayoutConfig,
            RefreshInterval = request.RefreshInterval,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        var id = await _configService.AddWidgetAsync(widget);
        return Success(new { id }, "新增组件成功");
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpPut("widget/{id}")]
    public async Task<IActionResult> UpdateWidget(long id, [FromBody] WidgetRequest request)
    {
        var widget = new DashboardWidget
        {
            Id = id,
            WidgetKey = request.WidgetKey,
            WidgetName = request.WidgetName,
            WidgetType = request.WidgetType,
            Description = request.Description,
            Icon = request.Icon,
            DefaultConfig = request.DefaultConfig,
            DataSourceType = request.DataSourceType,
            DataSourceConfig = request.DataSourceConfig,
            LayoutConfig = request.LayoutConfig,
            RefreshInterval = request.RefreshInterval,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        var result = await _configService.UpdateWidgetAsync(widget);
        return result ? Success("更新组件成功") : Fail("更新失败");
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpDelete("widget/{id}")]
    public async Task<IActionResult> DeleteWidget(long id)
    {
        var result = await _configService.DeleteWidgetAsync(id);
        return result ? Success("删除组件成功") : Fail("删除失败");
    }

    [Authorize(Policy = "dashboard-config")]
    [HttpPost("widget/preview")]
    public async Task<IActionResult> PreviewWidgetData([FromBody] WidgetPreviewRequest request)
    {
        var data = await _configService.PreviewWidgetDataAsync(request.DataSourceType, request.DataSourceConfig);
        return Success(data, "预览数据成功");
    }
}

public class WidgetRequest
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

public class WidgetPreviewRequest
{
    public string DataSourceType { get; set; } = "sql";
    public string? DataSourceConfig { get; set; }
}

public class SaveConfigRequest
{
    public long RoleId { get; set; }
    public List<Dictionary<string, object>> Configs { get; set; } = new();
}