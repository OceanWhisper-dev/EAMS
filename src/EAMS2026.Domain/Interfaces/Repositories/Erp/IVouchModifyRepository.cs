using EAMS2026.Domain.DTOs.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;

namespace EAMS2026.Domain.Interfaces.Repositories;

/// <summary>
/// ERP单据修改仓储接口
/// </summary>
public interface IVouchModifyRepository
{
    /// <summary>查询销售订单（含关联发货单，未审核）</summary>
    Task<(IEnumerable<OrderDto> Orders, int Total)> QueryOrdersAsync(VouchQueryParam param);

    /// <summary>查询发货单</summary>
    Task<(IEnumerable<DispatchDto> Dispatches, int Total)> QueryDispatchesAsync(VouchQueryParam param);

    /// <summary>查询未审核发货单（用于日期批量修改）</summary>
    Task<(IEnumerable<UnverifiedDispatchRow> Rows, int Total)> QueryUnverifiedDispatchesAsync(VouchQueryParam param);

    /// <summary>按单号查询发货单详情</summary>
    Task<DispatchDto?> GetDispatchByCodeAsync(string dlcode);

    /// <summary>修改订单客户</summary>
    Task<bool> UpdateOrderCustomerAsync(int soid, string newCusCode, string newCusName);

    /// <summary>修改发货单客户</summary>
    Task<bool> UpdateDispatchCustomerAsync(int dlid, string newCusCode, string newCusName);

    /// <summary>修改单笔发货单日期</summary>
    Task<bool> UpdateDispatchDateAsync(int dlid, DateTime newDate);

    /// <summary>客户参照</summary>
    Task<CustomerRefDto?> GetCustomerReferenceAsync(string code);

    /// <summary>检查发货单是否为未审核状态</summary>
    Task<bool> IsDispatchUnverifiedAsync(int dlid);

    /// <summary>检查销售订单是否有关联的已审核发货单</summary>
    Task<bool> HasVerifiedDispatchesAsync(int soid);

    /// <summary>获取订单关联的发货单ID列表</summary>
    Task<IEnumerable<int>> GetDispatchIdsByOrderIdAsync(int soid);

    /// <summary>获取业务员列表（从U8 Person表）</summary>
    Task<List<SalespersonDto>> GetSalespersonsAsync();
}