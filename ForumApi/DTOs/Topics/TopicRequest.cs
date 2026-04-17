using System.ComponentModel.DataAnnotations;

namespace ForumApi.DTOs.Topics;

public record TopicRequest
(
    [StringLength(500)] string Title
);
