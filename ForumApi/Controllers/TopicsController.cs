using Microsoft.AspNetCore.Mvc;
using ForumApi.DTOs.Topics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public interface ITopicsController
{
    Task<ActionResult<IEnumerable<TopicSummaryDto>>> GetAll();
    Task<ActionResult<TopicSummaryDto>> Create([FromBody] TopicRequest request);
    Task<IActionResult> Delete(int id);
    Task<ActionResult<TopicSummaryDto>> Modify(int id, [FromBody] TopicRequest request);
}

[ApiController]
[Route("api/topics")]
public class TopicsController : ControllerBase, ITopicsController
{
    private readonly ITopicService _service;

    public TopicsController(ITopicService service)
    {
       _service = service;
    }
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ClaimsHelper.AnonymousUser;

    [Authorize(Roles = "User, Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TopicSummaryDto>>> GetAll()
    {
        var topics = await _service.GetAllAsync();
        return Ok(topics);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TopicSummaryDto>> Create([FromBody] TopicRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest("Topic title is required.");
        } 
        var createdTopic = await _service.CreateAsync(request.Title, GetUserId());
        return CreatedAtAction(nameof(GetAll), new { id = createdTopic.Id }, createdTopic);
    }

    [Authorize(Roles = "Admin")]
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

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<TopicSummaryDto>> Modify(int id, [FromBody] TopicRequest request)
    {
        var success = await _service.ModifyAsync(id, request.Title, GetUserId());
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
