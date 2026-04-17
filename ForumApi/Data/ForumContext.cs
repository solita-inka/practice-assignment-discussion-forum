using Microsoft.EntityFrameworkCore;
using ForumApi.Models;

namespace ForumApi.Data;

public class ForumContext : DbContext
{
    public ForumContext(DbContextOptions<ForumContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageUpVote> MessageUpVotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(200);
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.Property(t => t.Title).HasMaxLength(500);
            entity.HasIndex(t => t.IsArchived);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(m => m.TopicId);
        });

        modelBuilder.Entity<MessageUpVote>(entity =>
        {
            entity.HasIndex(u => new { u.MessageId, u.CreatedByUserId }).IsUnique();
        });
    }
}