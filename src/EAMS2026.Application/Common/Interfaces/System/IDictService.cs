using EAMS2026.Domain.Entities.System;

namespace EAMS2026.Application.Common.Interfaces.System;

public interface IDictService
{
    Task<IEnumerable<DictType>> GetTypesAsync();
    Task<DictType?> GetTypeByIdAsync(long id);
    Task<(bool Success, string Message)> CreateTypeAsync(DictType dictType, long operatorId);
    Task<(bool Success, string Message)> UpdateTypeAsync(DictType dictType, long operatorId);
    Task<(bool Success, string Message)> DeleteTypeAsync(long id, long operatorId);
    Task<IEnumerable<DictItem>> GetItemsAsync(long dictTypeId);
    Task<IEnumerable<DictItem>> GetItemsByCodeAsync(string code);
    Task<(bool Success, string Message)> CreateItemAsync(DictItem item, long operatorId);
    Task<(bool Success, string Message)> UpdateItemAsync(DictItem item, long operatorId);
    Task<(bool Success, string Message)> DeleteItemAsync(long id, long operatorId);
}