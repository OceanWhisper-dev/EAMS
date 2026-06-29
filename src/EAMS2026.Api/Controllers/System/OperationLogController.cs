using EAMS2026.Domain.Interfaces.Repositories.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(500)]
[Route("api/operation-log")]
public class OperationLogController : BaseController
{
    private readonly IOperationLogRepository _logRepository;
    private readonly IUserRepository _userRepository;

    public OperationLogController(IOperationLogRepository logRepository, IUserRepository userRepository)
    {
        _logRepository = logRepository;
        _userRepository = userRepository;
    }

    [Authorize(Policy = "operation-log")]
    [HttpGet]
    public async Task<IActionResult> GetPaged(int page = 1, int pageSize = 20, long? userId = null, string? module = null, string? startDate = null, string? endDate = null)
    {
        var currentUser = await _userRepository.GetByIdAsync(GetUserId());
        var isAdmin = currentUser?.Roles.Any(r => r.Code == "super_admin") == true;

        if (!isAdmin)
            return Forbid();

        var start = string.IsNullOrEmpty(startDate) ? (DateTime?)null : DateTime.Parse(startDate);
        var end = string.IsNullOrEmpty(endDate) ? (DateTime?)null : DateTime.Parse(endDate).AddDays(1);

        var items = await _logRepository.GetPagedAsync(page, pageSize, userId, module, start, end);
        var total = await _logRepository.GetCountAsync(userId, module, start, end);

        return Success(new { items, total, page, pageSize });
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(int page = 1, int pageSize = 20)
    {
        var userId = GetUserId();
        var items = await _logRepository.GetPagedAsync(page, pageSize, userId: userId);
        var total = await _logRepository.GetCountAsync(userId: userId);

        return Success(new { items, total, page, pageSize });
    }

    [Authorize(Policy = "operation-log")]
    [HttpDelete("clear")]
    public async Task<IActionResult> Clear(string? beforeDate = null, long? userId = null)
    {
        var currentUser = await _userRepository.GetByIdAsync(GetUserId());
        var isAdmin = currentUser?.Roles.Any(r => r.Code == "super_admin") == true;

        if (!isAdmin)
            return Forbid();

        var before = string.IsNullOrEmpty(beforeDate) ? (DateTime?)null : DateTime.Parse(beforeDate);
        var count = await _logRepository.ClearAsync(before, userId);

        var msg = userId.HasValue ? $"已清理用户 {userId} 的 {count} 条日志" : (before.HasValue ? $"已清理 {before:yyyy-MM-dd} 之前的 {count} 条日志" : $"已清理全部 {count} 条日志");
        return Success(count, msg);
    }
}