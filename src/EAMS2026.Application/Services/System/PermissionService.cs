using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.DTOs.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(IPermissionRepository permissionRepository, ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<PermissionDto>> GetTreeAsync()
    {
        var permissions = await _permissionRepository.GetTreeAsync();
        return permissions.Select(MapToDto);
    }

    public async Task<PermissionDto?> GetByIdAsync(long id)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        return permission == null ? null : MapToDto(permission);
    }

    public async Task<PermissionDto?> GetByCodeAsync(string code)
    {
        var permission = await _permissionRepository.GetByCodeAsync(code);
        return permission == null ? null : MapToDto(permission);
    }

    public async Task<(bool Success, string Message)> CreateAsync(PermissionCreateRequest request, long operatorId)
    {
        if (await _permissionRepository.IsCodeExistsAsync(request.Code))
            return (false, "权限编码已存在");

        var permission = new Permission
        {
            Name = request.Name,
            Code = request.Code,
            Type = request.Type,
            ParentId = request.ParentId,
            Path = request.Path,
            Icon = request.Icon,
            SortOrder = request.SortOrder,
            CreatedBy = operatorId,
            UpdatedBy = operatorId
        };

        await _permissionRepository.AddAsync(permission);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(PermissionUpdateRequest request, long operatorId)
    {
        var existing = await _permissionRepository.GetByIdAsync(request.Id);
        if (existing == null)
            return (false, "权限不存在");

        if (await _permissionRepository.IsCodeExistsAsync(request.Code, request.Id))
            return (false, "权限编码已存在");

        existing.Name = request.Name;
        existing.Code = request.Code;
        existing.Type = request.Type;
        existing.ParentId = request.ParentId;
        existing.Path = request.Path;
        existing.Icon = request.Icon;
        existing.SortOrder = request.SortOrder;
        existing.UpdatedBy = operatorId;

        await _permissionRepository.UpdateAsync(existing);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId)
    {
        var permission = await _permissionRepository.GetByIdAsync(id);
        if (permission == null)
            return (false, "权限不存在");

        await _permissionRepository.SoftDeleteAsync(id);
        return (true, "删除成功");
    }

    private static PermissionDto MapToDto(Permission permission)
    {
        return new PermissionDto
        {
            Id = permission.Id,
            Name = permission.Name,
            Code = permission.Code,
            Type = permission.Type,
            ParentId = permission.ParentId,
            Path = permission.Path,
            Icon = permission.Icon,
            SortOrder = permission.SortOrder,
            Children = permission.Children?.Select(MapToDto).ToList() ?? new List<PermissionDto>()
        };
    }
}