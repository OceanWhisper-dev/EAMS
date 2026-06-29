using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.Erp;

/// <summary>
/// ERP 设置控制器 — 提供数据源配置、业务员映射等共享设置
/// 报表模块与单据修改模块共用此设置
/// </summary>
[Authorize]
[ApiController]
[Produces("application/json")]
[Route("api/erp/settings")]
public class ErpSettingsController : BaseController
{
    private readonly IReportService _reportService;

    public ErpSettingsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    // ==================== 数据源配置 ====================

    /// <summary>获取所有数据源</summary>
    [HttpGet("datasources")]
    public async Task<IActionResult> GetDataSources()
    {
        var data = await _reportService.GetDataSourcesAsync();
        return Success(data);
    }

    /// <summary>获取数据源详情</summary>
    [HttpGet("datasources/{id}")]
    public async Task<IActionResult> GetDataSource(long id)
    {
        var data = await _reportService.GetDataSourceAsync(id);
        return data != null ? Success(data) : Fail("数据源不存在");
    }

    /// <summary>创建数据源</summary>
    [HttpPost("datasources")]
    public async Task<IActionResult> AddDataSource([FromBody] CreateDataSourceRequest request)
    {
        var (ok, msg, id) = await _reportService.AddDataSourceAsync(request, GetUserId());
        return ok ? Success(new { id }, msg) : Fail(msg);
    }

    /// <summary>更新数据源</summary>
    [HttpPut("datasources/{id}")]
    public async Task<IActionResult> UpdateDataSource(long id, [FromBody] UpdateDataSourceRequest request)
    {
        var (ok, msg) = await _reportService.UpdateDataSourceAsync(id, request, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>删除数据源</summary>
    [HttpDelete("datasources/{id}")]
    public async Task<IActionResult> DeleteDataSource(long id)
    {
        var (ok, msg) = await _reportService.DeleteDataSourceAsync(id);
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>测试数据源连接</summary>
    [HttpPost("datasources/test")]
    public async Task<IActionResult> TestDataSource([FromBody] TestDataSourceRequest request)
    {
        var (ok, msg) = await _reportService.TestConnectionAsync(request.DbType, request.ConnectionString);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ==================== 业务员映射 ====================

    /// <summary>获取所有业务员映射</summary>
    [HttpGet("salespersons/mappings")]
    public async Task<IActionResult> GetSalespersonMappings()
    {
        var list = await _reportService.GetSalespersonMappingsAsync();
        return Success(list);
    }

    /// <summary>保存业务员映射</summary>
    [HttpPost("salespersons/mappings")]
    public async Task<IActionResult> SaveSalespersonMapping([FromBody] SaveSalespersonMappingRequest request)
    {
        var (ok, msg) = await _reportService.SaveSalespersonMappingAsync(request);
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>删除业务员映射</summary>
    [HttpDelete("salespersons/mappings/{employeeId}")]
    public async Task<IActionResult> DeleteSalespersonMapping(long employeeId)
    {
        var (ok, msg) = await _reportService.DeleteSalespersonMappingAsync(employeeId);
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>获取当前用户的业务员映射信息</summary>
    [HttpGet("salespersons/current")]
    public async Task<IActionResult> GetCurrentSalesperson()
    {
        var info = await _reportService.GetCurrentUserSalespersonInfoAsync(GetUserId());
        return Success(info);
    }

    /// <summary>获取业务员列表（从 erp_settings_salesperson_map 映射表，供下拉框使用）</summary>
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons()
    {
        var mappings = await _reportService.GetSalespersonMappingsAsync();
        var list = mappings
            .Select(m => new SalespersonDto
            {
                Code = m.SalespersonCode,
                Name = m.SalespersonName
            })
            .DistinctBy(m => m.Code)
            .ToList();
        return Success(list);
    }
}