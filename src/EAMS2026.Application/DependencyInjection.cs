using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.Attendance;
using EAMS2026.Application.Common.Interfaces.Erp;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using EAMS2026.Application.Services.Attendance;
using EAMS2026.Application.Services.Erp;
using EAMS2026.Application.Services.System;
using Microsoft.Extensions.DependencyInjection;

namespace EAMS2026.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IImportTaskService, ImportTaskService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDictService, DictService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IDashboardConfigService, DashboardConfigService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
        services.AddScoped<IDataPermissionService, DataPermissionService>();
        services.AddScoped<IVouchModifyService, VouchModifyService>();

        return services;
    }
}