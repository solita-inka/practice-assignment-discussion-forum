using ForumApi.Models;
using ForumApi.DTOs.Topics;

public interface ITopicService
{
    Task<IEnumerable<TopicSummaryDto>> GetAllAsync();
    Task<TopicSummaryDto> CreateAsync(string title, string userId);
    Task<bool> ModifyAsync(int id, string title, string userId);
    Task<bool> DeleteAsync(int id);
}
public class TopicService : ITopicService
{
    private readonly ITopicRepository _topicRepository;

    public TopicService(ITopicRepository topicRepository)
    {
        _topicRepository = topicRepository;
    }

    public async Task<IEnumerable<TopicSummaryDto>> GetAllAsync()
    {
        var topics = await _topicRepository.GetAllTopicsAsync();
        return topics.Select(t => new TopicSummaryDto(
            t.Id,
            t.Title,
            t.Messages.Count,
            t.Messages
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (DateTime?)m.CreatedAt)
                .FirstOrDefault()
        ));
    }

    public async Task<TopicSummaryDto> CreateAsync(string title, string userId)
    {
        var topic = new Topic
        {
            Title = title,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _topicRepository.CreateTopicAsync(topic);

        var topicDto = new TopicSummaryDto(
            topic.Id,
            topic.Title,
            0, 
            null
        );

        return topicDto;
    }

    public async Task<bool> ModifyAsync(int id, string title, string userId)
    {
       
        return await _topicRepository.UpdateTopicAsync(id, title);

    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _topicRepository.DeleteTopicAsync(id);
    
    }
}