using ForumApi.Models;
using ForumApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ForumApi.Repositories;

public interface ITopicRepository
{
    Task<(IEnumerable<Topic> Items, int TotalCount)> GetAllTopicsAsync(int page, int pageSize, bool archived = false);
    Task<Topic> GetTopicByIdAsync(int id);
    Task<Topic> CreateTopicAsync(Topic topic);
    Task<bool> UpdateTopicAsync(int id, string title);
    Task<bool> DeleteTopicAsync(int id);
    Task<bool> SetArchiveStatusAsync(int id, bool isArchived);
}

public class TopicRepository : ITopicRepository
{
    private readonly ForumContext _context;

    public TopicRepository(ForumContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Topic> Items, int TotalCount)> GetAllTopicsAsync(int page, int pageSize, bool archived = false)
    {
        var query = _context.Topics
            .Where(t => t.IsArchived == archived);

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(t => t.Messages)
            .OrderByDescending(t => t.Messages
                .Select(m => (DateTime?)m.CreatedAt)
                .Max() ?? t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Topic> GetTopicByIdAsync(int id)
    {   
        return await _context.Topics.FindAsync(id);
    }

    public async Task<Topic> CreateTopicAsync(Topic topic)
    {
        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();
        return topic;
    }

    public async Task<bool> UpdateTopicAsync(int id, string title)
    {
       var currentTopic = await _context.Topics.FindAsync(id);
        if (currentTopic == null) {
            return false;
        }
        currentTopic.Title = title;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteTopicAsync(int id)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return false;
        }
      
        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();
        return true;
       
    }

    public async Task<bool> SetArchiveStatusAsync(int id, bool isArchived)
    {
        var topic = await _context.Topics.FindAsync(id);
        if (topic == null)
        {
            return false;
        }
        topic.IsArchived = isArchived;
        await _context.SaveChangesAsync();
        return true;
    }
}