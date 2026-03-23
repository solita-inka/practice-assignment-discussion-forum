using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

[ApiController]
[Route("api/messages/{messageId}/upvotes")]   
public class UpVotesController : ControllerBase
{
    private readonly UpVoteService _upVoteService;

    public UpVotesController(UpVoteService upVoteService)
    {
        _upVoteService = upVoteService;
    }

    [HttpPost]
    //[Authorize(Roles = "User, Admin")]
    public async Task<IActionResult> Upvote(int messageId)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? "anonymous";
        var message = await _upVoteService.UpVoteAsync(messageId, userId);
        if (message == null)
            return NotFound();
        if (message == -1)
            return BadRequest("This user has already upvoted this message.");

        return Ok(message);
    }
}  