using Microsoft.AspNetCore.Mvc;
using ForumApi.DTOs.Messages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ForumApi.Services;

public interface IMessageController
{
    Task<ActionResult<IEnumerable<MessageResponse>>> GetAllMessagesByTopicId(int topicId);
    Task<ActionResult<MessageResponse>> CreateMessageAsync(int topicId, [FromBody] MessageRequest request);
    Task<ActionResult<bool>> ModifyMessageAsync(int id, [FromBody] MessageRequest request);
    Task<ActionResult<bool>> DeleteMessageAsync(int id);
}

[ApiController]
[Route("api/topics/{topicId}/messages")]
public class MessagesController : ControllerBase, IMessageController
{
    private readonly IMessageService _service;
    public MessagesController(IMessageService service)
    {
        _service = service;
    }

    [Authorize(Roles = "User, Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetAllMessagesByTopicId(int topicId)
    {
        var messages = await _service.GetAllMessagesByTopicIdAsync(topicId);
        if (messages == null)
        {
            return NotFound();
        }
        return Ok(messages);
    }

    [Authorize(Roles = "User, Admin")]
    [HttpPost]
    public async Task<ActionResult<MessageResponse>> CreateMessageAsync(int topicId, [FromBody] MessageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Forbid();
        }
        var username = User.FindFirstValue(ClaimTypes.Name);
        if (username == null)
        {
            return Forbid();
        }
        var createdMessage = await _service.CreateMessageAsync(topicId, request.Content, userId, username);
        if(createdMessage == null)
        {
            return BadRequest("Failed to create the message.");
        }
        return CreatedAtAction(nameof(GetAllMessagesByTopicId), new { topicId }, createdMessage);
    }

    [Authorize(Roles = "User, Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<bool>> ModifyMessageAsync(int id, [FromBody] MessageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Forbid();
        }
        var success = await _service.ModifyMessageAsync(id, request.Content, userId);
        if (success == null)
        {
            return NotFound();
        }
        if (success == false)
        {
            return Forbid();
        }
        return NoContent();
    }

    [Authorize(Roles = "User, Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> DeleteMessageAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Forbid();
        }
        var success = await _service.DeleteMessageAsync(id, userId);
        if (success == null)
        {
            return NotFound();
        }
        if (success == false)
        {
            return Forbid();
        }
        return NoContent();
    }
}
