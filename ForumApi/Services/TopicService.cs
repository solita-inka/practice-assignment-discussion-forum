using Microsoft.EntityFrameworkCore;
using ForumApi.Models;
using ForumApi.DTOs.Topics;

public class TopicService
{
    private readonly ForumContext _context;

    public TopicService(ForumContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TopicSummaryDto>> GetAllAsync()
    {
        return await _context.Topics
            .OrderByDescending(t => t.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefault())
            .Select(t => new TopicSummaryDto(
                t.Id,
                t.Title,
                t.Messages.Count,
                t.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => (DateTime?)m.CreatedAt)
                    .FirstOrDefault()
            ))
            .ToListAsync();
    }

    public async Task<TopicSummaryDto> CreateAsync(string title, string userId)
    {
        var topic = new Topic
        {
            Title = title,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();
        var topicDto = new TopicSummaryDto(
            topic.Id,
            topic.Title,
            0, 
            null
        );

        return topicDto;
    }

    public async Task<TopicSummaryDto?> ModifyAsync(int id, string title)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
            return null;

        topic.Title = title;
        await _context.SaveChangesAsync();

        var topicDto = new TopicSummaryDto(
            topic.Id,
            topic.Title,
            topic.Messages.Count,
            topic.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefault()
        );

        return topicDto;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
            return false;

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();

        return true;
    }
}