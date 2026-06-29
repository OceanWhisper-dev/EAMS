using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using EAMS2026.Domain.Entities.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize(Policy = "user")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
[Route("api/user")]
public class UserController : BaseController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _userService.GetAllAsync();
        return Success(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _userService.GetByIdAsync(id);
        if (data == null)
            return NotFound("用户不存在");
        return Success(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] User user)
    {
        return await ExecuteAsync<long>(() => _userService.CreateAsync(user, GetUserId()));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] User user)
    {
        return await ExecuteAsync(() => _userService.UpdateAsync(user, GetUserId()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        return await ExecuteAsync(() => _userService.DeleteAsync(id, GetUserId()));
    }

    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> AssignRoles(long userId, [FromBody] List<long> roleIds)
    {
        return await ExecuteAsync(() => _userService.AssignRolesAsync(userId, roleIds, GetUserId()));
    }

    [HttpPost("{userId}/reset-password")]
    public async Task<IActionResult> ResetPassword(long userId)
    {
        return await ExecuteAsync(() => _userService.ResetPasswordAsync(userId, GetUserId()));
    }

    /// <summary>获取已删除（软删除）的用户列表</summary>
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted()
    {
        var data = await _userService.GetDeletedAsync();
        return Success(data);
    }

    /// <summary>永久删除用户（从数据库中彻底移除）</summary>
    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(long id)
    {
        return await ExecuteAsync(() => _userService.HardDeleteAsync(id, GetUserId()));
    }
}