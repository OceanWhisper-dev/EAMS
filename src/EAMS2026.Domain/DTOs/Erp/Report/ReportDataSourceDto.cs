using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.Erp.Report;

/// <summary>
/// 数据源配置 DTO
/// </summary>
public class ReportDataSourceDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DbType { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 创建数据源请求
/// </summary>
public class CreateDataSourceRequest
{
    [Required(ErrorMessage = "数据源名称不能为空")]
    public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "显示名称不能为空")]
    public string DisplayName { get; set; } = string.Empty;
    [Required(ErrorMessage = "数据库类型不能为空")]
    public string DbType { get; set; } = "postgresql";
    [Required(ErrorMessage = "连接字符串不能为空")]
    public string ConnectionString { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 更新数据源请求
/// </summary>
public class UpdateDataSourceRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string DbType { get; set; } = "postgresql";
    [Required(ErrorMessage = "连接字符串不能为空")]
    public string ConnectionString { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 测试数据源连接请求
/// </summary>
public class TestDataSourceRequest
{
    [Required(ErrorMessage = "数据库类型不能为空")]
    public string DbType { get; set; } = "postgresql";
    [Required(ErrorMessage = "连接字符串不能为空")]
    public string ConnectionString { get; set; } = string.Empty;
}