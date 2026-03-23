namespace ForumApi.DTOs.Messages;
public record CreateMessageRequest
(
    string Content,
    string UserId
);
