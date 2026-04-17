using ForumApi.Models;
using ForumApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ForumApi.Repositories;

public interface IMessageRepository
{
    Task<Message> GetMessageByIdAsync(int id);
    Task<IEnumerable<Message>> GetMessagesByTopicIdAsync(int topicId);
    Task<Message> AddMessageAsync(Message message);
    Task<bool> UpdateMessageAsync(Message message);
    Task<bool> DeleteMessageAsync(int id);
}

public class MessageRepository : IMessageRepository
{
    private readonly ForumContext _context;

    public MessageRepository(ForumContext context)
    {
        _context = context;
    }

    public async Task<Message> GetMessageByIdAsync(int id)
    {
        return await _context.Messages
            .Include(m => m.CreatedByUser)
            .Include(m => m.Upvotes)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Message>> GetMessagesByTopicIdAsync(int topicId)
    {
        return await _context.Messages
            .Include(m => m.CreatedByUser)
            .Include(m => m.Upvotes)
            .Where(m => m.TopicId == topicId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<Message> AddMessageAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<bool> UpdateMessageAsync(Message message)
    {
        var messageToUpdate = await _context.Messages.FindAsync(message.Id);
        if (messageToUpdate == null)
            return false;
        messageToUpdate.Content = message.Content;
        messageToUpdate.EditedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMessageAsync(int id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message != null)
        {
            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}