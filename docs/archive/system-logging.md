# 系统日志查看方法

## 概述

EAMS2026 使用 ASP.NET Core 内置的 `Microsoft.Extensions.Logging` 记录系统运行时日志。默认仅输出到**控制台**，可通过以下方法查看。

日志级别：`Trace` < `Debug` < `Information` < `Warning` < `Error` < `Critical`

当前日志配置（`appsettings.json`）：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",       // 默认记录 Information 及以上级别
      "Microsoft.AspNetCore": "Warning"  // ASP.NET Core 框架只记录 Warning 及以上
    }
  }
}
```

---

## 方法一：控制台直接查看（开发环境）

### 后端启动时查看

在终端启动后端服务，日志会实时输出到终端：

```powershell
# 在项目根目录执行
& "D:\Scoop\apps\dotnet8-sdk\current\dotnet.exe" run --project src\EAMS2026.Api
```

启动后可在终端看到类似输出：

```
info: EAMS2026.Api.Program[0]
      系统启动完成，监听地址: http://0.0.0.0:5106
info: EAMS2026.Application.Services.AttendanceService[0]
      INSERT record: emp=1001, date=2026-06-01, bAtt=08:55:00, eAtt=18:05:00, id=1234
warn: EAMS2026.Application.Services.AttendanceService[0]
      自动同步HWATT打卡记录失败，继续执行导入
fail: EAMS2026.Api.Middleware.ExceptionMiddleware[0]
      请求 /api/xxx 发生未处理异常: ...
```

### 按级别过滤查看

PowerShell 中用 `Select-String` 过滤：

```powershell
# 只查看 Error 级别日志
dotnet run --project src\EAMS2026.Api 2>&1 | Select-String "fail|error|Error"
```

---

## 方法二：输出到日志文件（推荐生产环境） - 已按此方法实施！

### 步骤 1：安装 Serilog 包

```powershell
cd src\EAMS2026.Api
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

### 步骤 2：修改 `Program.cs`

在 `builder` 创建之后、`Build()` 之前添加 Serilog：

```csharp
// Program.cs — 在 var builder = WebApplication.CreateBuilder(args); 之后添加
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
```

### 步骤 3：修改 `appsettings.json`，添加 Serilog 文件输出配置

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/eams2026-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

配置完成后，日志会按天滚动写入 `logs/eams2026-20260604.log` 等文件，保留最近 30 天。

### 按级别查看日志文件

```powershell
# 查看 Error 级别的日志
Select-String "\[ERR\]" logs/eams2026-*.log

# 查看 Warning 及以上级别的日志
Select-String "\[WRN\]|\[ERR\]" logs/eams2026-*.log

# 实时跟踪最新日志
Get-Content logs/eams2026-$(Get-Date -Format "yyyyMMdd").log -Tail 20 -Wait
```

---

## 方法三：使用 Docker 查看（如果使用容器部署）

```bash
# 查看容器日志
docker logs eams2026-api -f

# 查看最近 100 行
docker logs eams2026-api --tail 100

# 按时间过滤
docker logs eams2026-api --since 2026-06-04T10:00:00
```

---

## 常用故障排查场景

| 场景 | 查看方法 |
|------|----------|
| 接口返回 500 | 搜索 `fail` 或 `Error` 级别日志，查看异常堆栈 |
| 第三方服务连接失败 | 搜索 `HWATT`、`DingTalk` 相关日志 |
| 依赖注入错误 | 搜索 `fail` 级别，通常在启动阶段出现 |
| 数据库查询慢 | 临时将 `LogLevel` 中的 `Microsoft.EntityFrameworkCore` 改为 `Information` |
| 权限验证失败 | 搜索 `Authorization` 或 `Permission` 相关日志 |

---

## 调整日志级别（临时调试）

修改 `appsettings.Development.json` 可临时输出更多信息：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",              // 输出所有 Debug 及以上日志
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"  // 显示 EF Core SQL 语句
    }
  }
}
```

修改后重启后端即可生效，无需重新编译。