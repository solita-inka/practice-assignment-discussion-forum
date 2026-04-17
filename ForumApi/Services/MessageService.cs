using ForumApi.Models;
using ForumApi.DTOs.Messages;

public interface IMessageService
{
    Task<IEnumerable<MessageResponse>> GetAllMessagesByTopicIdAsync(int topicId);
    Task<MessageResponse> CreateMessageAsync(int topicId, string content, string userId);
    Task<bool?> ModifyMessageAsync(int id, string content, string userId);
    Task<bool?> DeleteMessageAsync(int id, string userId);
}
public class MessageService : IMessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly ITopicRepository _topicRepository;

    public MessageService(IMessageRepository messageRepository, ITopicRepository topicRepository)
    {
        _messageRepository = messageRepository;
        _topicRepository = topicRepository;
    }

    public async Task<IEnumerable<MessageResponse>> GetAllMessagesByTopicIdAsync(int topicId)
    {
        var messages = await _messageRepository.GetMessagesByTopicIdAsync(topicId);
        return messages.Select(m => new MessageResponse(
            m.Id,
            m.Content,
            m.CreatedAt,
            m.EditedAt,
            m.UpvoteCount,
            m.CreatedByUser.Username
        )).ToList();
    }

    public async Task<MessageResponse> CreateMessageAsync(int topicId, string content, string userId)
    {
        var topic = await _topicRepository.GetTopicByIdAsync(topicId);
        if (topic == null)
        {
            return null;
        }

        if (topic.IsArchived)
        {
            throw new InvalidOperationException("Cannot post to an archived topic.");
        }

        var newMessage = new Message
        {
            TopicId = topicId,
            Content = content,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            EditedAt = null,
            UpvoteCount = 0
        };

        var createdMessage = await _messageRepository.AddMessageAsync(newMessage);

        // Re-fetch to include the User navigation property
        createdMessage = await _messageRepository.GetMessageByIdAsync(createdMessage.Id);

        return new MessageResponse(
            createdMessage.Id,
            createdMessage.Content,
            createdMessage.CreatedAt,
            createdMessage.EditedAt,
            createdMessage.UpvoteCount,
            createdMessage.CreatedByUser.Username
        );
    }

    public async Task<bool?> ModifyMessageAsync(int id, string content, string userId)
    {
        var message = await _messageRepository.GetMessageByIdAsync(id);
        if (message == null)
        {
            return null;
        }
        if (message.CreatedByUserId != userId)
        {
            return false;
        }
        message.Content = content;
        message.EditedAt = DateTime.UtcNow;
        var success = await _messageRepository.UpdateMessageAsync(message);
        if (!success) {
            return false;
        }
        return true;
    }

    public async Task<bool?> DeleteMessageAsync(int id, string userId)
    {
        var message = await _messageRepository.GetMessageByIdAsync(id);
        if (message == null)
        {
            return null;
        }
        if (message.CreatedByUserId != userId)
        {
            return false;
        }
        var success = await _messageRepository.DeleteMessageAsync(id);
        if (!success)
        {
            return null;
        }

        return true;
    }
}