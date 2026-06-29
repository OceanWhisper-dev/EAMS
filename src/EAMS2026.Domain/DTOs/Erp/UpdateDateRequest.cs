using System.ComponentModel.DataAnnotations;

namespace EAMS2026.Domain.DTOs.Erp;

/// <summary>
/// 单笔修改发货日期请求
/// </summary>
public class UpdateDateRequest
{
    /// <summary>发货单号（单笔修改时使用）</summary>
    public string? DlCode { get; set; }

    /// <summary>发货单ID</summary>
    public int? Dlid { get; set; }

    /// <summary>新送货日期</summary>
    public DateTime NewDate { get; set; }
}

/// <summary>
/// 批量修改发货日期请求
/// </summary>
public class BatchUpdateDateRequest
{
    [Required(ErrorMessage = "发货单ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少需要选择一个发货单")]
    public List<int> Dlids { get; set; } = new();

    public DateTime NewDate { get; set; }

    public bool AutoCalculate { get; set; }
}