namespace ForumApi.DTOs.Topics;

public record TopicSummaryDto
(
    int Id,
    string Title,
    int MessageCount,
    DateTime? LastMessageAt
);
