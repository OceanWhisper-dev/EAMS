using System.Net;
using System.Text.Json;

namespace EAMS2026.Api.Middleware;

/// <summary>
/// 全局异常处理中间件。
/// 捕获所有未处理的异常，将其转换为统一的 JSON 错误响应格式。
///
/// 异常映射规则：
/// - KeyNotFoundException       → 404 "请求的资源不存在"
/// - UnauthorizedAccessException → 401 "无权限访问"
/// - ArgumentException          → 400 参数错误消息
/// - 其他所有异常               → 500 "服务器内部错误，请稍后重试"
///
/// 注：此中间件必须在管道中尽早注册（在 UseRouting 之前），才能捕获后续所有中间件的异常。
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发生未处理的异常: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "请求的资源不存在"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "无权限访问"),
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            _ => (HttpStatusCode.InternalServerError, "服务器内部错误，请稍后重试")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new Dictionary<string, object>
        {
            { "success", false },
            { "message", message },
            { "statusCode", (int)statusCode }
        };

        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            response["detail"] = exception.ToString();
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}