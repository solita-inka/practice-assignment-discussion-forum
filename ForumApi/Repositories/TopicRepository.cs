using ForumApi.Models;
using Microsoft.EntityFrameworkCore;

public interface ITopicRepository
{
    Task<IEnumerable<Topic>> GetAllTopicsAsync();
    Task<Topic> GetTopicByIdAsync(int id);
    Task<Topic> CreateTopicAsync(Topic topic);
    Task<bool> UpdateTopicAsync(int id, string title);
    Task<bool> DeleteTopicAsync(int id);
}

public class TopicRepository : ITopicRepository
{
    private readonly ForumContext _context;

    public TopicRepository(ForumContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Topic>> GetAllTopicsAsync()
    {
        return await _context.Topics.Include(t => t.Messages).ToListAsync();
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
}