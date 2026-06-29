using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.Erp;

/// <summary>
/// 修改客户请求
/// </summary>
public class UpdateCustomerRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "订单ID或发货单ID至少需要提供一个")]
    public int? Soid { get; set; }

    public int? Dlid { get; set; }

    [Required(ErrorMessage = "新客户编码不能为空")]
    public string NewCusCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "新客户名称不能为空")]
    public string NewCusName { get; set; } = string.Empty;

    public string? OldCusCode { get; set; }

    public string? OldCusName { get; set; }

    public bool SyncDispatches { get; set; }
}