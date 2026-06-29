namespace EAMS2026.Domain.DTOs.Erp;

/// <summary>
/// 单笔修改结果
/// </summary>
public class VouchModifyResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int AffectedRows { get; set; }

    public static VouchModifyResult Ok(string? message = null, int affected = 1)
        => new() { Success = true, Message = message ?? "操作成功", AffectedRows = affected };

    public static VouchModifyResult Fail(string message)
        => new() { Success = false, Message = message };
}

/// <summary>
/// 批量修改结果
/// </summary>
public class VouchModifyBatchResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<BatchFailItem> Failures { get; set; } = new();
}

/// <summary>
/// 批量修改失败项
/// </summary>
public class BatchFailItem
{
    public int Dlid { get; set; }
    public string DlCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}