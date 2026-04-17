using Xunit;
using ForumApi.Models;
using ForumApi.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ForumApi.Functions;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace ForumApi.Functions.Tests;

public class TopicArchiveTimerTests
{
    private ForumContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ForumContext>()
            .UseSqlServer("Server=localhost,1433;Database=forumdb_test_functions;User Id=sa;Password=Forum_Password123!;TrustServerCertificate=True;")
            .Options;
        var context = new ForumContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }

    private ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder => builder.AddConsole());
    }


    [Fact]
    public async Task ArchivesTopics_OlderThanThreshold_WithNoMessages()
    {
        var context = CreateContext();
        var oldTopic = new Topic { Title = "Old", CreatedAt = DateTime.UtcNow.AddDays(-60), CreatedByUserId = "1" };
        var anotherOldTopic = new Topic { Title = "Another Old", CreatedAt = DateTime.UtcNow.AddDays(-40), CreatedByUserId = "1" };
        context.Topics.AddRange(oldTopic, anotherOldTopic);
        await context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("TopicArchiveTimeRangeInDays", "30");
        var timer = new TopicArchiveTimer(CreateLoggerFactory(), context);
        await timer.Run(null);

        var archivedOldTopic = await context.Topics.FindAsync(oldTopic.Id);
        Assert.NotNull(archivedOldTopic);
        Assert.True(archivedOldTopic.IsArchived);

        var archivedAnotherOldTopic = await context.Topics.FindAsync(anotherOldTopic.Id);
        Assert.NotNull(archivedAnotherOldTopic);
        Assert.True(archivedAnotherOldTopic.IsArchived);
    }

    [Fact]
    public async Task ArchivesTopics_OlderThanThreshold_WithMessages()
    {
        var context = CreateContext();
        var topic = new Topic { Title = "Topic with Messages", CreatedAt = DateTime.UtcNow.AddDays(-7), CreatedByUserId = "1" };
        context.Topics.Add(topic);
        await context.SaveChangesAsync();

        var user = new User { Id = "1", Username = "testuser" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var message = new Message { Content = "Old message", CreatedAt = DateTime.UtcNow.AddDays(-30), CreatedByUserId = "1", TopicId = topic.Id };
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("TopicArchiveTimeRangeInDays", "30");
        var timer = new TopicArchiveTimer(CreateLoggerFactory(), context);
        await timer.Run(null);

        var archivedTopic = await context.Topics.FindAsync(topic.Id);
        Assert.NotNull(archivedTopic);
        Assert.True(archivedTopic.IsArchived);

    }

    [Fact]
    public async Task DoesNotArchiveTopics_IfMessagesAreNewerThanThreshold()
    {
        var context = CreateContext();
        var topic = new Topic { Title = "Topic with Recent Messages", CreatedAt = DateTime.UtcNow.AddDays(-60), CreatedByUserId = "1" };
        context.Topics.Add(topic);
        await context.SaveChangesAsync();

        var user = new User { Id = "1", Username = "testuser" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var message = new Message { Content = "Recent message", CreatedAt = DateTime.UtcNow.AddDays(-1), CreatedByUserId = "1", TopicId = topic.Id };
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("TopicArchiveTimeRangeInDays", "30");
        var timer = new TopicArchiveTimer(CreateLoggerFactory(), context);
        await timer.Run(null);

        var archivedTopic = await context.Topics.FindAsync(topic.Id);
        Assert.NotNull(archivedTopic);
        Assert.False(archivedTopic.IsArchived);

    }

    [Fact]
    public async Task DoesNotArchiveTopics_IfNoMessagesAndCreatedAtIsNewerThanThreshold()
    {
        var context = CreateContext();
        var freshTopic = new Topic { Title = "Fresh", CreatedAt = DateTime.UtcNow.AddDays(-1), CreatedByUserId = "1" };
        context.Topics.Add(freshTopic);
        await context.SaveChangesAsync();

        Environment.SetEnvironmentVariable("TopicArchiveTimeRangeInDays", "30");
        var timer = new TopicArchiveTimer(CreateLoggerFactory(), context);
        await timer.Run(null);
        var archivedFreshTopic = await context.Topics.FindAsync(freshTopic.Id);
        Assert.NotNull(archivedFreshTopic);
        Assert.False(archivedFreshTopic.IsArchived);
    }
}