using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetWithRolesAsync(long id);
    Task<IEnumerable<User>> GetAllWithRolesAsync();
    Task<IEnumerable<Role>> GetUserRolesAsync(long userId);
    Task<bool> AssignRolesAsync(long userId, IEnumerable<long> roleIds);
    Task<bool> IsUsernameExistsAsync(string username, long? excludeId = null);
    Task UpdateLastLoginAsync(long userId);
    Task<IEnumerable<User>> GetDeletedAsync();
    Task<bool> HardDeleteAsync(long id);
}