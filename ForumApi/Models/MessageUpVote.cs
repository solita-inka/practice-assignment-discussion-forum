
namespace ForumApi.Models;

public class MessageUpVote
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public required string CreatedByUserId { get; set; }
    public Message Message { get; set; } = null!;
}