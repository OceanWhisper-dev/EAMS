using EAMS2026.Application.Common;
using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<(bool Success, string Message)> ChangePasswordAsync(long userId, string oldPassword, string newPassword);
    Task<(bool Success, string Message)> UpdateProfileAsync(long userId, string? phone, string? email, string? position);
    Task<(bool Success, string Message, object? Data)> GetProfileAsync(long userId);
}