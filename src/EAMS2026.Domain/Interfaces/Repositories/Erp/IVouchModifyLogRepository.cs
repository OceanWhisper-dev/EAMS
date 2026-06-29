using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Entities.Erp;
using EAMS2026.Domain.Common;

namespace EAMS2026.Domain.Interfaces.Repositories;

/// <summary>
/// 单据修改日志仓储接口
/// </summary>
public interface IVouchModifyLogRepository
{
    Task<long> AddAsync(VouchModifyLog log);
    Task<PagedResult<VouchModifyLog>> QueryAsync(string? vouchType = null, long? operatorId = null,
        DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取已映射的业务员列表（从 erp_settings_salesperson_map 读取，共享报表模块的数据）
    /// </summary>
    Task<List<SalespersonDto>> GetMappedSalespersonsAsync();
}