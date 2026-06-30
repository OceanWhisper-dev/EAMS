using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.Erp;

/// <summary>
/// 批量修改发货单客户请求
/// </summary>
public class BatchUpdateCustomerRequest
{
    [Required(ErrorMessage = "发货单ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个发货单")]
    public List<int> Dlids { get; set; } = new();

    [Required(ErrorMessage = "新客户编码不能为空")]
    public string NewCusCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "新客户名称不能为空")]
    public string NewCusName { get; set; } = string.Empty;

    public string? OldCusCode { get; set; }

    public string? OldCusName { get; set; }
}
