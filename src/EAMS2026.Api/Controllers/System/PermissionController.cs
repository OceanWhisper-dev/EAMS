using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.DTOs.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize(Policy = "permission")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
[Route("api/permission")]
public class PermissionController : BaseController
{
    private readonly IPermissionService _permissionService;

    public PermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var data = await _permissionService.GetTreeAsync();
        return Success(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _permissionService.GetByIdAsync(id);
        if (data == null)
            return NotFound("权限不存在");
        return Success(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PermissionCreateRequest request)
    {
        return await ExecuteAsync(() => _permissionService.CreateAsync(request, GetUserId()));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] PermissionUpdateRequest request)
    {
        return await ExecuteAsync(() => _permissionService.UpdateAsync(request, GetUserId()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        return await ExecuteAsync(() => _permissionService.DeleteAsync(id, GetUserId()));
    }
}