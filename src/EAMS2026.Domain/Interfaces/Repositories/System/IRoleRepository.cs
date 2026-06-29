using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IRoleRepository : IBaseRepository<Role>
{
    Task<Role?> GetByCodeAsync(string code);
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(long roleId);
    Task<bool> AssignPermissionsAsync(long roleId, IEnumerable<long> permissionIds);
    Task<bool> IsCodeExistsAsync(string code, long? excludeId = null);
    Task<IEnumerable<Role>> GetByCodesAsync(IEnumerable<string> codes);
    Task<IEnumerable<User>> GetRoleUsersAsync(long roleId);
    Task<bool> AssignUsersToRoleAsync(long roleId, IEnumerable<long> userIds);
    Task<IEnumerable<Role>> GetDeletedAsync();
    Task<bool> HardDeleteAsync(long id);
}