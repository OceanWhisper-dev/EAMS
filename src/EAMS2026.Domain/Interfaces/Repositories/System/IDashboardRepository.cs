namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IDashboardRepository
{
    Task<int> GetDepartmentCountAsync();
    Task<int> GetEmployeeCountAsync();
    Task<int> GetUserCountAsync();
    Task<int> GetRoleCountAsync();
}