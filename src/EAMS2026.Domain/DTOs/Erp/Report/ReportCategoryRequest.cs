using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.Erp.Report;

/// <summary>
/// 添加报表分类请求
/// </summary>
public class AddCategoryRequest
{
    [Required(ErrorMessage = "分类名称不能为空")]
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// 更新报表状态请求
/// </summary>
public class UpdateStatusRequest
{
    [Required(ErrorMessage = "状态不能为空")]
    public string Status { get; set; } = "published";
}