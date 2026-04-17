using System.ComponentModel.DataAnnotations;

namespace ForumApi.DTOs.Messages;
public record MessageRequest
(
    [StringLength(4000)] string Content
);