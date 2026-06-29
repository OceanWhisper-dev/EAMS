using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.Erp.Report;

/// <summary>
/// 报表导出请求
/// </summary>
public class ReportExportRequest
{
    [Required(ErrorMessage = "导出格式不能为空")]
    public string Format { get; set; } = "xlsx"; // xlsx / csv / pdf
    public Dictionary<string, object?> Params { get; set; } = new();
}

/// <summary>
/// 报表预览请求（设计时测试SQL）
/// </summary>
public class ReportPreviewRequest
{
    [Required(ErrorMessage = "SQL查询文本不能为空")]
    public string QueryText { get; set; } = string.Empty;
    [Required(ErrorMessage = "查询类型不能为空")]
    public string QueryType { get; set; } = "sql";  // sql / proc
    public Dictionary<string, object?> Params { get; set; } = new();
    public string QueryDatasource { get; set; } = "main";
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public List<ReportFilterDto>? Filters { get; set; }
    public List<ReportFieldDto>? Fields { get; set; }
}

/// <summary>
/// 报表分类DTO
/// </summary>
public class ReportCategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }
    public int ReportCount { get; set; }
    public List<ReportCategoryDto> Children { get; set; } = new();
}

/// <summary>
/// 收藏操作
/// </summary>
public class ReportBookmarkRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "报表ID无效")]
    public long ReportId { get; set; }
}

/// <summary>
/// 业务员信息
/// </summary>
public class SalespersonDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 当前用户业务员信息
/// </summary>
public class CurrentUserSalespersonDto
{
    /// <summary>是否业务员（有映射记录）</summary>
    public bool IsSalesperson { get; set; }
    /// <summary>业务员编码</summary>
    public string? SalespersonCode { get; set; }
    /// <summary>业务员名称</summary>
    public string? SalespersonName { get; set; }
    /// <summary>类型：salesperson=业务员（受限），supervisor=主管（不受限），null=默认受限</summary>
    public string? Type { get; set; }
}

/// <summary>
/// 业务员映射记录
/// </summary>
public class SalespersonMappingDto
{
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeNo { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string SalespersonCode { get; set; } = string.Empty;
    public string SalespersonName { get; set; } = string.Empty;
    public string Type { get; set; } = "salesperson";
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 保存业务员映射请求
/// </summary>
public class SaveSalespersonMappingRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "员工ID无效")]
    public long EmployeeId { get; set; }
    [Required(ErrorMessage = "业务员编码不能为空")]
    public string SalespersonCode { get; set; } = string.Empty;
    public string SalespersonName { get; set; } = string.Empty;
    public string Type { get; set; } = "salesperson";
}

/// <summary>
/// 报表权限 DTO
/// </summary>
public class ReportPermissionDto
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string PrincipalType { get; set; } = string.Empty; // role / user
    public long PrincipalId { get; set; }
    /// <summary>主体名称（用户名/角色名）</summary>
    public string PrincipalName { get; set; } = string.Empty;
    public string AccessType { get; set; } = string.Empty;    // view / export / manage
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 设置报表权限请求
/// </summary>
public class SetReportPermissionRequest
{
    [Required(ErrorMessage = "主体类型不能为空")]
    public string PrincipalType { get; set; } = string.Empty; // role / user
    [Range(1, long.MaxValue, ErrorMessage = "主体ID无效")]
    public long PrincipalId { get; set; }
    [Required(ErrorMessage = "权限类型不能为空")]
    public string AccessType { get; set; } = "view";          // view / export / manage
}

/// <summary>
/// 用户/角色选择器 DTO（用于权限管理下拉框）
/// </summary>
public class PrincipalOptionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // role / user
}

/// <summary>
/// 透视表配置 DTO
/// </summary>
public class ReportPivotDto
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public long UserId { get; set; }
    public string PivotName { get; set; } = string.Empty;
    public string PivotParams { get; set; } = string.Empty;
    public bool IsLast { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 保存透视表配置请求
/// </summary>
public class SavePivotViewRequest
{
    public long? Id { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "报表ID无效")]
    public long ReportId { get; set; }
    [Required(ErrorMessage = "透视表名称不能为空")]
    public string PivotName { get; set; } = string.Empty;
    [Required(ErrorMessage = "透视表参数不能为空")]
    public string PivotParams { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsLast { get; set; }
    public string? CreatorName { get; set; }
}

/// <summary>
/// 透视表共享目标 DTO
/// </summary>
public class PivotViewShareDto
{
    public long Id { get; set; }
    public long PivotViewId { get; set; }
    public string TargetType { get; set; } = string.Empty; // user / role
    public long TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 透视表共享请求
/// </summary>
public class PivotViewShareRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "透视表视图ID无效")]
    public long PivotViewId { get; set; }
    [Required(ErrorMessage = "共享类型不能为空")]
    public string TargetType { get; set; } = string.Empty; // user / role
    [Range(1, long.MaxValue, ErrorMessage = "共享目标ID无效")]
    public long TargetId { get; set; }
}

/// <summary>
/// 透视表共享目标请求（前端传入，不含 pivotViewId）
/// </summary>
public class PivotViewShareTargetRequest
{
    [Required(ErrorMessage = "共享类型不能为空")]
    public string TargetType { get; set; } = string.Empty; // user / role
    [Range(1, long.MaxValue, ErrorMessage = "共享目标ID无效")]
    public long TargetId { get; set; }
}

// ========== 数据表视图配置（类似透视表样式保存） ==========

/// <summary>
/// 数据表视图配置 DTO
/// </summary>
public class ReportTableViewDto
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public long UserId { get; set; }
    public string ViewName { get; set; } = string.Empty;
    public string ViewParams { get; set; } = string.Empty;
    public bool IsLast { get; set; }
    public bool IsDefault { get; set; }
    public bool IsShared { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 保存数据表视图配置请求
/// </summary>
public class SaveTableViewRequest
{
    public long? Id { get; set; }
    [Range(1, long.MaxValue, ErrorMessage = "报表ID无效")]
    public long ReportId { get; set; }
    [Required(ErrorMessage = "视图名称不能为空")]
    public string ViewName { get; set; } = string.Empty;
    [Required(ErrorMessage = "视图参数不能为空")]
    public string ViewParams { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsLast { get; set; }
    public string? CreatorName { get; set; }
}

/// <summary>
/// 数据表视图共享目标 DTO
/// </summary>
public class TableViewShareDto
{
    public long Id { get; set; }
    public long ViewId { get; set; }
    public string TargetType { get; set; } = string.Empty; // user / role
    public long TargetId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 数据表视图共享请求
/// </summary>
public class TableViewShareRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "视图ID无效")]
    public long ViewId { get; set; }
    [Required(ErrorMessage = "共享类型不能为空")]
    public string TargetType { get; set; } = string.Empty; // user / role
    [Range(1, long.MaxValue, ErrorMessage = "共享目标ID无效")]
    public long TargetId { get; set; }
}