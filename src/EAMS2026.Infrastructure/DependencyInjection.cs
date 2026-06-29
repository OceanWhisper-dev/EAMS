using EAMS2026.Application.Services;
using EAMS2026.Application.Services.Attendance;
using EAMS2026.Application.Common;
using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Attendance;
using EAMS2026.Application.Common.Interfaces.Erp;
using EAMS2026.Domain.Interfaces;
using EAMS2026.Domain.Interfaces.Repositories;
using EAMS2026.Domain.Interfaces.Repositories.Attendance;
using EAMS2026.Domain.Interfaces.Repositories.Erp;
using EAMS2026.Domain.Interfaces.Repositories.System;
using EAMS2026.Infrastructure.Data;
using EAMS2026.Infrastructure.Data.Repositories;
using EAMS2026.Infrastructure.Data.Repositories.Attendance;
using EAMS2026.Infrastructure.Data.Repositories.Erp;
using EAMS2026.Infrastructure.Data.Repositories.System;
using EAMS2026.Infrastructure.Services;
using EAMS2026.Infrastructure.Services.Erp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EAMS2026.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' not found.");
        var hwattConnectionString = configuration.GetConnectionString("HwattConnection")
            ?? throw new InvalidOperationException("ConnectionString 'HwattConnection' not found.");
        var erpAccountSet = configuration["ErpSettings:AccountSet"] ?? "001";
        var erpDataSourceName = configuration["ErpSettings:DataSourceName"] ?? "erp";

        services.AddSingleton(new DbConnectionFactory(connectionString));
        services.AddSingleton(new HwattConnectionFactory(hwattConnectionString));
        services.AddSingleton<ErpConnectionFactory>(sp =>
        {
            var dbFactory = sp.GetRequiredService<DbConnectionFactory>();
            return new ErpConnectionFactory(dbFactory, erpAccountSet, erpDataSourceName);
        });
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<AttendanceCalculationService>();
        services.AddScoped<ExcelService>();
        services.AddScoped<PrintService>();

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IOperationLogRepository, OperationLogRepository>();
        services.AddScoped<IDictTypeRepository, DictTypeRepository>();
        services.AddScoped<IDictItemRepository, DictItemRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDashboardWidgetRepository, DashboardWidgetRepository>();
        services.AddScoped<IDataPermissionRepository, DataPermissionRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IVouchModifyRepository, ErpVouchModifyRepository>();
        services.AddScoped<IVouchModifyLogRepository, VouchModifyLogRepository>();
        services.AddScoped<IHwattImportService, HwattImportService>();

        services.AddScoped<IReportService, ReportService>();

        services.Configure<DingTalkOptions>(configuration.GetSection("DingTalk"));
        services.AddHttpClient<IDingTalkService, DingTalkService>(client =>
        {
            client.BaseAddress = new Uri("https://oapi.dingtalk.com");
        });

        // HWATT 导入配置
        services.Configure<HwattImportOptions>(configuration.GetSection("HwattImport"));
        services.AddSingleton<IHwattImportSettings>(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HwattImportOptions>>().Value);

        // 后台任务队列（同时注册为 IHostedService 和自身，供 Controller 构造函数注入）
        services.AddSingleton<BackgroundTaskService>();
        services.AddHostedService(sp => sp.GetRequiredService<BackgroundTaskService>());

        // 仪表盘配置
        services.Configure<DashboardOptions>(configuration.GetSection("Dashboard"));
        services.AddSingleton<IDashboardSettings>(sp =>
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DashboardOptions>>().Value);

        services.AddScoped<DynamicDataEngine>();
        services.AddScoped<IDynamicDataEngine>(sp => sp.GetRequiredService<DynamicDataEngine>());
        services.AddHttpClient<DynamicDataEngine>();

        return services;
    }
}