namespace ForumApi.DTOs.Messages;

public record MessageResponse
(
    int Id,
    string Content,
    DateTime CreatedAt,
    DateTime? EditedAt,
    int UpVoteCount,
    string UserName
);