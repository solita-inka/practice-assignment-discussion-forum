using ForumApi.Models;
using Microsoft.EntityFrameworkCore;    
public interface IUpVoteRepository
{
    Task<MessageUpVote?> CreateUpVoteAsync(MessageUpVote upVote);
    Task<bool> DeleteUpVoteAsync(int messageId, string userId);
}

public class UpVoteRepository : IUpVoteRepository
{
    private readonly ForumContext _context;

    public UpVoteRepository(ForumContext context)
    {
        _context = context;
    }

    public async Task<MessageUpVote?> CreateUpVoteAsync(MessageUpVote upVote)
    {
        var message = await _context.Messages.FindAsync(upVote.MessageId);
        if (message == null) {
            return null;
        }   
        var alreadyUpVoted = await _context.MessageUpVotes.AnyAsync(u => u.MessageId == upVote.MessageId && u.CreatedByUserId == upVote.CreatedByUserId);
        if (alreadyUpVoted) {
            return null;
        }                    
        _context.MessageUpVotes.Add(upVote);
        message.UpvoteCount++;
        await _context.SaveChangesAsync();
        var createdUpVote = await _context.MessageUpVotes
            .Include(u => u.Message)
            .FirstOrDefaultAsync(u => u.Id == upVote.Id);
        return createdUpVote;
    }

    public async Task<bool> DeleteUpVoteAsync(int messageId, string userId)
    {
        var upVote = await _context.MessageUpVotes
            .FirstOrDefaultAsync(u => u.MessageId == messageId && u.CreatedByUserId == userId);
        if (upVote == null){
            return false;
        }

        var message = await _context.Messages.FindAsync(upVote.MessageId);
        if (message != null)
        {
            message.UpvoteCount--;
        }

        _context.MessageUpVotes.Remove(upVote);
        await _context.SaveChangesAsync();
        return true;
       
    }
}