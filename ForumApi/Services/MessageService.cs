using Microsoft.EntityFrameworkCore;
using ForumApi.Models;
using ForumApi.DTOs.Messages;

public class MessageService
{
    private readonly ForumContext _context;
    private readonly AuthService _authService;

    public MessageService(ForumContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    public async Task<IEnumerable<MessageResponse>> GetAllByTopicIdAsync(int topicId)
    {
        return await _context.Messages
        .Where(m => m.TopicId == topicId)
        .Select(m => new MessageResponse(
            m.Id,
            m.Content,
            m.CreatedAt,
            m.EditedAt,
            m.UpvoteCount,
            null
        ))
        .ToListAsync();
    }

    public async Task<MessageResponse?> CreateAsync(int topicId, string content, string userId)
    {
        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null)
            return null; 

        var message = new Message
        {
            TopicId = topicId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        var createdMessage = await _context.Messages.FindAsync(message.Id);
        if(createdMessage == null)
            return null;

        return new MessageResponse(
            createdMessage.Id,
            createdMessage.Content,
            createdMessage.CreatedAt,
            createdMessage.EditedAt,
            createdMessage.UpvoteCount,
            _authService.GetUsernameById(userId) 
        );
    }

    public async Task<MessageResponse?> ModifyAsync(int id, string content)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message == null)
            return null;

        message.Content = content;
        await _context.SaveChangesAsync();

        return new MessageResponse(
            message.Id,
            message.Content,
            message.CreatedAt,
            message.EditedAt,
            message.UpvoteCount,
            _authService.GetUsernameById(message.CreatedByUserId)
        );
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var message = await _context.Messages.FindAsync(id);
        if (message == null)
            return false;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        return true;
    }
}