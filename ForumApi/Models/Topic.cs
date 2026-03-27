
namespace ForumApi.Models;

public class Topic
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required string CreatedByUserId { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}