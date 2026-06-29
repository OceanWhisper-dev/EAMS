using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize(Policy = "data-permission")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/data-permission")]
public class DataPermissionController : BaseController
{
    private readonly IDataPermissionService _dataPermissionService;
    private readonly IRoleService _roleService;

    public DataPermissionController(
        IDataPermissionService dataPermissionService,
        IRoleService roleService)
    {
        _dataPermissionService = dataPermissionService;
        _roleService = roleService;
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleService.GetAllAsync();
        return Success(roles);
    }

    [HttpGet("rules/{module}")]
    public async Task<IActionResult> GetRules(string module)
    {
        var rules = await _dataPermissionService.GetRulesByModuleAsync(module);
        return Success(rules);
    }

    [HttpPost("rules/{module}/save")]
    public async Task<IActionResult> SaveRule(string module, [FromBody] SaveRuleRequest request)
    {
        var ok = await _dataPermissionService.SaveRuleAsync(request.RoleId, module, request.DataScope, GetUserId());
        return ok ? Success("保存成功") : Fail("保存失败");
    }

    [HttpDelete("rules/{module}/{roleId}")]
    public async Task<IActionResult> DeleteRule(string module, long roleId)
    {
        var ok = await _dataPermissionService.DeleteRuleAsync(roleId, module, GetUserId());
        return ok ? Success("删除成功") : Fail("删除失败");
    }
}

public class SaveRuleRequest
{
    public long RoleId { get; set; }
    public DataScope DataScope { get; set; }
}