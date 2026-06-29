using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Enums;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IDataPermissionRepository
{
    Task<DataPermissionRule?> GetByRoleAndModuleAsync(long roleId, string module);
    Task<IEnumerable<DataPermissionRule>> GetByModuleAsync(string module);
    Task<IEnumerable<DataPermissionRule>> GetAllAsync();
    Task<bool> SaveAsync(DataPermissionRule rule);
    Task<bool> DeleteAsync(long roleId, string module);
}