using EAMS2026.Application.Common.Interfaces;

namespace EAMS2026.Infrastructure.Data;

/// <summary>HWATT 导入配置</summary>
public class HwattImportOptions : IHwattImportSettings
{
    /// <summary>HWATT 数据库的分公司 ID</summary>
    public int BrchId { get; set; } = 3;

    /// <summary>同步打卡记录的默认起始日期</summary>
    public string DefaultSyncStartDate { get; set; } = "2024-01-01";

    /// <summary>同步打卡记录时每批次的天数</summary>
    public int BatchDays { get; set; } = 7;
}

/// <summary>仪表盘配置</summary>
public class DashboardOptions : IDashboardSettings
{
    /// <summary>仪表盘最近操作日志显示条数</summary>
    public int RecentLogLimit { get; set; } = 10;
}