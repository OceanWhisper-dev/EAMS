using EAMS2026.Domain.DTOs.Erp.Report;
using EAMS2026.Domain.Entities.Erp.Report;

namespace EAMS2026.Domain.Interfaces.Repositories.Erp;

public interface IReportRepository
{
    // ===== 报表分类 =====
    Task<IEnumerable<ReportCategoryDto>> GetCategoriesAsync();
    Task<long> AddCategoryAsync(string name, long? parentId, int sortOrder, long operatorId);
    Task<bool> UpdateCategoryAsync(long id, string name, long? parentId, int sortOrder, long operatorId);
    Task<bool> DeleteCategoryAsync(long id);

    // ===== 报表 =====
    Task<IEnumerable<ReportDto>> GetReportsAsync(long? categoryId, long? userId = null);
    Task<ReportDetailDto?> GetReportDetailAsync(long id);
    Task<long> AddReportAsync(ReportDetailDto report, long operatorId);
    Task<bool> UpdateReportAsync(ReportDetailDto report, long operatorId);
    Task<bool> DeleteReportAsync(long id);
    Task<bool> UpdateReportStatusAsync(long id, string status);

    // ===== 报表收藏 =====
    Task<IEnumerable<ReportDto>> GetBookmarksAsync(long userId);
    Task<bool> AddBookmarkAsync(long userId, long reportId);
    Task<bool> RemoveBookmarkAsync(long userId, long reportId);

    // ===== 执行日志 =====
    Task<long> AddExecutionLogAsync(long reportId, long userId, string? paramsJson, int rowCount, int durationMs, bool isSuccess, string? errorMessage);

    // ===== 报表权限 =====
    Task<bool> HasPermissionAsync(long reportId, long userId, string accessType);
    Task<bool> SetPermissionAsync(long reportId, string principalType, long principalId, string accessType);
    Task<bool> RemovePermissionAsync(long reportId, string principalType, long principalId, string accessType);
    Task<IEnumerable<ReportPermission>> GetPermissionsAsync(long reportId);
    Task<bool> DeletePermissionByIdAsync(long permissionId);

    // ===== 数据源配置 =====
    Task<IEnumerable<ReportDataSourceDto>> GetDataSourcesAsync();
    Task<ReportDataSourceDto?> GetDataSourceAsync(long id);
    Task<ReportDataSourceDto?> GetDataSourceByNameAsync(string name);
    Task<long> AddDataSourceAsync(CreateDataSourceRequest request, long operatorId);
    Task<bool> UpdateDataSourceAsync(long id, UpdateDataSourceRequest request, long operatorId);
    Task<bool> DeleteDataSourceAsync(long id);
    Task<bool> TestConnectionAsync(string dbType, string connectionString);

    // ===== 透视表配置 =====
    Task<List<ReportPivotDto>> GetPivotViewsAsync(long reportId, long userId);
    Task<long> SavePivotViewAsync(SavePivotViewRequest request, long userId, string? creatorName = null);
    Task<bool> DeletePivotViewAsync(long id, long userId = 0);
    Task<bool> UpdatePivotViewLastFlagAsync(long id, bool isLast);

    // ===== 透视表共享 =====
    Task<bool> IsPivotViewOwnerAsync(long pivotViewId, long userId);
    Task<long?> GetSharePivotViewIdAsync(long shareId);
    Task<List<PivotViewShareDto>> GetSharesAsync(long pivotViewId);
    Task<bool> AddShareAsync(PivotViewShareRequest request);
    Task<bool> RemoveShareAsync(long shareId);

    // ===== 数据表视图配置 =====
    Task<List<ReportTableViewDto>> GetTableViewsAsync(long reportId, long userId);
    Task<long> SaveTableViewAsync(SaveTableViewRequest request, long userId, string? creatorName = null);
    Task<bool> DeleteTableViewAsync(long id, long userId = 0);
    Task<bool> UpdateTableViewLastFlagAsync(long id, bool isLast);

    // ===== 数据表视图共享 =====
    Task<bool> IsTableViewOwnerAsync(long viewId, long userId);
    Task<long?> GetTableViewShareViewIdAsync(long shareId);
    Task<List<TableViewShareDto>> GetTableViewSharesAsync(long viewId);
    Task<bool> AddTableViewShareAsync(TableViewShareRequest request);
    Task<bool> RemoveTableViewShareAsync(long shareId);
}