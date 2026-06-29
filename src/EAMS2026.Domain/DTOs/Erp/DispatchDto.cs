namespace EAMS2026.Domain.DTOs.Erp;

/// <summary>
/// 发货单DTO（聚合结构：发货单主表 + 发货单明细）
/// </summary>
public class DispatchDto
{
    // === 发货单主表 ===
    public int Dlid { get; set; }
    public string CDLCode { get; set; } = string.Empty;
    public DateTime? DlDate { get; set; }
    public string DlCusCode { get; set; } = string.Empty;
    public string DlCusName { get; set; } = string.Empty;
    public string? CVerifier { get; set; }

    // === 发货单明细 ===
    public List<DispatchDetailDto> DispatchDetails { get; set; } = new();
}

/// <summary>
/// 发货单明细
/// </summary>
public class DispatchDetailDto
{
    public string? DlInvCode { get; set; }
    public string? DlInvName { get; set; }
    public string? DlInvStd { get; set; }
    public decimal DlQuantity { get; set; }
}

/// <summary>
/// 未审核发货单行（用于日期批量修改页面）
/// </summary>
public class UnverifiedDispatchRow
{
    public int Dlid { get; set; }
    public string CDLCode { get; set; } = string.Empty;
    public string CusCode { get; set; } = string.Empty;
    public string CusName { get; set; } = string.Empty;
    public DateTime? DDate { get; set; }
    public string? CVerifier { get; set; }
    public string? CPersonName { get; set; }
}