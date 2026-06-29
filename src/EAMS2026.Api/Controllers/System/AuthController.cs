using EAMS2026.Application.Common;
using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { success = false, message = "用户名或密码错误" });

        return Success(result, "登录成功");
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        return await ExecuteAsync(() => _authService.GetProfileAsync(GetUserId()));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeRequest request)
    {
        return await ExecuteAsync(() => _authService.ChangePasswordAsync(GetUserId(), request.OldPassword, request.NewPassword));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
    {
        return await ExecuteAsync(() => _authService.UpdateProfileAsync(GetUserId(), request.Phone, request.Email, request.Position));
    }
}

public record ProfileUpdateRequest(string? Phone, string? Email, string? Position);