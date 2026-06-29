using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IDashboardRepository dashboardRepository, ILogger<DashboardService> logger)
    {
        _dashboardRepository = dashboardRepository;
        _logger = logger;
    }

    public async Task<Dictionary<string, int>> GetStatsAsync()
    {
        var departments = await _dashboardRepository.GetDepartmentCountAsync();
        var employees = await _dashboardRepository.GetEmployeeCountAsync();
        var users = await _dashboardRepository.GetUserCountAsync();
        var roles = await _dashboardRepository.GetRoleCountAsync();

        return new Dictionary<string, int>
        {
            { "departments", departments },
            { "employees", employees },
            { "users", users },
            { "roles", roles }
        };
    }
}