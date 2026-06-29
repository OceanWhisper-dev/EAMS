using EAMS2026.Application.Common.Interfaces.Erp;
using EAMS2026.Domain.DTOs.Erp.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace EAMS2026.Api.Controllers.Erp;

[Authorize(Policy = "erp-report")]
[ApiController]
[Produces("application/json")]
[Route("api/reports")]
public class ReportController : BaseController
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    // ========== 报表分类 ==========

    /// <summary>获取分类树</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var data = await _reportService.GetCategoriesAsync();
        return Success(data);
    }

    /// <summary>创建分类</summary>
    [HttpPost("categories")]
    public async Task<IActionResult> AddCategory([FromBody] AddCategoryRequest request)
    {
        var (ok, msg) = await _reportService.AddCategoryAsync(request.Name, request.ParentId, request.SortOrder, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>更新分类</summary>
    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(long id, [FromBody] AddCategoryRequest request)
    {
        var (ok, msg) = await _reportService.UpdateCategoryAsync(id, request.Name, request.ParentId, request.SortOrder, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>删除分类</summary>
    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(long id)
    {
        var (ok, msg) = await _reportService.DeleteCategoryAsync(id);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 报表CRUD ==========

    /// <summary>获取报表列表</summary>
    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] long? categoryId)
    {
        var data = await _reportService.GetReportsAsync(categoryId, GetUserId());
        return Success(data);
    }

    /// <summary>获取报表详情（含字段/过滤/排序/图表配置）</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetReportDetail(long id)
    {
        var data = await _reportService.GetReportDetailAsync(id);
        return data != null ? Success(data) : Fail("报表不存在");
    }

    /// <summary>创建报表</summary>
    [HttpPost]
    public async Task<IActionResult> AddReport([FromBody] ReportDetailDto report)
    {
        var (ok, msg, id) = await _reportService.AddReportAsync(report, GetUserId());
        return ok ? Success(new { id }, msg) : Fail(msg);
    }

    /// <summary>更新报表</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReport(long id, [FromBody] ReportDetailDto report)
    {
        report.Id = id;
        var (ok, msg) = await _reportService.UpdateReportAsync(report, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>删除报表</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReport(long id)
    {
        var (ok, msg) = await _reportService.DeleteReportAsync(id);
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>更新报表状态</summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusRequest request)
    {
        var (ok, msg) = await _reportService.UpdateReportStatusAsync(id, request.Status);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 报表执行 ==========

    /// <summary>执行报表查询</summary>
    [HttpPost("{id}/execute")]
    public async Task<IActionResult> ExecuteReport(long id, [FromBody] ReportExecuteRequest request)
    {
        try
        {
            var result = await _reportService.ExecuteReportAsync(id, request, GetUserId());
            return Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
    }

    /// <summary>预览SQL（设计时测试）</summary>
    [HttpPost("preview")]
    public async Task<IActionResult> PreviewReport([FromBody] ReportPreviewRequest request)
    {
        try
        {
            var result = await _reportService.PreviewReportAsync(request);
            return Success(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Fail($"查询失败: {ex.Message}");
        }
    }

    // ========== 导出 ==========

    /// <summary>导出报表数据</summary>
    [HttpPost("{id}/export")]
    public async Task<IActionResult> ExportReport(long id, [FromBody] ReportExportRequest request)
    {
        try
        {
            var data = await _reportService.ExportReportAsync(id, request, GetUserId());
            var fileName = $"report_{id}.{request.Format}";
            var contentType = request.Format.ToLower() switch
            {
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "csv" => "text/csv",
                _ => "application/octet-stream"
            };
            return File(data, contentType, fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Fail($"导出失败: {ex.Message}");
        }
    }

    // ========== 收藏 ==========

    /// <summary>获取收藏列表</summary>
    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks()
    {
        var data = await _reportService.GetBookmarksAsync(GetUserId());
        return Success(data);
    }

    /// <summary>切换收藏状态</summary>
    [HttpPost("bookmarks/{reportId}")]
    public async Task<IActionResult> ToggleBookmark(long reportId)
    {
        var ok = await _reportService.ToggleBookmarkAsync(GetUserId(), reportId);
        return Success(new { bookmarked = ok });
    }

    // ========== 数据源配置 ==========

    /// <summary>获取所有数据源</summary>
    [HttpGet("datasources")]
    public async Task<IActionResult> GetDataSources()
    {
        var data = await _reportService.GetDataSourcesAsync();
        return Success(data);
    }

    /// <summary>获取数据源详情</summary>
    [HttpGet("datasources/{id}")]
    public async Task<IActionResult> GetDataSource(long id)
    {
        var data = await _reportService.GetDataSourceAsync(id);
        return data != null ? Success(data) : Fail("数据源不存在");
    }

    /// <summary>创建数据源</summary>
    [HttpPost("datasources")]
    public async Task<IActionResult> AddDataSource([FromBody] CreateDataSourceRequest request)
    {
        var (ok, msg, id) = await _reportService.AddDataSourceAsync(request, GetUserId());
        return ok ? Success(new { id }, msg) : Fail(msg);
    }

    /// <summary>更新数据源</summary>
    [HttpPut("datasources/{id}")]
    public async Task<IActionResult> UpdateDataSource(long id, [FromBody] UpdateDataSourceRequest request)
    {
        var (ok, msg) = await _reportService.UpdateDataSourceAsync(id, request, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>删除数据源</summary>
    [HttpDelete("datasources/{id}")]
    public async Task<IActionResult> DeleteDataSource(long id)
    {
        var (ok, msg) = await _reportService.DeleteDataSourceAsync(id);
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>测试数据源连接</summary>
    [HttpPost("datasources/test")]
    public async Task<IActionResult> TestDataSource([FromBody] TestDataSourceRequest request)
    {
        var (ok, msg) = await _reportService.TestConnectionAsync(request.DbType, request.ConnectionString);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 业务员 ==========

    /// <summary>获取业务员列表（从ERP数据源）</summary>
    [HttpGet("salespersons")]
    public async Task<IActionResult> GetSalespersons([FromQuery] string? datasource)
    {
        var list = await _reportService.GetSalespersonsAsync(datasource);
        return Success(list);
    }

    /// <summary>获取当前用户的业务员映射信息</summary>
    [HttpGet("salespersons/current")]
    public async Task<IActionResult> GetCurrentSalesperson()
    {
        var info = await _reportService.GetCurrentUserSalespersonInfoAsync(GetUserId());
        return Success(info);
    }

    /// <summary>获取所有业务员映射</summary>
    [HttpGet("salespersons/mappings")]
    public async Task<IActionResult> GetSalespersonMappings()
    {
        var list = await _reportService.GetSalespersonMappingsAsync();
        return Success(list);
    }

    /// <summary>保存业务员映射</summary>
    [HttpPost("salespersons/mappings")]
    public async Task<IActionResult> SaveSalespersonMapping([FromBody] SaveSalespersonMappingRequest request)
    {
        var (ok, msg) = await _reportService.SaveSalespersonMappingAsync(request);
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>删除业务员映射</summary>
    [HttpDelete("salespersons/mappings/{employeeId}")]
    public async Task<IActionResult> DeleteSalespersonMapping(long employeeId)
    {
        var (ok, msg) = await _reportService.DeleteSalespersonMappingAsync(employeeId);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 报表权限 ==========

    /// <summary>获取报表权限列表</summary>
    [HttpGet("{id}/permissions")]
    public async Task<IActionResult> GetReportPermissions(long id)
    {
        var list = await _reportService.GetReportPermissionsAsync(id);
        return Success(list);
    }

    /// <summary>检查当前用户是否有指定权限</summary>
    [HttpGet("{id}/check-permission")]
    public async Task<IActionResult> CheckPermission(long id, [FromQuery] string accessType = "manage")
    {
        var userId = GetUserId();
        var hasPermission = await _reportService.CheckPermissionAsync(id, userId, accessType);
        return Success(new { hasPermission });
    }

    /// <summary>获取可选主体列表</summary>
    [HttpGet("permissions/principals")]
    public async Task<IActionResult> GetPrincipalOptions()
    {
        var list = await _reportService.GetPrincipalOptionsAsync();
        return Success(list);
    }

    /// <summary>添加报表权限</summary>
    [HttpPost("{id}/permissions")]
    public async Task<IActionResult> SetReportPermission(long id, [FromBody] SetReportPermissionRequest request)
    {
        var (ok, msg) = await _reportService.SetReportPermissionAsync(id, request);
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>删除报表权限</summary>
    [HttpDelete("permissions/{permissionId}")]
    public async Task<IActionResult> DeleteReportPermission(long permissionId)
    {
        var (ok, msg) = await _reportService.DeleteReportPermissionAsync(permissionId);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 透视表配置 ==========

    /// <summary>获取透视表配置列表（含共享给当前用户的）</summary>
    [HttpGet("{id}/pivots")]
    public async Task<IActionResult> GetPivotViews(long id)
    {
        var list = await _reportService.GetPivotViewsAsync(id, GetUserId());
        return Success(list);
    }

    /// <summary>保存透视表配置</summary>
    [HttpPost("pivots")]
    public async Task<IActionResult> SavePivotView([FromBody] SavePivotViewRequest request)
    {
        var (ok, msg, pivotId) = await _reportService.SavePivotViewAsync(request, GetUserId());
        return ok ? Success(new { id = pivotId }, msg) : Fail(msg);
    }

    /// <summary>删除透视表配置</summary>
    [HttpDelete("pivots/{id}")]
    public async Task<IActionResult> DeletePivotView(long id)
    {
        var userId = GetUserId();
        var (ok, msg) = await _reportService.DeletePivotViewAsync(id, userId);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 透视表共享管理 ==========

    /// <summary>获取共享目标列表</summary>
    [HttpGet("pivots/{pivotViewId}/shares")]
    public async Task<IActionResult> GetShares(long pivotViewId)
    {
        var list = await _reportService.GetSharesAsync(pivotViewId, GetUserId());
        return Success(list);
    }

    /// <summary>添加共享目标（指定用户或角色）</summary>
    [HttpPost("pivots/{pivotViewId}/shares")]
    public async Task<IActionResult> AddShare(long pivotViewId, [FromBody] PivotViewShareTargetRequest request)
    {
        var req = new PivotViewShareRequest
        {
            PivotViewId = pivotViewId,
            TargetType = request.TargetType,
            TargetId = request.TargetId
        };
        var (ok, msg) = await _reportService.AddShareAsync(req, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>移除共享目标</summary>
    [HttpDelete("pivots/{pivotViewId}/shares/{shareId}")]
    public async Task<IActionResult> RemoveShare(long pivotViewId, long shareId)
    {
        var (ok, msg) = await _reportService.RemoveShareAsync(shareId, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 数据表视图格式配置（类似透视表样式保存） ==========

    /// <summary>获取数据表视图列表（含共享给当前用户的）</summary>
    [HttpGet("{id}/table-views")]
    public async Task<IActionResult> GetTableViews(long id)
    {
        var list = await _reportService.GetTableViewsAsync(id, GetUserId());
        return Success(list);
    }

    /// <summary>保存数据表视图</summary>
    [HttpPost("table-views")]
    public async Task<IActionResult> SaveTableView([FromBody] SaveTableViewRequest request)
    {
        var (ok, msg, viewId) = await _reportService.SaveTableViewAsync(request, GetUserId());
        return ok ? Success(new { id = viewId }, msg) : Fail(msg);
    }

    /// <summary>删除数据表视图</summary>
    [HttpDelete("table-views/{id}")]
    public async Task<IActionResult> DeleteTableView(long id)
    {
        var userId = GetUserId();
        var (ok, msg) = await _reportService.DeleteTableViewAsync(id, userId);
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 数据表视图共享管理 ==========

    /// <summary>获取数据表视图共享目标列表</summary>
    [HttpGet("table-views/{viewId}/shares")]
    public async Task<IActionResult> GetTableViewShares(long viewId)
    {
        var list = await _reportService.GetTableViewSharesAsync(viewId, GetUserId());
        return Success(list);
    }

    /// <summary>添加数据表视图共享目标</summary>
    [HttpPost("table-views/{viewId}/shares")]
    public async Task<IActionResult> AddTableViewShare(long viewId, [FromBody] PivotViewShareTargetRequest request)
    {
        var req = new TableViewShareRequest
        {
            ViewId = viewId,
            TargetType = request.TargetType,
            TargetId = request.TargetId
        };
        var (ok, msg) = await _reportService.AddTableViewShareAsync(req, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    /// <summary>移除数据表视图共享目标</summary>
    [HttpDelete("table-views/{viewId}/shares/{shareId}")]
    public async Task<IActionResult> RemoveTableViewShare(long viewId, long shareId)
    {
        var (ok, msg) = await _reportService.RemoveTableViewShareAsync(shareId, GetUserId());
        return ok ? Success(null, msg) : Fail(msg);
    }

    // ========== 报表配置导入导出（仅管理员） ==========

    /// <summary>导出全部报表配置为 JSON</summary>
    [HttpGet("export-config")]
    public async Task<IActionResult> ExportReportsConfig()
    {
        try
        {
            var data = await _reportService.ExportReportsConfigAsync(GetUserId());
            return File(data, "application/json", "report-config.json");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
    }

    /// <summary>导入报表配置（上传 JSON 文件）</summary>
    [HttpPost("import-config")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    public async Task<IActionResult> ImportReportsConfig(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Fail("请选择要导入的 JSON 文件");

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return Fail("仅支持 .json 格式文件");

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var json = await reader.ReadToEndAsync();
            var (success, fail, msg) = await _reportService.ImportReportsConfigAsync(json, GetUserId());
            return Success(new { successCount = success, failCount = fail }, msg);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Fail($"导入失败: {ex.Message}");
        }
    }
}

