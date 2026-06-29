using EAMS2026.Application.Common.Interfaces;
using EAMS2026.Application.Common.Interfaces.System;
using EAMS2026.Application.Services;
using EAMS2026.Domain.Entities.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EAMS2026.Api.Controllers.System;

[Authorize]
[ApiController]
[Produces("application/json")]
[ProducesResponseType(200)]
[ProducesResponseType(400)]
[ProducesResponseType(401)]
[ProducesResponseType(404)]
[ProducesResponseType(500)]
[Route("api/message")]
public class MessageController : BaseController
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [Authorize(Policy = "message")]
    [HttpGet("received")]
    public async Task<IActionResult> GetReceived([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (messages, unreadCount) = await _messageService.GetReceivedAsync(GetUserId(), page, pageSize);
        return Success(new { messages, unreadCount });
    }

    [Authorize(Policy = "message")]
    [HttpGet("sent")]
    public async Task<IActionResult> GetSent([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (messages, _) = await _messageService.GetSentAsync(GetUserId(), page, pageSize);
        return Success(messages);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _messageService.GetUnreadCountAsync(GetUserId());
        return Success(count);
    }

    [Authorize(Policy = "message")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _messageService.GetByIdAsync(id);
        if (data == null)
            return NotFound("消息不存在");
        return Success(data);
    }

    [Authorize(Policy = "message")]
    [HttpGet("{id}/conversation")]
    public async Task<IActionResult> GetConversation(long id)
    {
        var data = await _messageService.GetConversationAsync(id);
        return Success(data);
    }

    [Authorize(Policy = "message")]
    [HttpPost]
    public async Task<IActionResult> Send([FromBody] Message message)
    {
        var userName = User.Identity?.Name ?? "";
        message.SenderName = userName;
        var (success, msg) = await _messageService.SendAsync(message, GetUserId());
        if (success)
            return Success(msg);
        return Fail(msg);
    }

    [Authorize(Policy = "message")]
    [HttpPost("{id}/reply")]
    public async Task<IActionResult> Reply(long id, [FromBody] ReplyRequest request)
    {
        var userName = User.Identity?.Name ?? "";
        var (success, msg) = await _messageService.ReplyAsync(id, request.Content, GetUserId(), userName);
        if (success)
            return Success(msg);
        return Fail(msg);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var (success, msg) = await _messageService.MarkAsReadAsync(id, GetUserId());
        if (success)
            return Success(msg);
        return Fail(msg);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var (success, msg) = await _messageService.MarkAllAsReadAsync(GetUserId());
        if (success)
            return Success(msg);
        return Fail(msg);
    }

    [Authorize(Policy = "message")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var (success, msg) = await _messageService.DeleteAsync(id, GetUserId());
        if (success)
            return Success(msg);
        return Fail(msg);
    }
}

public class ReplyRequest
{
    public string Content { get; set; } = string.Empty;
}