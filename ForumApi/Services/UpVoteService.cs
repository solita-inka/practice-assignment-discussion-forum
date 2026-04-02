// Services/UpVoteService.cs
using ForumApi.Models;

namespace ForumApi.Services;
public interface IUpVoteService
{
    Task<bool> UpVoteAsync(int messageId, string userId);
    Task<DeleteUpVoteResult> DeleteUpVoteAsync(int messageId, string userId);
}

public class UpVoteService : IUpVoteService
{
    private readonly IUpVoteRepository _upVoteRepository;

    public UpVoteService(IUpVoteRepository upVoteRepository)
    {
        _upVoteRepository = upVoteRepository;
    }

    public async Task<bool> UpVoteAsync(int messageId, string userId)
    {
        var upVote = new MessageUpVote
        {
            MessageId = messageId,
            CreatedByUserId = userId
        };

        var createdUpVote = await _upVoteRepository.CreateUpVoteAsync(upVote);
        if (createdUpVote == null)
        {
            return false;
        }

        return true;
    }

    public async Task<DeleteUpVoteResult> DeleteUpVoteAsync(int messageId, string userId)
    {
        return await _upVoteRepository.DeleteUpVoteAsync(messageId, userId);
    }

}
