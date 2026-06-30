using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Erp;
using EAMS2026.Domain.DTOs.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.Erp;

/// <summary>
/// ERP单据修改控制器
/// </summary>
[Route("api/erp/vouch-modify")]
[Authorize]
public class VouchModifyController : BaseController
{
    private readonly IVouchModifyService _service;

    public VouchModifyController(IVouchModifyService service)
    {
        _service = service;
    }

    // ==================== 查询 ====================

    /// <summary>查询销售订单</summary>
    [HttpGet("orders")]
    public async Task<IActionResult> QueryOrders([FromQuery] VouchQueryParam param)
    {
        var result = await _service.QueryOrdersAsync(param);
        return Success(result);
    }

    /// <summary>查询发货单</summary>
    [HttpGet("dispatches")]
    public async Task<IActionResult> QueryDispatches([FromQuery] VouchQueryParam param)
    {
        var result = await _service.QueryDispatchesAsync(param);
        return Success(result);
    }

    /// <summary>查询未审核发货单（用于日期批量修改）</summary>
    [HttpGet("unverified-dispatches")]
    public async Task<IActionResult> QueryUnverifiedDispatches([FromQuery] VouchQueryParam param)
    {
        var result = await _service.QueryUnverifiedDispatchesAsync(param);
        return Success(result);
    }

    /// <summary>按单号查询发货单详情</summary>
    [HttpGet("dispatch/{dlcode}")]
    public async Task<IActionResult> GetDispatch(string dlcode)
    {
        var dispatch = await _service.GetDispatchByCodeAsync(dlcode);
        return dispatch == null ? Fail("发货单不存在") : Success(dispatch);
    }

    /// <summary>检查销售订单是否有已审核的发货单</summary>
    [HttpGet("orders/{soid}/has-verified-dispatches")]
    public async Task<IActionResult> HasVerifiedDispatches(int soid)
    {
        var hasVerified = await _service.HasVerifiedDispatchesAsync(soid);
        return Success(new { hasVerified });
    }

    // ==================== 修改 ====================

    /// <summary>修改订单客户</summary>
    [HttpPut("order/customer")]
    public async Task<IActionResult> UpdateOrderCustomer([FromBody] UpdateCustomerRequest request)
    {
        var result = await _service.UpdateOrderCustomerAsync(request, GetUserId(), GetUsername());
        return result.Success ? Success(result.Message ?? string.Empty) : Fail(result.Message ?? string.Empty);
    }

    /// <summary>修改发货单客户</summary>
    [HttpPut("dispatch/customer")]
    public async Task<IActionResult> UpdateDispatchCustomer([FromBody] UpdateCustomerRequest request)
    {
        var result = await _service.UpdateDispatchCustomerAsync(request, GetUserId(), GetUsername());
        return result.Success ? Success(result.Message ?? string.Empty) : Fail(result.Message ?? string.Empty);
    }

    /// <summary>修改单笔发货单日期</summary>
    [HttpPut("dispatch/date")]
    public async Task<IActionResult> UpdateDispatchDate([FromBody] UpdateDateRequest request)
    {
        var result = await _service.UpdateDispatchDateAsync(request, GetUserId(), GetUsername());
        return result.Success ? Success(result.Message ?? string.Empty) : Fail(result.Message ?? string.Empty);
    }

    /// <summary>批量修改发货单日期</summary>
    [HttpPut("dispatch/date/batch")]
    public async Task<IActionResult> BatchUpdateDispatchDate([FromBody] BatchUpdateDateRequest request)
    {
        var result = await _service.BatchUpdateDispatchDateAsync(request, GetUserId(), GetUsername());
        return Success(result);
    }

    /// <summary>批量修改发货单客户</summary>
    [HttpPut("dispatch/customer/batch")]
    public async Task<IActionResult> BatchUpdateDispatchCustomer([FromBody] BatchUpdateCustomerRequest request)
    {
        var result = await _service.BatchUpdateDispatchCustomerAsync(request, GetUserId(), GetUsername());
        return Success(result);
    }

    // ==================== 客户参照 ====================

    /// <summary>客户参照（验证客户编码是否存在）</summary>
    [HttpGet("customer-ref/{code}")]
    public async Task<IActionResult> CustomerRef(string code)
    {
        var customer = await _service.GetCustomerReferenceAsync(code);
        return customer == null ? Fail("客户不存在") : Success(customer);
    }

    // ==================== 日志查询 ====================

    /// <summary>查询操作日志</summary>
    [HttpGet("logs")]
    public async Task<IActionResult> QueryLogs(
        [FromQuery] string? vouchType,
        [FromQuery] long? operatorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.QueryLogsAsync(vouchType, operatorId, from, to, page, pageSize);
        return Success(result);
    }

    // ==================== 业务员 ====================

    /// <summary>获取业务员列表（从U8 ERP）</summary>
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons()
    {
        var list = await _service.GetSalespersonsAsync();
        return Success(list);
    }
}