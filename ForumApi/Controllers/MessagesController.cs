using Microsoft.AspNetCore.Mvc;
using ForumApi.Models;
using ForumApi.DTOs.Messages;

[ApiController]
[Route("api/topics/{topicId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly MessageService _service;
    public MessagesController(MessageService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(int topicId)
    {
        var messages = await _service.GetAllByTopicIdAsync(topicId);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Create(int topicId, [FromBody] CreateMessageRequest request)
    {
        var createdMessage = await _service.CreateAsync(topicId, request.Content,request.UserId);
        if(createdMessage == null)
        {
            return BadRequest("Failed to create message.");
        }
        return CreatedAtAction(nameof(GetAll), new { topicId }, createdMessage);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MessageResponse>> Modify(int id, [FromBody] CreateMessageRequest request)
    {
        var message = await _service.ModifyAsync(id, request.Content);
        if (message == null)
        {
            return NotFound();
        }
        return Ok(message);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }
}
