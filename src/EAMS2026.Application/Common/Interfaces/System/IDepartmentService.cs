using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IDepartmentService
{
    Task<IEnumerable<Department>> GetTreeAsync();
    Task<IEnumerable<Department>> GetAllAsync();
    Task<Department?> GetByIdAsync(long id);
    Task<Department?> GetByCodeAsync(string code);
    Task<bool> IsCodeExistsAsync(string code);
    Task<(bool Success, string Message)> CreateAsync(Department department, long operatorId);
    Task<(bool Success, string Message)> UpdateAsync(Department department, long operatorId);
    Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId);
}