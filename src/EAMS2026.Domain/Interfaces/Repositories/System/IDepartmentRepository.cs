using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IDepartmentRepository : IBaseRepository<Department>
{
    Task<IEnumerable<Department>> GetTreeAsync();
    Task<IEnumerable<Department>> GetChildrenAsync(long parentId);
    Task<bool> HasChildrenAsync(long id);
    Task<bool> IsCodeExistsAsync(string code, long? excludeId = null);
    Task<Department?> GetByCodeAsync(string code);
}