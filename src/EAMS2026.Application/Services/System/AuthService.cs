using EAMS2026.Application.Common;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EAMS2026.Application.Services.System;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly Common.Interfaces.IJwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IEmployeeRepository employeeRepository,
        IOperationLogRepository logRepository,
        Common.Interfaces.IJwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _employeeRepository = employeeRepository;
        _logRepository = logRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录验证。
    /// 流程：查用户 → 验密码 → 查角色权限 → 生成JWT Token。
    /// </summary>
    /// <param name="request">登录请求 {Username, Password}</param>
    /// <returns>登录成功返回Token和用户信息，失败返回null</returns>
    public async Task<Common.LoginResponse?> LoginAsync(Common.LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user == null || !user.Status)
            return null;

        if (user.ForceChangePassword)
        {
            // 强制修改密码状态下，跳过旧密码正确性验证
            // 但仍需验证密码非空，防止完全无认证登录
            if (string.IsNullOrEmpty(request.Password))
                return null;
        }
        else if (string.IsNullOrEmpty(user.PasswordHash))
        {
            // 密码哈希为空时，允许任意密码登录（首次登录/密码为空的场景）
            // 自动设置强制修改密码标志
            user.ForceChangePassword = true;
            await _userRepository.UpdateAsync(user);
        }
        else
        {
            // 正常情况：BCrypt验证密码
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return null;
        }

        // 更新最后登录时间
        await _userRepository.UpdateLastLoginAsync(user.Id);

        // 获取用户的角色编码列表
        var roles = await _userRepository.GetUserRolesAsync(user.Id);
        var roleNames = roles.Select(r => r.Code).ToList();

        // 获取用户的所有权限编码列表
        var permissions = await _permissionRepository.GetByUserIdAsync(user.Id);
        var permissionCodes = permissions.Select(p => p.Code).ToList();

        // 生成JWT Token，将角色和权限编码写入Claims
        var token = _jwtService.GenerateToken(user.Id, user.Username, roleNames, permissionCodes);

        // 查询员工姓名
        string? employeeName = null;
        if (user.EmployeeId.HasValue)
        {
            var employee = await _employeeRepository.GetByIdAsync(user.EmployeeId.Value);
            employeeName = employee?.Name;
        }

        return new Common.LoginResponse
        {
            Token = token,
            Username = user.Username,
            EmployeeName = employeeName,
            UserId = user.Id,
            Roles = roleNames,
            Permissions = permissionCodes,
            ForceChangePassword = user.ForceChangePassword
        };
    }

    /// <summary>
    /// 修改登录密码。
    /// 正常情况需验证原密码；强制修改密码状态下无需验证原密码。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="oldPassword">原密码（强制修改状态下可忽略）</param>
    /// <param name="newPassword">新密码</param>
    /// <returns>操作结果</returns>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(long userId, string oldPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return (false, "用户不存在");

        if (!user.ForceChangePassword)
        {
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                return (false, "原密码错误");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ForceChangePassword = false;
        user.UpdatedBy = userId;
        await _userRepository.UpdateAsync(user);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = userId,
            Username = user.Username,
            OperationType = "ChangePassword",
            Module = "Profile",
            Description = "修改登录密码"
        });

        return (true, "密码修改成功");
    }

    /// <summary>
    /// 更新当前用户的个人信息（电话、邮箱、职位）。
    /// 同步更新关联的员工记录。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="phone">电话</param>
    /// <param name="email">邮箱</param>
    /// <param name="position">职位</param>
    /// <returns>操作结果</returns>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(long userId, string? phone, string? email, string? position)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return (false, "用户不存在");

        // 先读取数据库中的旧值（从员工记录中获取），用于操作日志记录
        string? oldPhone = null, oldEmail = null, oldPosition = null;
        if (user.EmployeeId.HasValue)
        {
            var currentEmployee = await _employeeRepository.GetByIdAsync(user.EmployeeId.Value);
            if (currentEmployee != null)
            {
                oldPhone = currentEmployee.Phone;
                oldEmail = currentEmployee.Email;
                oldPosition = currentEmployee.Position;
            }
        }
        var oldValues = new { phone = oldPhone, email = oldEmail, position = oldPosition };

        // 如果用户关联了员工记录，同步更新员工信息
        if (user.EmployeeId.HasValue)
        {
            var employee = await _employeeRepository.GetByIdAsync(user.EmployeeId.Value);
            if (employee != null)
            {
                if (!string.IsNullOrEmpty(phone)) employee.Phone = phone;
                if (!string.IsNullOrEmpty(email)) employee.Email = email;
                if (!string.IsNullOrEmpty(position)) employee.Position = position;
                employee.UpdatedBy = userId;
                await _employeeRepository.UpdateAsync(employee);
            }
        }

        user.UpdatedBy = userId;
        await _userRepository.UpdateAsync(user);

        await _logRepository.AddAsync(new OperationLog
        {
            UserId = userId,
            Username = user.Username,
            OperationType = "Update",
            Module = "Profile",
            Description = "修改个人信息",
            OldValue = JsonSerializer.Serialize(oldValues),
            NewValue = JsonSerializer.Serialize(new { phone, email, position })
        });

        return (true, "个人信息更新成功");
    }

    /// <summary>
    /// 获取当前用户的完整个人资料，包括关联的员工信息。
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户档案信息</returns>
    public async Task<(bool Success, string Message, object? Data)> GetProfileAsync(long userId)
    {
        var user = await _userRepository.GetWithRolesAsync(userId);
        if (user == null)
            return (false, "用户不存在", null);

        Employee? employee = null;
        if (user.EmployeeId.HasValue)
        {
            employee = await _employeeRepository.GetByIdAsync(user.EmployeeId.Value);
        }

        var profile = new
        {
            user.Id,
            user.Username,
            user.Status,
            user.LastLoginAt,
            user.ForceChangePassword,
            Roles = user.Roles,
            Employee = employee != null ? new
            {
                employee.Id,
                employee.EmployeeNo,
                employee.Name,
                employee.Gender,
                employee.Phone,
                employee.Email,
                employee.DepartmentId,
                employee.DepartmentName,
                employee.Position,
                employee.HireDate
            } : null
        };

        return (true, "获取成功", profile);
    }
}