using EAMS2026.Domain.DTOs.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllAsync();
    Task<RoleDto?> GetByIdAsync(long id);
    Task<RoleDto?> GetByCodeAsync(string code);
    Task<(bool Success, string Message)> CreateAsync(RoleCreateRequest request, long operatorId);
    Task<(bool Success, string Message)> UpdateAsync(RoleUpdateRequest request, long operatorId);
    Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId);
    Task<IEnumerable<PermissionDto>> GetPermissionsAsync(long roleId);
    Task<(bool Success, string Message)> AssignPermissionsAsync(long roleId, IEnumerable<long> permissionIds, long operatorId);
    Task<IEnumerable<RoleUserDto>> GetUsersAsync(long roleId);
    Task<(bool Success, string Message)> BatchAssignUsersAsync(long roleId, IEnumerable<long> userIds, long operatorId);
    Task<IEnumerable<RoleDto>> GetDeletedAsync();
    Task<(bool Success, string Message)> HardDeleteAsync(long id, long operatorId);
}