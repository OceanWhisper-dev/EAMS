using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.DTOs.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize(Policy = "role")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
[Route("api/role")]
public class RoleController : BaseController
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _roleService.GetAllAsync();
        return Success(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _roleService.GetByIdAsync(id);
        if (data == null)
            return NotFound("角色不存在");
        return Success(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleCreateRequest request)
    {
        return await ExecuteAsync(() => _roleService.CreateAsync(request, GetUserId()));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] RoleUpdateRequest request)
    {
        return await ExecuteAsync(() => _roleService.UpdateAsync(request, GetUserId()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        return await ExecuteAsync(() => _roleService.DeleteAsync(id, GetUserId()));
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetPermissions(long roleId)
    {
        var data = await _roleService.GetPermissionsAsync(roleId);
        return Success(data);
    }

    [HttpPost("{roleId}/permissions")]
    public async Task<IActionResult> AssignPermissions(long roleId, [FromBody] List<long> permissionIds)
    {
        return await ExecuteAsync(() => _roleService.AssignPermissionsAsync(roleId, permissionIds, GetUserId()));
    }

    [HttpGet("{roleId}/users")]
    public async Task<IActionResult> GetUsers(long roleId)
    {
        var data = await _roleService.GetUsersAsync(roleId);
        return Success(data);
    }

    [HttpPost("{roleId}/users")]
    public async Task<IActionResult> AssignUsers(long roleId, [FromBody] List<long> userIds)
    {
        return await ExecuteAsync(() => _roleService.BatchAssignUsersAsync(roleId, userIds, GetUserId()));
    }

    /// <summary>获取已删除（软删除）的角色列表</summary>
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted()
    {
        var data = await _roleService.GetDeletedAsync();
        return Success(data);
    }

    /// <summary>永久删除角色（从数据库中彻底移除）</summary>
    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(long id)
    {
        return await ExecuteAsync(() => _roleService.HardDeleteAsync(id, GetUserId()));
    }
}