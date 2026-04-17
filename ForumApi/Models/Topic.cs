
namespace ForumApi.Models;

public class Topic
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string CreatedByUserId { get; set; }
    public bool IsArchived { get; set; } = false;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}