namespace EAMS2026.Domain.DTOs.Erp;

/// <summary>
/// 销售订单DTO（聚合结构：订单主表 + 订单明细 + 关联发货单）
/// </summary>
public class OrderDto
{
    // === 订单主表 ===
    public int Soid { get; set; }
    public string CSOCode { get; set; } = string.Empty;
    public DateTime? SoDate { get; set; }
    public string SoCusCode { get; set; } = string.Empty;
    public string SoCusName { get; set; } = string.Empty;
    public string? CPersonName { get; set; }

    // === 订单明细 ===
    public List<OrderDetailDto> OrderDetails { get; set; } = new();

    // === 关联发货单 ===
    public List<DispatchDto> Dispatches { get; set; } = new();
}

/// <summary>
/// 订单明细
/// </summary>
public class OrderDetailDto
{
    public string? SoInvCode { get; set; }
    public string? SoInvName { get; set; }
    public string? SoInvStd { get; set; }
    public decimal SoQuantity { get; set; }
}