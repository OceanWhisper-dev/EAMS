using EAMS2026.Domain.DTOs.Erp.Report;

namespace EAMS2026.Application.Common.Interfaces.Erp;

public interface IReportService
{
    // ===== 报表分类 =====
    Task<IEnumerable<ReportCategoryDto>> GetCategoriesAsync();
    Task<(bool Success, string Message)> AddCategoryAsync(string name, long? parentId, int sortOrder, long operatorId);
    Task<(bool Success, string Message)> UpdateCategoryAsync(long id, string name, long? parentId, int sortOrder, long operatorId);
    Task<(bool Success, string Message)> DeleteCategoryAsync(long id);

    // ===== 报表CRUD =====
    Task<IEnumerable<ReportDto>> GetReportsAsync(long? categoryId, long userId);
    Task<ReportDetailDto?> GetReportDetailAsync(long id);
    Task<(bool Success, string Message, long Id)> AddReportAsync(ReportDetailDto report, long operatorId);
    Task<(bool Success, string Message)> UpdateReportAsync(ReportDetailDto report, long operatorId);
    Task<(bool Success, string Message)> DeleteReportAsync(long id);
    Task<(bool Success, string Message)> UpdateReportStatusAsync(long id, string status);

    // ===== 报表执行 =====
    Task<ReportExecuteResult> ExecuteReportAsync(long reportId, ReportExecuteRequest request, long userId);
    Task<ReportExecuteResult> PreviewReportAsync(ReportPreviewRequest request);
    Task<byte[]> ExportReportAsync(long reportId, ReportExportRequest request, long userId);

    // ===== 收藏 =====
    Task<IEnumerable<ReportDto>> GetBookmarksAsync(long userId);
    Task<bool> ToggleBookmarkAsync(long userId, long reportId);

    // ===== 数据源配置 =====
    Task<IEnumerable<ReportDataSourceDto>> GetDataSourcesAsync();
    Task<ReportDataSourceDto?> GetDataSourceAsync(long id);
    Task<(bool Success, string Message, long Id)> AddDataSourceAsync(CreateDataSourceRequest request, long operatorId);
    Task<(bool Success, string Message)> UpdateDataSourceAsync(long id, UpdateDataSourceRequest request, long operatorId);
    Task<(bool Success, string Message)> DeleteDataSourceAsync(long id);
    Task<(bool Success, string Message)> TestConnectionAsync(string dbType, string connectionString);

    // ===== 业务员 =====
    /// <summary>获取业务员列表（从U8 Person表）</summary>
    Task<List<SalespersonDto>> GetSalespersonsAsync(string? datasource);
    /// <summary>获取当前用户的业务员映射信息</summary>
    Task<CurrentUserSalespersonDto> GetCurrentUserSalespersonInfoAsync(long userId);
    /// <summary>获取所有业务员映射</summary>
    Task<List<SalespersonMappingDto>> GetSalespersonMappingsAsync();
    /// <summary>保存业务员映射</summary>
    Task<(bool Success, string Message)> SaveSalespersonMappingAsync(SaveSalespersonMappingRequest request);
    /// <summary>删除业务员映射</summary>
    Task<(bool Success, string Message)> DeleteSalespersonMappingAsync(long employeeId);

    // ===== 报表权限 =====
    /// <summary>获取报表权限列表</summary>
    Task<List<ReportPermissionDto>> GetReportPermissionsAsync(long reportId);
    /// <summary>检查当前用户是否有指定权限</summary>
    Task<bool> CheckPermissionAsync(long reportId, long userId, string accessType);
    /// <summary>添加报表权限</summary>
    Task<(bool Success, string Message)> SetReportPermissionAsync(long reportId, SetReportPermissionRequest request);
    /// <summary>删除报表权限</summary>
    Task<(bool Success, string Message)> DeleteReportPermissionAsync(long permissionId);
    /// <summary>获取可选主体列表（用户和角色）</summary>
    Task<List<PrincipalOptionDto>> GetPrincipalOptionsAsync();

    // ===== 透视表配置 =====
    /// <summary>获取透视表配置列表（含共享给当前用户的）</summary>
    Task<List<ReportPivotDto>> GetPivotViewsAsync(long reportId, long userId);
    /// <summary>保存透视表配置</summary>
    Task<(bool Success, string Message, long Id)> SavePivotViewAsync(SavePivotViewRequest request, long userId, string? creatorName = null);
    /// <summary>删除透视表配置</summary>
    Task<(bool Success, string Message)> DeletePivotViewAsync(long id, long? userId = null);

    // ===== 透视表共享 =====
    /// <summary>获取共享目标列表</summary>
    Task<List<PivotViewShareDto>> GetSharesAsync(long pivotViewId, long? userId = null);
    /// <summary>添加共享目标</summary>
    Task<(bool Success, string Message)> AddShareAsync(PivotViewShareRequest request, long? userId = null);
    /// <summary>移除共享目标</summary>
    Task<(bool Success, string Message)> RemoveShareAsync(long shareId, long? userId = null);

    // ===== 数据表视图格式配置 =====
    /// <summary>获取数据表视图列表</summary>
    Task<List<ReportTableViewDto>> GetTableViewsAsync(long reportId, long userId);
    /// <summary>保存数据表视图</summary>
    Task<(bool Success, string Message, long Id)> SaveTableViewAsync(SaveTableViewRequest request, long userId, string? creatorName = null);
    /// <summary>删除数据表视图</summary>
    Task<(bool Success, string Message)> DeleteTableViewAsync(long id, long? userId = null);
    /// <summary>获取数据表视图共享目标列表</summary>
    Task<List<TableViewShareDto>> GetTableViewSharesAsync(long viewId, long? userId = null);
    /// <summary>添加数据表视图共享目标</summary>
    Task<(bool Success, string Message)> AddTableViewShareAsync(TableViewShareRequest request, long? userId = null);
    /// <summary>移除数据表视图共享目标</summary>
    Task<(bool Success, string Message)> RemoveTableViewShareAsync(long shareId, long? userId = null);

    // ===== 报表配置导入导出 =====
    /// <summary>导出所有报表配置（JSON）</summary>
    Task<byte[]> ExportReportsConfigAsync(long userId);
    /// <summary>导入报表配置（JSON）</summary>
    Task<(int SuccessCount, int FailCount, string Message)> ImportReportsConfigAsync(string reportsJson, long userId);
}