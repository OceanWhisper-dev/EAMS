using EAMS2026.Domain.Common;
using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<PagedResult<User>> GetPagedAsync(int page = 1, int pageSize = 10, string? keyword = null);
    Task<User?> GetByIdAsync(long id);
    Task<User?> GetByUsernameAsync(string username);
    Task<(bool Success, string Message, long Data)> CreateAsync(User user, long operatorId);
    Task<(bool Success, string Message)> UpdateAsync(User user, long operatorId);
    Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId);
    Task<(bool Success, string Message)> AssignRolesAsync(long userId, IEnumerable<long> roleIds, long operatorId);
    Task<(bool Success, string Message)> ResetPasswordAsync(long userId, long operatorId);
    Task<IEnumerable<User>> GetDeletedAsync();
    Task<(bool Success, string Message)> HardDeleteAsync(long id, long operatorId);
}