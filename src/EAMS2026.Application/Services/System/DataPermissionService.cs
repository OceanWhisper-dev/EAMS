using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Enums;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class DataPermissionService : IDataPermissionService
{
    private readonly IDataPermissionRepository _dataPermissionRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly ILogger<DataPermissionService> _logger;

    public DataPermissionService(
        IDataPermissionRepository dataPermissionRepository,
        IOperationLogRepository logRepository,
        ILogger<DataPermissionService> logger)
    {
        _dataPermissionRepository = dataPermissionRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<DataScope?> GetDataScopeAsync(long roleId, string module)
    {
        var rule = await _dataPermissionRepository.GetByRoleAndModuleAsync(roleId, module);
        return rule?.DataScope;
    }

    public async Task<IEnumerable<DataPermissionRule>> GetRulesByModuleAsync(string module)
    {
        return await _dataPermissionRepository.GetByModuleAsync(module);
    }

    public async Task<bool> SaveRuleAsync(long roleId, string module, DataScope dataScope, long operatorId)
    {
        var rule = new DataPermissionRule
        {
            RoleId = roleId,
            Module = module,
            DataScope = dataScope,
            CreatedBy = operatorId,
            UpdatedBy = operatorId
        };
        var result = await _dataPermissionRepository.SaveAsync(rule);

        if (result)
        {
            await _logRepository.AddAsync(new OperationLog
            {
                UserId = operatorId,
                OperationType = "Save",
                Module = "DataPermission",
                EntityType = "DataPermissionRule",
                Description = $"保存数据权限规则: 角色{roleId}, 模块{module}, 范围{dataScope}"
            });
        }

        return result;
    }

    public async Task<bool> DeleteRuleAsync(long roleId, string module, long operatorId)
    {
        var result = await _dataPermissionRepository.DeleteAsync(roleId, module);

        if (result)
        {
            await _logRepository.AddAsync(new OperationLog
            {
                UserId = operatorId,
                OperationType = "Delete",
                Module = "DataPermission",
                EntityType = "DataPermissionRule",
                Description = $"删除数据权限规则: 角色{roleId}, 模块{module}"
            });
        }

        return result;
    }
}