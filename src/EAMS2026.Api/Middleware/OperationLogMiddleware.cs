using System.Security.Claims;
using System.Text.Json;
using EAMS2026.Domain.Entities.System;
using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EAMS2026.Api.Middleware;

public class OperationLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OperationLogMiddleware> _logger;
    private const int MaxResponseBodyBytes = 10 * 1024 * 1024; // 10MB 上限，超过则不缓冲
    private static readonly HashSet<string> _skipPaths = new()
    {
        "/api/auth/login",
        "/api/auth/profile",
        "/api/health",
        "/api/reports/preview",
        "/api/reports/execute",
        "/swagger",
        "/favicon.ico",
        ".js",
        ".css",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".ico",
        ".svg",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot"
    };

    public OperationLogMiddleware(RequestDelegate next, ILogger<OperationLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (ShouldSkipLogging(path))
        {
            await _next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var originalBodyStream = context.Response.Body;
        var bodyExceededLimit = false;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            // 在 _next 之后检查 ContentType，此时响应头已正确设置
            var contentType = context.Response.ContentType ?? "";
            var isBinaryResponse = contentType.StartsWith("application/octet-stream")
                || contentType.StartsWith("image/")
                || contentType.StartsWith("video/");

            responseBody.Seek(0, SeekOrigin.Begin);

            if (isBinaryResponse)
            {
                // 二进制响应：恢复原始流并记录日志
                context.Response.Body = originalBodyStream;
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else if (responseBody.Length <= MaxResponseBodyBytes)
            {
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                bodyExceededLimit = true;
                context.Response.Body = originalBodyStream;
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                var skipMsg = System.Text.Encoding.UTF8.GetBytes(
                    "{\"message\":\"响应数据量过大已截断，请使用分页参数\"}");
                await originalBodyStream.WriteAsync(skipMsg);
            }
            context.Response.Body = originalBodyStream;
        }

        if (!bodyExceededLimit && context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            await LogOperationAsync(context, path, startTime);
        }
    }

    private bool ShouldSkipLogging(string path)
    {
        if (path.StartsWith("/api/operation-log"))
            return true;

        foreach (var skipPath in _skipPaths)
        {
            if (skipPath.StartsWith("/"))
            {
                if (path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (skipPath.StartsWith("."))
            {
                if (path.EndsWith(skipPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private async Task LogOperationAsync(HttpContext context, string path, DateTime startTime)
    {
        try
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return;

            var userId = long.TryParse(userIdClaim, out var parsedId) ? parsedId : 0;
            var username = context.User.Identity?.Name ?? "Unknown";
            var method = context.Request.Method;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var module = ExtractModule(path);
            var operation = ExtractOperation(method, path);
            var description = GenerateDescription(method, path);

            using var scope = context.RequestServices.CreateScope();
            var logRepository = scope.ServiceProvider.GetRequiredService<IOperationLogRepository>();

            await logRepository.AddAsync(new OperationLog
            {
                UserId = userId,
                Username = username,
                Module = module,
                OperationType = operation,
                Description = description,
                IpAddress = ipAddress
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "操作日志记录失败: {Path}", path);
        }
    }

    private string ExtractModule(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0] == "api")
        {
            var moduleName = parts[1];
            return char.ToUpper(moduleName[0]) + moduleName.Substring(1);
        }
        return "Unknown";
    }

    private string ExtractOperation(string method, string path)
    {
        return method switch
        {
            "GET" => "Query",
            "POST" => "Create",
            "PUT" => "Update",
            "DELETE" => "Delete",
            "PATCH" => "Patch",
            _ => method
        };
    }

    private string GenerateDescription(string method, string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return $"{method} {path}";

        var resource = parts[1];
        var action = method switch
        {
            "GET" => "查询",
            "POST" => "新增",
            "PUT" => "修改",
            "DELETE" => "删除",
            "PATCH" => "部分更新",
            _ => method
        };

        return $"{action}{resource}";
    }
}