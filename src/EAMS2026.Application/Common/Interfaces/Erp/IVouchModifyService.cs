using EAMS2026.Domain.Common;
using EAMS2026.Domain.DTOs.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Entities.Erp;

namespace EAMS2026.Application.Common.Interfaces.Erp;

/// <summary>
/// ERP单据修改服务接口
/// </summary>
public interface IVouchModifyService
{
    // 查询
    Task<PagedResult<OrderDto>> QueryOrdersAsync(VouchQueryParam param);
    Task<PagedResult<DispatchDto>> QueryDispatchesAsync(VouchQueryParam param);
    Task<PagedResult<UnverifiedDispatchRow>> QueryUnverifiedDispatchesAsync(VouchQueryParam param);
    Task<DispatchDto?> GetDispatchByCodeAsync(string dlcode);

    // 修改
    Task<VouchModifyResult> UpdateOrderCustomerAsync(UpdateCustomerRequest request, long operatorId, string operatorName);
    Task<VouchModifyResult> UpdateDispatchCustomerAsync(UpdateCustomerRequest request, long operatorId, string operatorName);
    Task<VouchModifyResult> UpdateDispatchDateAsync(UpdateDateRequest request, long operatorId, string operatorName);
    Task<VouchModifyBatchResult> BatchUpdateDispatchDateAsync(BatchUpdateDateRequest request, long operatorId, string operatorName);

    // 客户参照
    Task<CustomerRefDto?> GetCustomerReferenceAsync(string code);

    /// <summary>检查销售订单是否有已审核的发货单</summary>
    Task<bool> HasVerifiedDispatchesAsync(int soid);

    // 日志查询
    Task<PagedResult<VouchModifyLog>> QueryLogsAsync(string? vouchType, long? operatorId, DateTime? from, DateTime? to, int page, int pageSize);

    // 业务员
    /// <summary>获取业务员列表（从U8 ERP）</summary>
    Task<List<SalespersonDto>> GetSalespersonsAsync();
}