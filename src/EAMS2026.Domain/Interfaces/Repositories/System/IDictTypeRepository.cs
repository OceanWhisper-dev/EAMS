using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IDictTypeRepository : IBaseRepository<DictType>
{
    Task<DictType?> GetByCodeAsync(string code);
    Task<bool> IsCodeExistsAsync(string code, long? excludeId = null);
}