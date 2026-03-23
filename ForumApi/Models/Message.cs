
namespace ForumApi.Models;

public class Message
{
    public int Id { get; set; }
    public required int TopicId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public int UpvoteCount { get; set; } = 0;
    public required string CreatedByUserId { get; set; }
    public Topic Topic { get; set; }  = null!;
    public ICollection<MessageUpVote> Upvotes { get; set; } = new List<MessageUpVote>();
}