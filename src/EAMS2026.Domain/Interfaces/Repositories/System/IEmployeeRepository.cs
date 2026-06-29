using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IEmployeeRepository : IBaseRepository<Employee>
{
    Task<IEnumerable<Employee>> GetByDepartmentIdAsync(long departmentId);
    Task<Employee?> GetByEmployeeNoAsync(string employeeNo);
    Task<bool> IsEmployeeNoExistsAsync(string employeeNo, long? excludeId = null);
    Task<IEnumerable<Employee>> GetDeletedAsync();
    Task<bool> HardDeleteAsync(long id);
}