using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Domain.Interfaces.Repositories.System;

public interface IDictItemRepository : IBaseRepository<DictItem>
{
    Task<IEnumerable<DictItem>> GetByTypeIdAsync(long dictTypeId);
    Task<IEnumerable<DictItem>> GetByTypeCodeAsync(string code);
    Task<DictItem?> GetByTypeCodeAndValueAsync(string code, string value);
}