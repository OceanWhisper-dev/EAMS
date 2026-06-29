using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IOperationLogRepository
{
    Task<long> AddAsync(OperationLog log);
    Task<OperationLog?> GetByIdAsync(long id);
    Task<IEnumerable<OperationLog>> GetPagedAsync(int page, int pageSize, long? userId = null, string? module = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<int> GetCountAsync(long? userId = null, string? module = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<int> ClearAsync(DateTime? beforeDate = null, long? userId = null);
}