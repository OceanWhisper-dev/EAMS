namespace EAMS2026.Domain.DTOs.Erp;

/// <summary>
/// 单据查询参数
/// </summary>
public class VouchQueryParam
{
    public string? CusCode { get; set; }
    public string? CusName { get; set; }
    public string? CusAbbName { get; set; }
    public string? CusContact { get; set; }
    public string? CusPPerson { get; set; }
    public string? CusPhone { get; set; }
    public string? CusMobile { get; set; }
    public string? CusAddr { get; set; }
    public string? VouchCode { get; set; }
    public DateTime? VouchDateFrom { get; set; }
    public DateTime? VouchDateTo { get; set; }
    /// <summary>
    /// 审核状态过滤：null/空=全部，unverified=未审核，verified=已审核
    /// </summary>
    public string? VerifierStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}