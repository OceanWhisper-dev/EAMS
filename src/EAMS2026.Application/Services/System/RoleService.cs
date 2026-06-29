using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.DTOs.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IRoleRepository roleRepository, IOperationLogRepository logRepository, ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(MapToDto);
    }

    public async Task<RoleDto?> GetByIdAsync(long id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role == null ? null : MapToDto(role);
    }

    public async Task<RoleDto?> GetByCodeAsync(string code)
    {
        var role = await _roleRepository.GetByCodeAsync(code);
        return role == null ? null : MapToDto(role);
    }

    public async Task<(bool Success, string Message)> CreateAsync(RoleCreateRequest request, long operatorId)
    {
        if (await _roleRepository.IsCodeExistsAsync(request.Code))
            return (false, "角色编码已存在");

        var role = new Role
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Status = request.Status,
            CreatedBy = operatorId,
            UpdatedBy = operatorId
        };

        var id = await _roleRepository.AddAsync(role);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Create",
            Module = "Role",
            EntityType = "Role",
            EntityId = id,
            Description = $"创建角色: {role.Name}"
        });

        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(RoleUpdateRequest request, long operatorId)
    {
        var existing = await _roleRepository.GetByIdAsync(request.Id);
        if (existing == null)
            return (false, "角色不存在");

        if (await _roleRepository.IsCodeExistsAsync(request.Code, request.Id))
            return (false, "角色编码已存在");

        if (request.Id == 1 && !request.Status)
            return (false, "不能禁用超级管理员角色");

        existing.Name = request.Name;
        existing.Code = request.Code;
        existing.Description = request.Description;
        existing.Status = request.Status;
        existing.UpdatedBy = operatorId;

        await _roleRepository.UpdateAsync(existing);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Update",
            Module = "Role",
            EntityType = "Role",
            EntityId = request.Id,
            Description = $"更新角色: {request.Name}"
        });

        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
            return (false, "角色不存在");

        if (id == 1)
            return (false, "不能删除超级管理员角色");

        await _roleRepository.SoftDeleteAsync(id);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Delete",
            Module = "Role",
            EntityType = "Role",
            EntityId = id,
            Description = $"删除角色: {role.Name}"
        });

        return (true, "删除成功");
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsAsync(long roleId)
    {
        var permissions = await _roleRepository.GetRolePermissionsAsync(roleId);
        return permissions.Select(p => new PermissionDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Type = p.Type,
            ParentId = p.ParentId,
            Path = p.Path,
            Icon = p.Icon,
            SortOrder = p.SortOrder
        });
    }

    public async Task<(bool Success, string Message)> AssignPermissionsAsync(long roleId, IEnumerable<long> permissionIds, long operatorId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
            return (false, "角色不存在");

        await _roleRepository.AssignPermissionsAsync(roleId, permissionIds);
        return (true, "权限分配成功");
    }

    public async Task<IEnumerable<RoleUserDto>> GetUsersAsync(long roleId)
    {
        var users = await _roleRepository.GetRoleUsersAsync(roleId);
        return users.Select(u => new RoleUserDto
        {
            Id = u.Id,
            Username = u.Username,
            EmployeeId = u.EmployeeId,
            EmployeeName = u.EmployeeName,
            Status = u.Status
        });
    }

    public async Task<(bool Success, string Message)> BatchAssignUsersAsync(long roleId, IEnumerable<long> userIds, long operatorId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
            return (false, "角色不存在");

        await _roleRepository.AssignUsersToRoleAsync(roleId, userIds);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Assign",
            Module = "Role",
            EntityType = "Role",
            EntityId = roleId,
            Description = $"批量分配用户到角色: {role.Name}"
        });

        return (true, "用户分配成功");
    }

    public async Task<IEnumerable<RoleDto>> GetDeletedAsync()
    {
        var roles = await _roleRepository.GetDeletedAsync();
        return roles.Select(MapToDto);
    }

    public async Task<(bool Success, string Message)> HardDeleteAsync(long id, long operatorId)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        if (role == null)
        {
            var deleted = await _roleRepository.GetDeletedAsync();
            role = deleted.FirstOrDefault(r => r.Id == id);
            if (role == null)
                return (false, "角色不存在");
        }

        if (id == 1)
            return (false, "不能删除超级管理员角色");

        await _roleRepository.HardDeleteAsync(id);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "HardDelete",
            Module = "Role",
            EntityType = "Role",
            EntityId = id,
            Description = $"永久删除角色: {role.Name}"
        });

        return (true, "已永久删除");
    }

    private static RoleDto MapToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Code = role.Code,
            Description = role.Description,
            Status = role.Status
        };
    }
}