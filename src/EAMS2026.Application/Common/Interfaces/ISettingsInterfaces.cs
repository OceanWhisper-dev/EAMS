namespace EAMS2026.Application.Common.Interfaces;

/// <summary>HWATT 导入配置（从 appsettings.json 的 HwattImport 节读取）</summary>
public interface IHwattImportSettings
{
    /// <summary>HWATT 数据库的分公司 ID，用于筛选员工列表</summary>
    int BrchId { get; }

    /// <summary>同步打卡记录的默认起始日期 (yyyy-MM-dd)</summary>
    string DefaultSyncStartDate { get; }

    /// <summary>同步打卡记录时每批次的天数</summary>
    int BatchDays { get; }
}

/// <summary>仪表盘配置（从 appsettings.json 的 Dashboard 节读取）</summary>
public interface IDashboardSettings
{
    /// <summary>仪表盘最近操作日志显示条数</summary>
    int RecentLogLimit { get; }
}