using EAMS2026.Domain.DTOs.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IPermissionService
{
    Task<IEnumerable<PermissionDto>> GetTreeAsync();
    Task<PermissionDto?> GetByIdAsync(long id);
    Task<PermissionDto?> GetByCodeAsync(string code);
    Task<(bool Success, string Message)> CreateAsync(PermissionCreateRequest request, long operatorId);
    Task<(bool Success, string Message)> UpdateAsync(PermissionUpdateRequest request, long operatorId);
    Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId);
}