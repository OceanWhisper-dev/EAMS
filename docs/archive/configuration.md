# EAMS2026 配置说明

## 概述

所有配置项均在 `src/EAMS2026.Api/appsettings.json` 中管理。以下按配置节逐项说明。

---

## ConnectionStrings — 数据库连接

| 键 | 说明 | 示例值 |
|---|------|--------|
| `DefaultConnection` | 主数据库 (PostgreSQL) 连接字符串 | `Host=localhost;Port=5432;Database=eams2026;Username=postgres;Password=postgres` |
| `HwattConnection` | HWATT 考勤机数据库 (SQL Server) 连接字符串 | `Server=db;Database=HWATT;User ID=sa;Password=dfl_DB;TrustServerCertificate=True` |

---

## Jwt — JWT 认证

| 键 | 说明 | 示例值 |
|---|------|--------|
| `SecretKey` | JWT 签名密钥（至少 32 字符） | `EAMS2026_SuperSecretKey_AtLeast32Characters!` |
| `Issuer` | JWT 签发者 | `EAMS2026` |
| `Audience` | JWT 受众 | `EAMS2026` |
| `ExpireMinutes` | Token 过期时间（分钟） | `1440`（24 小时） |

---

## DingTalk — 钉钉集成

| 键 | 说明 | 默认值 | 修改说明 |
|---|------|--------|----------|
| `AppKey` | 钉钉应用 AppKey | — | — |
| `AppSecret` | 钉钉应用 AppSecret | — | — |
| `AttendanceGroupName` | 考勤组名称 | `"考勤组"` | — |
| `DepartmentUserPageSize` | 获取部门用户列表的分页大小 | `100` | 原硬编码 `size=100` |
| `AttendancePageSize` | 考勤记录 API 每页/每批大小 | `50` | 原硬编码多处 `50`（分页、分批、偏移步长） |
| `MaxRetries` | API 限流最大重试次数 | `5` | 原硬编码 `const int maxRetries = 5` |
| `RetryBaseDelayMs` | 限流重试指数退避基数（毫秒） | `1000` | 原硬编码 `1000 * 2^(n-1)` 中的基数 `1000` |
| `TokenExpireBufferSeconds` | Token 缓存过期时间（秒），官方为 7200 秒 | `7000` | 原硬编码 `AddSeconds(7000)` |
| `AttendanceBatchDays` | 查询考勤记录时每批次的天数 | `7` | 原硬编码 `AddDays(7)` |

---

## HwattImport — HWATT 考勤机导入

| 键 | 说明 | 默认值 | 修改说明 |
|---|------|--------|----------|
| `BrchId` | HWATT 数据库的分公司 ID，用于筛选员工列表 | `3` | 原硬编码 `BrchID = 3` |
| `DefaultSyncStartDate` | 同步打卡记录的默认起始日期 | `"2024-01-01"` | 原硬编码 `new DateTime(2024, 1, 1)` |
| `BatchDays` | 同步打卡记录时每批次的天数 | `7` | 原硬编码 `AddDays(7)` |

---

## Dashboard — 仪表盘

| 键 | 说明 | 默认值 | 修改说明 |
|---|------|--------|----------|
| `RecentLogLimit` | 仪表盘最近操作日志显示条数 | `10` | 原硬编码 `var limit = 10` |

---

## 配置类与注入方式

每个配置节对应一个 C# 配置类，通过 ASP.NET Core 的 Options 模式绑定：

| 配置节 | 配置类 | 位置 | 注册方式 |
|--------|--------|------|----------|
| `DingTalk` | `DingTalkOptions` | `Infrastructure/Data/DingTalkService.cs` | `services.Configure<DingTalkOptions>(...)` |
| `HwattImport` | `HwattImportOptions` | `Infrastructure/Data/OptionsClasses.cs` | `services.Configure<HwattImportOptions>(...)` → 注册为 `IHwattImportSettings` 单例 |
| `Dashboard` | `DashboardOptions` | `Infrastructure/Data/OptionsClasses.cs` | `services.Configure<DashboardOptions>(...)` → 注册为 `IDashboardSettings` 单例 |

Application 层通过接口 `IHwattImportSettings`、`IDashboardSettings` 读取配置，避免依赖 `IOptions<T>`。

### 配置注册代码

```csharp
// DependencyInjection.cs
services.Configure<DingTalkOptions>(configuration.GetSection("DingTalk"));

services.Configure<HwattImportOptions>(configuration.GetSection("HwattImport"));
services.AddSingleton<IHwattImportSettings>(sp =>
    sp.GetRequiredService<IOptions<HwattImportOptions>>().Value);

services.Configure<DashboardOptions>(configuration.GetSection("Dashboard"));
services.AddSingleton<IDashboardSettings>(sp =>
    sp.GetRequiredService<IOptions<DashboardOptions>>().Value);
```

---

## 修改历史

| 日期 | 修改内容 |
|------|----------|
| 2026-06-04 | 将 HWATT BrchId 从硬编码 `3` 改为 `HwattImport:BrchId` 配置 |
| 2026-06-04 | 将 HWATT 同步默认起始日期从 `new DateTime(2024,1,1)` 改为 `HwattImport:DefaultSyncStartDate` |
| 2026-06-04 | 将 HWATT 批次天数从硬编码 `7` 改为 `HwattImport:BatchDays` |
| 2026-06-04 | 将钉钉分页大小、重试次数、退避基数、Token 过期缓冲、批次天数等 7 个值改为 `DingTalk` 配置节 |
| 2026-06-04 | 将仪表盘日志显示条数从硬编码 `10` 改为 `Dashboard:RecentLogLimit` |