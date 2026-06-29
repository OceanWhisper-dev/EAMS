using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Enums;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IDataPermissionService
{
    Task<DataScope?> GetDataScopeAsync(long roleId, string module);
    Task<IEnumerable<DataPermissionRule>> GetRulesByModuleAsync(string module);
    Task<bool> SaveRuleAsync(long roleId, string module, DataScope dataScope, long operatorId);
    Task<bool> DeleteRuleAsync(long roleId, string module, long operatorId);
}