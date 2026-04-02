using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ForumApi.Services;

public interface IUpVotesController
{
    Task<IActionResult> Upvote(int messageId);
    Task<IActionResult> DeleteUpvote(int messageId);
}

[ApiController]
[Route("api/messages/{messageId}/upvotes")]   
public class UpVotesController : ControllerBase, IUpVotesController
{
    private readonly IUpVoteService _upVoteService;

    public UpVotesController(IUpVoteService upVoteService)
    {
        _upVoteService = upVoteService;
    }

    [HttpPost]
    [Authorize(Roles = "User, Admin")]
    public async Task<IActionResult> Upvote(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        var success = await _upVoteService.UpVoteAsync(messageId, userId);
        if (!success){
            return BadRequest();
        }

        return NoContent();
    }

    [HttpDelete]
    [Authorize(Roles = "User, Admin")]
    public async Task<IActionResult> DeleteUpvote(int messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }
        var result = await _upVoteService.DeleteUpVoteAsync(messageId, userId);
        return result switch
        {
            DeleteUpVoteResult.NotFound => NotFound(),
            DeleteUpVoteResult.Forbidden => Forbid(),
            _ => NoContent()
        };
    }       

}