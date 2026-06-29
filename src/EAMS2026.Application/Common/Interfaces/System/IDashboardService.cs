namespace EAMS2026.Application.Common.Interfaces.System;

public interface IDashboardService
{
    Task<Dictionary<string, int>> GetStatsAsync();
}