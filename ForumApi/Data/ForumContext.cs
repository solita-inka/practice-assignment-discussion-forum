using Microsoft.EntityFrameworkCore;
using ForumApi.Models;

public class ForumContext : DbContext
{
    public ForumContext(DbContextOptions<ForumContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageUpVote> MessageUpVotes { get; set; }
}