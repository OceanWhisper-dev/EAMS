using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.Extensions.Logging;

namespace EAMS2026.Application.Services.System;

public class DictService : IDictService
{
    private readonly IDictTypeRepository _dictTypeRepository;
    private readonly IDictItemRepository _dictItemRepository;
    private readonly IOperationLogRepository _logRepository;
    private readonly ILogger<DictService> _logger;

    public DictService(
        IDictTypeRepository dictTypeRepository,
        IDictItemRepository dictItemRepository,
        IOperationLogRepository logRepository,
        ILogger<DictService> logger)
    {
        _dictTypeRepository = dictTypeRepository;
        _dictItemRepository = dictItemRepository;
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<DictType>> GetTypesAsync()
    {
        return await _dictTypeRepository.GetAllAsync();
    }

    public async Task<DictType?> GetTypeByIdAsync(long id)
    {
        return await _dictTypeRepository.GetByIdAsync(id);
    }

    public async Task<(bool Success, string Message)> CreateTypeAsync(DictType dictType, long operatorId)
    {
        if (await _dictTypeRepository.IsCodeExistsAsync(dictType.Code))
            return (false, "字典编码已存在");

        dictType.CreatedBy = operatorId;
        dictType.UpdatedBy = operatorId;
        await _dictTypeRepository.AddAsync(dictType);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateTypeAsync(DictType dictType, long operatorId)
    {
        var existing = await _dictTypeRepository.GetByIdAsync(dictType.Id);
        if (existing == null)
            return (false, "字典类型不存在");

        if (await _dictTypeRepository.IsCodeExistsAsync(dictType.Code, dictType.Id))
            return (false, "字典编码已存在");

        dictType.UpdatedBy = operatorId;
        await _dictTypeRepository.UpdateAsync(dictType);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteTypeAsync(long id, long operatorId)
    {
        var dictType = await _dictTypeRepository.GetByIdAsync(id);
        if (dictType == null)
            return (false, "字典类型不存在");

        await _dictTypeRepository.SoftDeleteAsync(id);
        return (true, "删除成功");
    }

    public async Task<IEnumerable<DictItem>> GetItemsAsync(long dictTypeId)
    {
        return await _dictItemRepository.GetByTypeIdAsync(dictTypeId);
    }

    public async Task<IEnumerable<DictItem>> GetItemsByCodeAsync(string code)
    {
        return await _dictItemRepository.GetByTypeCodeAsync(code);
    }

    public async Task<(bool Success, string Message)> CreateItemAsync(DictItem item, long operatorId)
    {
        item.CreatedBy = operatorId;
        item.UpdatedBy = operatorId;
        await _dictItemRepository.AddAsync(item);
        return (true, "创建成功");
    }

    public async Task<(bool Success, string Message)> UpdateItemAsync(DictItem item, long operatorId)
    {
        item.UpdatedBy = operatorId;
        await _dictItemRepository.UpdateAsync(item);
        return (true, "更新成功");
    }

    public async Task<(bool Success, string Message)> DeleteItemAsync(long id, long operatorId)
    {
        await _dictItemRepository.SoftDeleteAsync(id);
        return (true, "删除成功");
    }
}