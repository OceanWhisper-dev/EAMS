using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using EAMS2026.Domain.Entities.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize(Policy = "employee")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
[Route("api/employee")]
public class EmployeeController : BaseController
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _employeeService.GetAllAsync();
        return Success(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _employeeService.GetByIdAsync(id);
        if (data == null)
            return NotFound("员工不存在");
        return Success(data);
    }

    [HttpGet("by-department/{departmentId}")]
    public async Task<IActionResult> GetByDepartmentId(long departmentId)
    {
        var data = await _employeeService.GetByDepartmentIdAsync(departmentId);
        return Success(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Employee employee)
    {
        return await ExecuteAsync(() => _employeeService.CreateAsync(employee, GetUserId()));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Employee employee)
    {
        return await ExecuteAsync(() => _employeeService.UpdateAsync(employee, GetUserId()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        return await ExecuteAsync(() => _employeeService.DeleteAsync(id, GetUserId()));
    }

    /// <summary>获取已删除（软删除）的员工列表</summary>
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted()
    {
        var data = await _employeeService.GetDeletedAsync();
        return Success(data);
    }

    /// <summary>永久删除员工（从数据库中彻底移除）</summary>
    [HttpDelete("{id}/hard")]
    public async Task<IActionResult> HardDelete(long id)
    {
        return await ExecuteAsync(() => _employeeService.HardDeleteAsync(id, GetUserId()));
    }
}