// Services/UpVoteService.cs
using ForumApi.Models;
using Microsoft.EntityFrameworkCore;
public class UpVoteService
{
    private readonly ForumContext _context;

    public UpVoteService(ForumContext context)
    {
        _context = context;
    }

    public async Task<int?> UpVoteAsync(int messageId, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null)
            return null;

        var alreadyUpVoted = await _context.MessageUpVotes.AnyAsync(u => u.MessageId == messageId && u.CreatedByUserId == userId);

        if (alreadyUpVoted)
            return -1;

        _context.MessageUpVotes.Add(new MessageUpVote
        {
            MessageId = messageId,
            CreatedByUserId = userId
        });

        await _context.SaveChangesAsync();

        return await _context.MessageUpVotes.CountAsync(u => u.MessageId == messageId);
    }

}
