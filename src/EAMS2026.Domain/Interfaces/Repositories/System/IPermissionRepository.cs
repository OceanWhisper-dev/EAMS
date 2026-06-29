using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IPermissionRepository : IBaseRepository<Permission>
{
    Task<IEnumerable<Permission>> GetTreeAsync();
    Task<IEnumerable<Permission>> GetByRoleIdAsync(long roleId);
    Task<IEnumerable<Permission>> GetByUserIdAsync(long userId);
    Task<Permission?> GetByCodeAsync(string code);
    Task<bool> IsCodeExistsAsync(string code, long? excludeId = null);
}