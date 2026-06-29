using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Common;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EAMS2026.Application.Services.System;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IOperationLogRepository logRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _userRepository.GetAllWithRolesAsync();
    }

    public async Task<PagedResult<User>> GetPagedAsync(int page = 1, int pageSize = 10, string? keyword = null)
    {
        var all = await _userRepository.GetAllWithRolesAsync();
        var query = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (u.Roles.Any(r => r.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase))));
        }

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<User>(items, total, page, pageSize);
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _userRepository.GetWithRolesAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _userRepository.GetByUsernameAsync(username);
    }

    public async Task<(bool Success, string Message, long Data)> CreateAsync(User user, long operatorId)
    {
        if (await _userRepository.IsUsernameExistsAsync(user.Username))
            return (false, "用户名已存在", 0);

        // 注意：此处 user.PasswordHash 实际存储的是客户端传入的明文密码。
        // 在插入数据库之前，进行 BCrypt 哈希加密，然后覆盖原字段值。
        // 这样做是为了避免创建额外的 DTO 层，但字段名 PasswordHash 有误导性。
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        user.CreatedBy = operatorId;
        user.UpdatedBy = operatorId;
        var id = await _userRepository.AddAsync(user);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Create",
            Module = "User",
            EntityType = "User",
            EntityId = id,
            Description = $"创建用户: {user.Username}",
            NewValue = JsonSerializer.Serialize(user)
        });

        return (true, "创建成功", id);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(User user, long operatorId)
    {
        var existing = await _userRepository.GetByIdAsync(user.Id);
        if (existing == null)
            return (false, "用户不存在");

        if (await _userRepository.IsUsernameExistsAsync(user.Username, user.Id))
            return (false, "用户名已存在");

        user.UpdatedBy = operatorId;
        await _userRepository.UpdateAsync(user);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Update",
            Module = "User",
            EntityType = "User",
            EntityId = user.Id,
            Description = $"更新用户: {user.Username}",
            OldValue = JsonSerializer.Serialize(existing),
            NewValue = JsonSerializer.Serialize(user)
        });

        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(long id, long operatorId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return (false, "用户不存在");

        if (id == 1)
            return (false, "不能删除超级管理员");

        await _userRepository.SoftDeleteAsync(id);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "Delete",
            Module = "User",
            EntityType = "User",
            EntityId = id,
            Description = $"删除用户: {user.Username}"
        });

        return (true, "删除成功");
    }

    public async Task<(bool Success, string Message)> AssignRolesAsync(long userId, IEnumerable<long> roleIds, long operatorId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return (false, "用户不存在");

        await _userRepository.AssignRolesAsync(userId, roleIds);
        return (true, "角色分配成功");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(long userId, long operatorId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return (false, "用户不存在");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("");
        user.ForceChangePassword = true;
        user.UpdatedBy = operatorId;
        await _userRepository.UpdateAsync(user);

        return (true, "密码已重置为空，下次登录必须修改密码");
    }

    public async Task<IEnumerable<User>> GetDeletedAsync()
    {
        return await _userRepository.GetDeletedAsync();
    }

    public async Task<(bool Success, string Message)> HardDeleteAsync(long id, long operatorId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            // 可能已被软删除，检查已删除列表
            var deleted = await _userRepository.GetDeletedAsync();
            user = deleted.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return (false, "用户不存在");
        }

        if (id == 1)
            return (false, "不能删除超级管理员");

        await _userRepository.HardDeleteAsync(id);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = operatorId,
            OperationType = "HardDelete",
            Module = "User",
            EntityType = "User",
            EntityId = id,
            Description = $"永久删除用户: {user.Username}"
        });

        return (true, "已永久删除");
    }
}