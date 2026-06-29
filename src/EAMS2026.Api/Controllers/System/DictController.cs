using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using EAMS2026.Domain.Entities.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize(Policy = "dict")]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
[Route("api/dict")]
public class DictController : BaseController
{
    private readonly IDictService _dictService;

    public DictController(IDictService dictService)
    {
        _dictService = dictService;
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetTypes()
    {
        var data = await _dictService.GetTypesAsync();
        return Success(data);
    }

    [HttpGet("types/{id}")]
    public async Task<IActionResult> GetTypeById(long id)
    {
        var data = await _dictService.GetTypeByIdAsync(id);
        if (data == null)
            return NotFound("字典类型不存在");
        return Success(data);
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreateType([FromBody] DictType dictType)
    {
        return await ExecuteAsync(() => _dictService.CreateTypeAsync(dictType, GetUserId()));
    }

    [HttpPut("types")]
    public async Task<IActionResult> UpdateType([FromBody] DictType dictType)
    {
        return await ExecuteAsync(() => _dictService.UpdateTypeAsync(dictType, GetUserId()));
    }

    [HttpDelete("types/{id}")]
    public async Task<IActionResult> DeleteType(long id)
    {
        return await ExecuteAsync(() => _dictService.DeleteTypeAsync(id, GetUserId()));
    }

    [HttpGet("types/{typeId}/items")]
    public async Task<IActionResult> GetItems(long typeId)
    {
        var data = await _dictService.GetItemsAsync(typeId);
        return Success(data);
    }

    [HttpGet("items/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItemsByCode(string code)
    {
        var data = await _dictService.GetItemsByCodeAsync(code);
        return Success(data);
    }

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] DictItem item)
    {
        return await ExecuteAsync(() => _dictService.CreateItemAsync(item, GetUserId()));
    }

    [HttpPut("items")]
    public async Task<IActionResult> UpdateItem([FromBody] DictItem item)
    {
        return await ExecuteAsync(() => _dictService.UpdateItemAsync(item, GetUserId()));
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteItem(long id)
    {
        return await ExecuteAsync(() => _dictService.DeleteItemAsync(id, GetUserId()));
    }
}