namespace EAMS2026.Domain.Interfaces;

public interface IDynamicDataEngine
{
    Task<object> ExecuteAsync(string dataSourceType, string? dataSourceConfig);
}
