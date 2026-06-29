using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Application.Common;

public class ApiResult<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResult<T> Fail(string message)
        => new() { Success = false, Message = message };

    public static ApiResult<T> NotFound(string message = "未找到记录")
        => new() { Success = false, Message = message };
}

public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public long UserId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public bool ForceChangePassword { get; set; }
}

public class PasswordChangeRequest
{
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;
    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码长度不能少于6位")]
    public string NewPassword { get; set; } = string.Empty;
}