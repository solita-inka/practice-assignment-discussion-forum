using ForumApi.Models;
using ForumApi.DTOs.Pagination;
using ForumApi.DTOs.Topics;
using ForumApi.Repositories;
using Microsoft.Extensions.Logging;

namespace ForumApi.Services;

public interface ITopicService
{
    Task<PagedResponse<TopicSummaryDto>> GetAllAsync(int page, int pageSize, bool archived = false);
    Task<TopicSummaryDto> CreateAsync(string title, string userId);
    Task<bool> ModifyAsync(int id, string title, string userId);
    Task<bool> DeleteAsync(int id);
    Task<bool> SetArchiveStatusAsync(int id, bool isArchived);
}
public class TopicService : ITopicService
{
    private readonly ITopicRepository _topicRepository;
    private readonly ILogger<TopicService> _logger;

    public TopicService(ITopicRepository topicRepository, ILogger<TopicService> logger)
    {
        _topicRepository = topicRepository;
        _logger = logger;
    }

    public async Task<PagedResponse<TopicSummaryDto>> GetAllAsync(int page, int pageSize, bool archived = false)
    {
        var (topics, totalCount) = await _topicRepository.GetAllTopicsAsync(page, pageSize, archived);
        var items = topics.Select(t => new TopicSummaryDto(
            t.Id,
            t.Title,
            t.Messages.Count,
            t.Messages.Any() ? t.Messages.Max(m => m.CreatedAt) : null,
            t.IsArchived
        ));
        return new PagedResponse<TopicSummaryDto>(items, totalCount, page, pageSize);
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

        _logger.LogInformation("Topic {TopicId} created by user {UserId}", topic.Id, userId);
        var topicDto = new TopicSummaryDto(
            topic.Id,
            topic.Title,
            0, 
            null,
            false
        );

        return topicDto;
    }

    public async Task<bool> ModifyAsync(int id, string title, string userId)
    {
       
        return await _topicRepository.UpdateTopicAsync(id, title);

    }

    public async Task<bool> DeleteAsync(int id)
    {
        var result = await _topicRepository.DeleteTopicAsync(id);
        if (result)
            _logger.LogInformation("Topic {TopicId} deleted", id);
        return result;
    }

    public async Task<bool> SetArchiveStatusAsync(int id, bool isArchived)
    {
        var result = await _topicRepository.SetArchiveStatusAsync(id, isArchived);
        if (result)
            _logger.LogInformation("Topic {TopicId} archive status set to {IsArchived}", id, isArchived);
        return result;
    }
}