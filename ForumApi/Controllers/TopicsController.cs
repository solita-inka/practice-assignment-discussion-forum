using Microsoft.AspNetCore.Mvc;
using ForumApi.DTOs.Pagination;
using ForumApi.DTOs.Topics;
using ForumApi.Helpers;
using ForumApi.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ForumApi.Controllers;

public interface ITopicsController
{
    Task<ActionResult<PagedResponse<TopicSummaryDto>>> GetAll(int page, int pageSize, bool archived = false);
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

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TopicSummaryDto>>> GetAll(int page = 1, int pageSize = 10, bool archived = false)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var topics = await _service.GetAllAsync(page, pageSize, archived);
        return Ok(topics);
    }

    [Authorize(Roles = "User, Admin")]
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

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/archive")]
    public async Task<IActionResult> SetArchiveStatus(int id, [FromBody] bool isArchived)
    {
        var success = await _service.SetArchiveStatusAsync(id, isArchived);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
