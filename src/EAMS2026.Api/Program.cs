using System.Text;
using System.Text.Json.Serialization;
using EAMS2026.Api.JsonConverters;
using EAMS2026.Api.Middleware;
using EAMS2026.Application;
using EAMS2026.Infrastructure;
using EAMS2026.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// Npgsql 兼容开关：使 PostgreSQL DATE/TIME 映射为 DateTime/TimeSpan 而非 DateOnly/TimeOnly
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Serilog 日志配置
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// 配置Kestrel监听所有网络接口，允许局域网内其他设备访问
builder.WebHost.UseUrls(builder.Configuration.GetValue<string>("Urls") ?? "http://0.0.0.0:5106");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase, true));
        options.JsonSerializerOptions.Converters.Add(new NullableDateOnlyJsonConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EAMS2026 API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// JWT 配置仅从配置文件读取，不提供硬编码回退（安全要求）
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey 未在配置文件中设置，请检查 appsettings.json");
}
if (secretKey.Length < 32)
{
    throw new InvalidOperationException("JWT SecretKey 至少需要 32 个字符以确保 HMAC-SHA256 签名强度");
}
var issuer = jwtSettings["Issuer"] ?? "EAMS2026";
var audience = jwtSettings["Audience"] ?? "EAMS2026";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // SignalR WebSocket 无法发送 Header，改为从 query string 读取 token
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// CORS 策略：开发环境允许所有来源（便于局域网调试），生产环境应从配置文件读取
builder.Services.AddCors(options =>
{
    options.AddPolicy("LanAccess", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// 响应压缩，减少网络传输量
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.EnableForHttps = false;
});

builder.Services.AddSignalR();

// 权限校验策略注册
    builder.Services.AddAuthorization(options =>
    {
        // 超级管理员策略
        options.AddPolicy("super_admin", policy =>
            policy.RequireRole("super_admin"));

        // 通用模块权限策略（与前端权限编码保持一致）
        var moduleCodes = new[] { "department", "employee", "user", "role", "permission", "dict", "dashboard-config", "operation-log", "message", "attendance", "data-permission", "erp-report", "erp-vouchmodify", "erp-settings" };
        foreach (var code in moduleCodes)
        {
            options.AddPolicy(code, policy =>
                policy.AddRequirements(new PermissionRequirement(code)));
        }
    });

    builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseResponseCompression();
app.UseCors("LanAccess");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<OperationLogMiddleware>();

app.MapControllers();

app.MapHub<EAMS2026.Api.Hubs.ImportHub>("/hubs/import");

app.Run();