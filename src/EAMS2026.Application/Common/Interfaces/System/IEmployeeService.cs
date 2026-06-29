using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IEmployeeService
{
    Task<IEnumerable<Employee>> GetAllAsync();
    Task<Employee?> GetByIdAsync(long id);
    Task<IEnumerable<Employee>> GetByDepartmentIdAsync(long departmentId);
    Task<Employee?> GetByEmployeeNoAsync(string employeeNo);
    Task<(bool Success, string Message)> CreateAsync(Employee employee, long operatorId);
    Task<(bool Success, string Message)> UpdateAsync(Employee employee, long operatorId);
    Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId);
    Task<IEnumerable<Employee>> GetDeletedAsync();
    Task<(bool Success, string Message)> HardDeleteAsync(long id, long operatorId);
}