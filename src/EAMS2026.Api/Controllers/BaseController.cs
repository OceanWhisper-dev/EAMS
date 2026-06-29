using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers;

/// <summary>
/// 控制器基类，提供统一的响应格式和用户身份获取方法。
/// 所有业务控制器均继承此类。
/// </summary>
[ApiController]
public class BaseController : ControllerBase
{
    /// <summary>
    /// 从JWT Token中提取当前登录用户的ID。
    /// 未登录或Token无效时抛出 UnauthorizedAccessException。
    /// </summary>
    /// <returns>用户ID</returns>
    /// <exception cref="UnauthorizedAccessException">用户未登录或Token无效</exception>
    protected long GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !long.TryParse(claim.Value, out var id))
        {
            throw new UnauthorizedAccessException("用户未登录或Token无效");
        }
        return id;
    }

    /// <summary>
    /// 从JWT Token中提取当前登录用户的用户名。
    /// 未登录或Token无效时抛出 UnauthorizedAccessException。
    /// </summary>
    /// <returns>用户名</returns>
    /// <exception cref="UnauthorizedAccessException">用户未登录或Token无效</exception>
    protected string GetUsername()
    {
        var claim = User.FindFirst(ClaimTypes.Name);
        if (claim == null || string.IsNullOrEmpty(claim.Value))
        {
            throw new UnauthorizedAccessException("用户未登录或Token无效");
        }
        return claim.Value;
    }

    /// <summary>
    /// 返回成功响应（带数据）。
    /// </summary>
    /// <param name="data">响应数据</param>
    /// <param name="message">成功消息</param>
    /// <returns>200 OK + {success:true, data, message}</returns>
    protected IActionResult Success(object? data, string message = "操作成功")
    {
        return Ok(new { success = true, data, message });
    }

    /// <summary>
    /// 返回成功响应（不带数据，仅有消息）。
    /// </summary>
    /// <param name="message">成功消息</param>
    protected IActionResult Success(string message)
    {
        return Ok(new { success = true, message });
    }

    /// <summary>
    /// 返回业务失败响应。
    /// 用于表示操作因业务规则校验未通过（如"用户名已存在"），并非服务器异常。
    /// HTTP状态码为400，通过 success=false 标识失败。
    /// </summary>
    /// <param name="message">失败原因</param>
    /// <returns>400 BadRequest + {success:false, message}</returns>
    protected IActionResult Fail(string message)
    {
        return BadRequest(new { success = false, message });
    }

    /// <summary>
    /// 执行异步操作并自动处理结果。
    /// 适用于 Service 层返回 (bool Success, string Message) 元组的场景。
    /// </summary>
    /// <param name="func">异步操作委托</param>
    /// <returns>成功返回Success，失败返回Fail</returns>
    protected async Task<IActionResult> ExecuteAsync(Func<Task<(bool Success, string Message)>> func)
    {
        var (success, message) = await func();
        return success ? Success(message) : Fail(message);
    }

    /// <summary>
    /// 执行异步操作并自动处理结果（带返回值）。
    /// 适用于 Service 层返回 (bool Success, string Message, T Data) 元组的场景。
    /// </summary>
    /// <typeparam name="T">返回数据类型</typeparam>
    /// <param name="func">异步操作委托</param>
    /// <returns>成功返回Success(data)，失败返回Fail</returns>
    protected async Task<IActionResult> ExecuteAsync<T>(Func<Task<(bool Success, string Message, T Data)>> func)
    {
        var (success, message, data) = await func();
        return success ? Success(data, message) : Fail(message);
    }
}