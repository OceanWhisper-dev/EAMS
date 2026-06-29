using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using EAMS2026.Domain.Entities.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize(Policy = "department")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/department")]
public class DepartmentController : BaseController
{
    private readonly IDepartmentService _departmentService;

    public DepartmentController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var data = await _departmentService.GetTreeAsync();
        return Success(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _departmentService.GetByIdAsync(id);
        if (data == null)
            return NotFound("部门不存在");
        return Success(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Department department)
    {
        return await ExecuteAsync(() => _departmentService.CreateAsync(department, GetUserId()));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Department department)
    {
        return await ExecuteAsync(() => _departmentService.UpdateAsync(department, GetUserId()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        return await ExecuteAsync(() => _departmentService.DeleteAsync(id, GetUserId()));
    }
}