using ForumApi.Models;
using ForumApi.DTOs.Messages;
using ForumApi.Exceptions;
using ForumApi.Repositories;
using Microsoft.Extensions.Logging;

namespace ForumApi.Services;

public enum MessageOperationResult
{
    Success,
    NotFound,
    Forbidden
}

public interface IMessageService
{
    Task<IEnumerable<MessageResponse>> GetAllMessagesByTopicIdAsync(int topicId);
    Task<MessageResponse?> CreateMessageAsync(int topicId, string content, string userId);
    Task<MessageOperationResult> ModifyMessageAsync(int id, string content, string userId);
    Task<MessageOperationResult> DeleteMessageAsync(int id, string userId);
}
public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly IContentModerationService _contentModerationService;
    private readonly ILogger<MessageService> _logger;

    public MessageService(IMessageRepository messageRepository, ITopicRepository topicRepository, IContentModerationService contentModerationService, ILogger<MessageService> logger)
    {
        _messageRepository = messageRepository;
        _topicRepository = topicRepository;
        _contentModerationService = contentModerationService;
        _logger = logger;
    }

    public async Task<IEnumerable<MessageResponse>> GetAllMessagesByTopicIdAsync(int topicId)
    {
        var messages = await _messageRepository.GetMessagesByTopicIdAsync(topicId);
        return messages.Select(m => new MessageResponse(
            m.Id,
            m.Content,
            m.CreatedAt,
            m.EditedAt,
            m.Upvotes.Count,
            m.CreatedByUser.Username
        )).ToList();
    }

    public async Task<MessageResponse?> CreateMessageAsync(int topicId, string content, string userId)
    {
        var topic = await _topicRepository.GetTopicByIdAsync(topicId);
        if (topic == null)
        {
            return null;
        }

        if (topic.IsArchived)
        {
            _logger.LogWarning("Attempt to post to archived topic {TopicId} by user {UserId}", topicId, userId);
            throw new InvalidOperationException("Cannot post to an archived topic.");
        }

        var moderationResult = await _contentModerationService.AnalyzeContentAsync(content);
        if (!moderationResult.IsAllowed){
            _logger.LogWarning("Content moderation failed for user {UserId} on topic {TopicId}: {Reason}", userId, topicId, moderationResult.RejectionReason);
            throw new ContentModerationException(moderationResult.RejectionReason ?? "Content rejected by moderation.");
        }

        var newMessage = new Message
        {
            TopicId = topicId,
            Content = content,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            EditedAt = null
        };

        var createdMessage = await _messageRepository.AddMessageAsync(newMessage);

        // Re-fetch to include the User and Upvotes navigation properties
        createdMessage = (await _messageRepository.GetMessageByIdAsync(createdMessage.Id))!;

        return new MessageResponse(
            createdMessage.Id,
            createdMessage.Content,
            createdMessage.CreatedAt,
            createdMessage.EditedAt,
            createdMessage.Upvotes.Count,
            createdMessage.CreatedByUser.Username
        );
    }

    public async Task<MessageOperationResult> ModifyMessageAsync(int id, string content, string userId)
    {
        var message = await _messageRepository.GetMessageByIdAsync(id);
        if (message == null)
        {
            return MessageOperationResult.NotFound;
        }
        if (message.CreatedByUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to modify message {MessageId} owned by {OwnerId}", userId, id, message.CreatedByUserId);
            return MessageOperationResult.Forbidden;
        }

        var moderationResult = await _contentModerationService.AnalyzeContentAsync(content);
        if (!moderationResult.IsAllowed)
        {
            _logger.LogWarning("Content moderation failed for user {UserId} on message {MessageId}: {Reason}", userId, id, moderationResult.RejectionReason);
            throw new ContentModerationException(moderationResult.RejectionReason ?? "Content rejected by moderation.");
        }

        message.Content = content;
        message.EditedAt = DateTime.UtcNow;
        var success = await _messageRepository.UpdateMessageAsync(message);
        if (!success) {
            return MessageOperationResult.NotFound;
        }
        return MessageOperationResult.Success;
    }

    public async Task<MessageOperationResult> DeleteMessageAsync(int id, string userId)
    {
        var message = await _messageRepository.GetMessageByIdAsync(id);
        if (message == null)
        {
            return MessageOperationResult.NotFound;
        }
        if (message.CreatedByUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to delete message {MessageId} owned by {OwnerId}", userId, id, message.CreatedByUserId);
            return MessageOperationResult.Forbidden;
        }
        var success = await _messageRepository.DeleteMessageAsync(id);
        if (!success)
        {
            return MessageOperationResult.NotFound;
        }

        return MessageOperationResult.Success;
    }
}